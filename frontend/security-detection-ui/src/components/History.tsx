import type { Detection } from "../types";
import { DetectionStatus } from "../types";

export function History({
    detections,
}: {
    detections: Detection[];
}) {
    return (
        <section className="card">
            <h2>Detection History</h2>

            <p className="muted">
                All active and historical detections.
            </p>

            <div className="table">
                <table>
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Customer</th>
                            <th>Signature</th>
                            <th>Priority</th>
                            <th>Status</th>
                            <th>Created UTC</th>
                            <th>Resolution</th>
                        </tr>
                    </thead>

                    <tbody>
                        {detections.map((d) => (
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
        </section>
    );
}