import { useEffect, useMemo, useState } from "react";
import {
  claimDetection,
  createCustomer,
  createSignature,
  getCustomers,
  getDetections,
  getQueue,
  getSignatures,
  resolveDetection
} from "./api";
import { createDetectionConnection } from "./signalr";
import type {
  Customer,
  Detection,
  LoginResponse,
  Signature
} from "./types";
import { DetectionStatus } from "./types";

import { Login } from "./components/Login";
import { DetectionQueue } from "./components/DetectionQueue";
import { ActiveDetection } from "./components/ActiveDetection";
import { Customers } from "./components/Customers";
import { Signatures } from "./components/Signatures";
import { History } from "./components/History";

// @ts-expect-error CSS is handled by the frontend bundler.
import "./styles.css";

type Page = "queue" | "customers" | "signatures" | "detections";

export default function App() {
  const [session, setSession] = useState<LoginResponse | null>(() => {
    const x = localStorage.getItem("session");
    return x ? JSON.parse(x) : null;
  });

  const [page, setPage] = useState<Page>("queue");
  const [queue, setQueue] = useState<Detection[]>([]);
  const [history, setHistory] = useState<Detection[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [signatures, setSignatures] = useState<Signature[]>([]);
  const [active, setActive] = useState<Detection | null>(null);
  const [error, setError] = useState("");

  const analyst = session?.role === "SecurityAnalyst";
  const admin = session?.role === "Admin";

  const sorted = useMemo(
    () =>
      [...queue].sort(
        (a, b) =>
          b.priority - a.priority ||
          new Date(a.createdAtUtc).getTime() -
            new Date(b.createdAtUtc).getTime()
      ),
    [queue]
  );

  useEffect(() => {
    if (!session) {
      return;
    }

    void load();

    const c = createDetectionConnection();

    c.on("DetectionCreated", (d: Detection) => {
      setQueue((q) =>
        q.some((x) => x.id === d.id) ? q : [...q, d]
      );

      setHistory((h) =>
        h.some((x) => x.id === d.id) ? h : [d, ...h]
      );
    });

    c.on(
      "DetectionClaimed",
      (e: { detectionId: number; userId: number }) => {
        setQueue((q) =>
          q.filter((x) => x.id !== e.detectionId)
        );

        if (e.userId === session.userId) {
          void refresh();
        }
      }
    );

    c.on(
      "DetectionResolved",
      (e: { detectionId: number }) => {
        setQueue((q) =>
          q.filter((x) => x.id !== e.detectionId)
        );

        setActive((a) =>
          a?.id === e.detectionId ? null : a
        );

        void refresh();
      }
    );

    void c.start();

    return () => {
      void c.stop();
    };
  }, [session]);

  async function load() {
    try {
      const [q, h, cs, ss] = await Promise.all([
        getQueue(),
        getDetections(),
        getCustomers(),
        getSignatures()
      ]);

      setQueue(q);
      setHistory(h);
      setCustomers(cs);
      setSignatures(ss);

      if (analyst) {
        setActive(
          h.find(
            (x) =>
              x.status === DetectionStatus.Assigned &&
              x.assignedToUserId === session?.userId
          ) ?? null
        );
      }
    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : "Failed to load data."
      );
    }
  }

  async function refresh() {
    try {
      setHistory(await getDetections());
    } catch {
      // Ignore refresh errors.
    }
  }

  function onLogin(x: LoginResponse) {
    localStorage.setItem("token", x.token);
    localStorage.setItem("session", JSON.stringify(x));
    setSession(x);
  }

  function logout() {
    localStorage.clear();
    setSession(null);
  }

  async function claim(id: number) {
    try {
      const d = await claimDetection(id);

      setQueue((q) =>
        q.filter((x) => x.id !== id)
      );

      setActive(d);

      await refresh();
    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : "Claim failed."
      );
    }
  }

  async function resolve(id: number, resolution: string) {
    try {
      await resolveDetection(id, resolution);

      setActive(null);

      await refresh();
    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : "Resolve failed."
      );
    }
  }

  async function handleCreateCustomer(
    name: string,
    importance: number
  ) {
    try {
      const customer = await createCustomer(
        name,
        importance
      );

      setCustomers((current) => [
        ...current,
        customer
      ]);
    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : "Create failed."
      );
    }
  }

  async function handleCreateSignature(
    name: string,
    priority: number,
    conditions: string
  ) {
    try {
      const signature = await createSignature(
        name,
        priority,
        conditions
      );

      setSignatures((current) => [
        ...current,
        signature
      ]);
    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : "Create failed."
      );
    }
  }

  if (!session) {
    return <Login onLogin={onLogin} />;
  }

  return (
    <div className="app">
      <header>
        <div>
          <strong>
            Security Detection Platform
          </strong>

          <span>
            {session.username} · {session.role}
          </span>
        </div>

        <nav>
          {(
            [
              ["queue", "Queue"],
              ["customers", "Customers"],
              ["signatures", "Signatures"],
              ["detections", "Detections"]
            ] as const
          ).map(([key, title]) => (
            <button
              key={key}
              className={
                page === key
                  ? "nav active"
                  : "nav"
              }
              onClick={() => setPage(key)}
            >
              {title}
            </button>
          ))}

          <button
            className="nav"
            onClick={logout}
          >
            Logout
          </button>
        </nav>
      </header>

      <main>
        {error && (
          <div className="error">
            {error}

            <button
              onClick={() => setError("")}
            >
              Dismiss
            </button>
          </div>
        )}

        {page === "queue" && (
          <>
            <DetectionQueue
              detections={sorted}
              canClaim={analyst}
              onClaim={(id) => void claim(id)}
            />

            {analyst && (
              <ActiveDetection
                detection={active}
                onResolve={(id, resolution) =>
                  void resolve(id, resolution)
                }
              />
            )}
          </>
        )}

        {page === "customers" && (
          <Customers
            customers={customers}
            canManage={admin}
            onCreate={handleCreateCustomer}
          />
        )}

        {page === "signatures" && (
          <Signatures
            signatures={signatures}
            canManage={admin}
            onCreate={handleCreateSignature}
          />
        )}

        {page === "detections" && (
          <History detections={history} />
        )}
      </main>
    </div>
  );
}