import { useState } from "react";
import type { Customer } from "../types";

export function Customers({
    customers,
    canManage,
    onCreate,
}: {
    customers: Customer[];
    canManage: boolean;
    onCreate: (n: string, i: number) => void;
}) {
    const [n, setN] = useState("");
    const [i, setI] = useState(5);

    return (
        <section className="card">
            <h2>Customers</h2>

            <p className="muted">
                Importance contributes to detection priority.
            </p>

            {canManage && (
                <div className="form">
                    <input
                        placeholder="Customer name"
                        value={n}
                        onChange={(e) => setN(e.target.value)}
                    />

                    <input
                        type="number"
                        min="1"
                        max="10"
                        value={i}
                        onChange={(e) => setI(Number(e.target.value))}
                    />

                    <button
                        onClick={() => {
                            if (n.trim()) {
                                onCreate(n.trim(), i);
                                setN("");
                            }
                        }}
                    >
                        Add Customer
                    </button>
                </div>
            )}

            <div className="table">
                <table>
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Importance</th>
                            <th>Created UTC</th>
                        </tr>
                    </thead>

                    <tbody>
                        {customers.map((c) => (
                            <tr key={c.id}>
                                <td>{c.name}</td>
                                <td>{c.importance}</td>
                                <td>
                                    {new Date(
                                        c.createdAtUtc
                                    ).toISOString()}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </section>
    );
}