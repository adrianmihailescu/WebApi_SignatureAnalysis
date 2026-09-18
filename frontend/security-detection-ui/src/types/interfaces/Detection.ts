import type { DetectionStatus } from "../enums/DetectionStatus";

export interface Detection {
    id: number;
    customerId: number;
    customerName: string;
    signatureId: number;
    signatureName: string;
    priority: number;
    status: DetectionStatus;
    assignedToUserId?: number | null;
    incidentPayload: string;
    createdAtUtc: string;
    claimedAtUtc?: string | null;
    resolvedAtUtc?: string | null;
    resolution?: string | null;
}
