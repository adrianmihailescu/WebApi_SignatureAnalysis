import { useState } from "react";
import type { Detection } from "../types";

export function ActiveDetection({
    detection,
    onResolve,
}: {
    detection: Detection | null;
    onResolve: (id: number, r: string) => void;
}) {
    const [r, setR] = useState("");

    if (!detection) {
        return (
            <section className="card">
                <h2>Active Detection</h2>

                <div className="empty">
                    No active detection. Claim one from the queue.
                </div>
            </section>
        );
    }

    return (
        <section className="card active">
            <div className="head">
                <div>
                    <h2>Active Detection #{detection.id}</h2>

                    <p className="muted">
                        Currently assigned to the signed-in analyst.
                    </p>
                </div>

                <span className="priority">
                    Priority {detection.priority}
                </span>
            </div>

            <div className="grid">
                <div>
                    <small>Customer</small>
                    <b>{detection.customerName}</b>
                </div>

                <div>
                    <small>Signature</small>
                    <b>{detection.signatureName}</b>
                </div>

                <div>
                    <small>Claimed UTC</small>
                    <b>
                        {detection.claimedAtUtc
                            ? new Date(
                                detection.claimedAtUtc
                            ).toISOString()
                            : "-"}
                    </b>
                </div>
            </div>

            <h3>Incident Payload</h3>

            <pre>
                {JSON.stringify(
                    JSON.parse(detection.incidentPayload),
                    null,
                    2
                )}
            </pre>

            <h3>Resolution</h3>

            <textarea
                rows={5}
                value={r}
                onChange={(e) => setR(e.target.value)}
                placeholder="Describe how the incident was resolved..."
            />

            <button
                disabled={!r.trim()}
                onClick={() => {
                    onResolve(detection.id, r);
                    setR("");
                }}
            >
                Resolve Detection
            </button>
        </section>
    );
}