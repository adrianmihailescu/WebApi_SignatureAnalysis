import { useState } from "react";
import type { Detection } from "../types";
import { DetectionStatus } from "../types";

export function History({
    detections,
}: {
    detections: Detection[];
}) {
    const [search, setSearch] = useState("");
    const [sortBy, setSortBy] = useState<"id" | "customer" | "signature" | "priority" | "status" | "created" | "resolution">("created");
    const [sortDescending, setSortDescending] = useState(true);
    const normalizedSearch = search.trim().toLowerCase();
    const filteredDetections = detections.filter((detection) =>
        [detection.customerName, detection.signatureName].some((value) =>
            value.toLowerCase().includes(normalizedSearch)
        )
    );
    const sortedDetections = [...filteredDetections].sort((a, b) => {
        let comparison: number;

        switch (sortBy) {
            case "id":
                comparison = a.id - b.id;
                break;
            case "customer":
                comparison = a.customerName.localeCompare(b.customerName);
                break;
            case "signature":
                comparison = a.signatureName.localeCompare(b.signatureName);
                break;
            case "priority":
                comparison = a.priority - b.priority;
                break;
            case "status":
                comparison = DetectionStatus[a.status].localeCompare(DetectionStatus[b.status]);
                break;
            case "resolution":
                comparison = (a.resolution ?? "-").localeCompare(b.resolution ?? "-");
                break;
            case "created":
            default:
                comparison = new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime();
                break;
        }

        return sortDescending ? -comparison : comparison;
    });
    const statusCounts = filteredDetections.reduce(
        (counts, detection) => {
            counts[detection.status] += 1;
            return counts;
        },
        {
            [DetectionStatus.Open]: 0,
            [DetectionStatus.Assigned]: 0,
            [DetectionStatus.Resolved]: 0,
        } as Record<DetectionStatus, number>
    );

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
                    <h2>Detection History</h2>

                    <p className="muted">
                        All active and historical detections.
                    </p>
                </div>

                <input
                    type="search"
                    placeholder="Search by customer or signature"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    aria-label="Search detection history"
                />
            </div>

            {!filteredDetections.length ? (
                <div className="empty">
                    {search.trim()
                        ? "No detections found."
                        : "No detection history available."}
                </div>
            ) : (
                <div className="table">
                    <table>
                        <thead>
                            <tr>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("id")}>
                                        ID{sortIndicator("id")}
                                    </button>
                                </th>
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
                                    <button className="sort-header" onClick={() => changeSort("status")}>
                                        Status{sortIndicator("status")}
                                    </button>
                                </th>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("created")}>
                                        Created UTC{sortIndicator("created")}
                                    </button>
                                </th>
                                <th>
                                    <button className="sort-header" onClick={() => changeSort("resolution")}>
                                        Resolution{sortIndicator("resolution")}
                                    </button>
                                </th>
                            </tr>
                        </thead>

                        <tbody>
                            {sortedDetections.map((d) => (
                                <tr key={d.id}>
                                    <td>#{d.id}</td>
                                    <td>{d.customerName}</td>
                                    <td>{d.signatureName}</td>
                                    <td>{d.priority}</td>
                                    <td>{DetectionStatus[d.status]}</td>
                                    <td>
                                        {new Date(d.createdAtUtc).toISOString()}
                                    </td>
                                    <td>{d.resolution ?? "-"}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            <div className="status-summary">
                <strong>Status summary</strong>

                <div className="status-counts">
                    <span>Open: {statusCounts[DetectionStatus.Open]}</span>
                    <span>Assigned: {statusCounts[DetectionStatus.Assigned]}</span>
                    <span>Resolved: {statusCounts[DetectionStatus.Resolved]}</span>
                </div>
            </div>
        </section>
    );
}