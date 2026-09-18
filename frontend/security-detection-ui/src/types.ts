export type Role = "Admin" | "ReadOnly" | "SecurityAnalyst";
export enum DetectionStatus { Open=0, Assigned=1, Resolved=2 }
export interface LoginResponse { token:string; userId:number; username:string; role:Role; }
export interface Customer { id:number; name:string; importance:number; createdAtUtc:string; }
export interface Signature { id:number; name:string; priority:number; conditionsJson:string; isEnabled:boolean; createdAtUtc:string; }
export interface Detection { id:number; customerId:number; customerName:string; signatureId:number; signatureName:string; priority:number; status:DetectionStatus; assignedToUserId?:number|null; incidentPayload:string; createdAtUtc:string; claimedAtUtc?:string|null; resolvedAtUtc?:string|null; resolution?:string|null; }
