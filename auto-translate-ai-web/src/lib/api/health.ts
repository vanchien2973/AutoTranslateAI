import { API_BASE_URL } from "@/lib/api/client";

export type HealthState = "Healthy" | "Degraded" | "Unhealthy";

export interface HealthCheck {
  name: string;
  status: HealthState;
  description: string | null;
  error: string | null;
}

export interface HealthReport {
  status: HealthState;
  totalDurationMs: number;
  checks: HealthCheck[];
}

export async function getHealth(): Promise<HealthReport> {
  const response = await fetch(`${API_BASE_URL}/health/ready`, {
    headers: { Accept: "application/json" },
  });
  if (!response.ok && response.status !== 503) {
    throw new Error(`Health check failed with status ${response.status}.`);
  }
  return (await response.json()) as HealthReport;
}

export const healthKeys = { ready: ["health", "ready"] as const };
