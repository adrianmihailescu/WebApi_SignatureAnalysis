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
    const [search, setSearch] = useState("");
    const [sortBy, setSortBy] = useState<"name" | "importance" | "created">("created");
    const [sortDescending, setSortDescending] = useState(true);

    const filteredCustomers = customers.filter((customer) =>
        customer.name.toLowerCase().includes(search.trim().toLowerCase())
    );
    const sortedCustomers = [...filteredCustomers].sort((a, b) => {
        let comparison: number;

        switch (sortBy) {
            case "name":
                comparison = a.name.localeCompare(b.name);
                break;
            case "importance":
                comparison = a.importance - b.importance;
                break;
            case "created":
            default:
                comparison = new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime();
                break;
        }

        return sortDescending ? -comparison : comparison;
    });

    function changeSort(nextSort: typeof sortBy) {
        if (sortBy === nextSort) {
            setSortDescending((descending) => !descending);
        } else {
            setSortBy(nextSort);
            setSortDescending(nextSort !== "name");
        }
    }

    function sortIndicator(column: typeof sortBy) {
        if (sortBy !== column) {
            return "";
        }

        return sortDescending ? " ↓" : " ↑";
    }

    return (
        <section className="card">
            <div className="head">
                <div>
                    <h2>Customers</h2>

                    <p className="muted">
                        Importance contributes to detection priority.
                    </p>
                </div>

                <input
                    type="search"
                    placeholder="Search customers by name"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    aria-label="Search customers"
                />
            </div>

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

            {filteredCustomers.length === 0 ? (
                <div className="empty">
                    {search.trim() ? "No customers found." : "No customers available."}
                </div>
            ) : (
                <div className="table">
                    <table>
                        <thead>
                            <tr>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("name")}>
                                        Name{sortIndicator("name")}
                                    </button>
                                </th>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("importance")}>
                                        Importance{sortIndicator("importance")}
                                    </button>
                                </th>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("created")}>
                                        Created UTC{sortIndicator("created")}
                                    </button>
                                </th>
                            </tr>
                        </thead>

                        <tbody>
                            {sortedCustomers.map((c) => (
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
            )}
        </section>
    );
}