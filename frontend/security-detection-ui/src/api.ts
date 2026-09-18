import { API_BASE_URL } from "./config";
import type { Customer, Detection, LoginResponse, Signature } from "./types";

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem("token");
  const r = await fetch(`${API_BASE_URL}${path}`, { ...options, headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(options.headers ?? {}) } });
  if (!r.ok) { let m = `Request failed (${r.status}).`; try { const b = await r.json(); m = b.message ?? m; } catch { } throw new Error(m); }
  if (r.status === 204) return undefined as T;
  return r.json();
}
export const login = (username: string, password: string) => request<LoginResponse>("/auth/login", { method: "POST", body: JSON.stringify({ username, password }) });
export const getQueue = () => request<Detection[]>("/detections/queue");
export const getDetections = () => request<Detection[]>("/detections");
export const getCustomers = () => request<Customer[]>("/customers");
export const getSignatures = () => request<Signature[]>("/signatures");
export const claimDetection = (id: number) => request<Detection>(`/detections/${id}/claim`, { method: "POST" });
export const resolveDetection = (id: number, resolution: string) => request<Detection>(`/detections/${id}/resolve`, { method: "POST", body: JSON.stringify({ resolution }) });
export const createCustomer = (name: string, importance: number) => request<Customer>("/customers", { method: "POST", body: JSON.stringify({ name, importance }) });
export const createSignature = (name: string, priority: number, conditionsJson: string) => request<Signature>("/signatures", { method: "POST", body: JSON.stringify({ name, priority, conditionsJson, isEnabled: true }) });
