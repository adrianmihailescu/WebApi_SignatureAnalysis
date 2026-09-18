import { useState } from "react";
import type { Detection } from "../types";

export function DetectionQueue({
    detections,
    canClaim,
    onClaim,
}: {
    detections: Detection[];
    canClaim: boolean;
    onClaim: (id: number) => void;
}) {
    const [search, setSearch] = useState("");
    const [sortBy, setSortBy] = useState<"customer" | "signature" | "priority" | "created">("priority");
    const [sortDescending, setSortDescending] = useState(true);
    const normalizedSearch = search.trim().toLowerCase();
    const filteredDetections = detections.filter((detection) =>
        [
            detection.customerName,
            detection.signatureName,
        ].some((value) => value.toLowerCase().includes(normalizedSearch))
    );
    const sortedDetections = [...filteredDetections].sort((a, b) => {
        let comparison: number;

        switch (sortBy) {
            case "customer":
                comparison = a.customerName.localeCompare(b.customerName);
                break;
            case "signature":
                comparison = a.signatureName.localeCompare(b.signatureName);
                break;
            case "created":
                comparison = new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime();
                break;
            case "priority":
            default:
                comparison = a.priority - b.priority;
                break;
        }

        return sortDescending ? -comparison : comparison;
    });

    function changeSort(nextSort: typeof sortBy) {
        if (sortBy === nextSort) {
            setSortDescending((descending) => !descending);
        } else {
            setSortBy(nextSort);
            setSortDescending(nextSort === "priority" || nextSort === "created");
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
                    <h2>Detection Queue</h2>

                    <p className="muted">
                        Open detections requiring attention.
                    </p>
                </div>

                <span className="badge">
                    {detections.length}
                </span>
            </div>

            <div className="controls">
                <input
                    type="search"
                    placeholder="Search queue by customer or signature"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    aria-label="Search detection queue"
                />

            </div>

            {!detections.length ? (
                <div className="empty">
                    No open detections.
                </div>
            ) : !filteredDetections.length ? (
                <div className="empty">
                    No detections found.
                </div>
            ) : (
                <div className="table">
                    <table>
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("customer")}>
                                        Customer{sortIndicator("customer")}
                                    </button>
                                </th>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("signature")}>
                                        Signature{sortIndicator("signature")}
                                    </button>
                                </th>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("priority")}>
                                        Priority{sortIndicator("priority")}
                                    </button>
                                </th>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("created")}>
                                        Created UTC{sortIndicator("created")}
                                    </button>
                                </th>
                                <th />
                            </tr>
                        </thead>

                        <tbody>
                            {sortedDetections.map((d) => (
                                <tr key={d.id}>
                                    <td>#{d.id}</td>

                                    <td>{d.customerName}</td>

                                    <td>{d.signatureName}</td>

                                    <td>
                                        <b>{d.priority}</b>
                                    </td>

                                    <td>
                                        {new Date(
                                            d.createdAtUtc
                                        ).toISOString()}
                                    </td>

                                    <td>
                                        {canClaim && (
                                            <button
                                                className="small"
                                                onClick={() => onClaim(d.id)}
                                            >
                                                Claim
                                            </button>
                                        )}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            <div className="status-summary">
                <strong>Total detections: {detections.length}</strong>
            </div>
        </section>
    );
}