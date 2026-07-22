import { apiFetch } from "@/lib/api/client";

export interface UsageByKey {
  key: string;
  costUsd: number;
  callCount: number;
  inputUnits: number;
  outputUnits: number;
}

export interface UsageByDay {
  date: string;
  costUsd: number;
  callCount: number;
}

export interface UsageSummary {
  totalCostUsd: number;
  callCount: number;
  byProvider: UsageByKey[];
  byOperation: UsageByKey[];
  byDay: UsageByDay[];
}

export function getUsage(days: number) {
  return apiFetch<{ days: number; summary: UsageSummary }>(`/api/usage?days=${days}`);
}

export const usageKeys = { summary: (days: number) => ["usage", days] as const };
