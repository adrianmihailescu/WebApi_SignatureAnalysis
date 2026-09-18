import { useState } from "react";
import { login } from "../api";
import type { LoginResponse } from "../types";

export function Login({
    onLogin,
}: {
    onLogin: (x: LoginResponse) => void;
}) {
    const [u, setU] = useState("analyst");
    const [p, setP] = useState("Analyst123!");
    const [e, setE] = useState("");

    async function submit() {
        try {
            setE("");
            onLogin(await login(u, p));
        } catch (x) {
            setE(
                x instanceof Error
                    ? x.message
                    : "Login failed."
            );
        }
    }

    return (
        <main className="login">
            <section className="card login-card">
                <h1>Security Detection Platform</h1>

                <p className="muted">
                    Sign in to continue.
                </p>

                <label>Username</label>

                <input
                    value={u}
                    onChange={(x) => setU(x.target.value)}
                />

                <label>Password</label>

                <input
                    type="password"
                    value={p}
                    onChange={(x) => setP(x.target.value)}
                    onKeyDown={(x) =>
                        x.key === "Enter" && void submit()
                    }
                />

                {e && (
                    <div className="error">
                        {e}
                    </div>
                )}

                <button onClick={() => void submit()}>
                    Login
                </button>

                <div className="demo">
                    <b>Demo:</b>
                    <br />
                    analyst / Analyst123!
                    <br />
                    admin / Admin123!
                    <br />
                    support / Support123!
                </div>
            </section>
        </main>
    );
}