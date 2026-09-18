import type { Role } from "../types";

export interface LoginResponse {
    token: string;
    userId: number;
    username: string;
    role: Role;
}
