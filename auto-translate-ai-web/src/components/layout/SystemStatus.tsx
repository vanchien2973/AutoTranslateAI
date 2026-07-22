"use client";
import { useHealth } from "@/hooks/useHealth";
import type { HealthState } from "@/lib/api/health";
import { cn } from "@/lib/utils";

const DOT: Record<HealthState, string> = {
  Healthy: "bg-green",
  Degraded: "bg-amber",
  Unhealthy: "bg-red",
};

const TEXT: Record<HealthState, string> = {
  Healthy: "text-green",
  Degraded: "text-amber",
  Unhealthy: "text-red",
};

const ROLLUP: Record<HealthState, string> = {
  Healthy: "All systems operational",
  Degraded: "Running with degraded services",
  Unhealthy: "Some services are down",
};

const LABEL: Record<string, string> = {
  postgres: "Database",
  rabbitmq: "Message queue",
  r2: "Storage (R2)",
  "masstransit-bus": "Worker bus",
};

export function SystemStatus({ collapsed = false }: { collapsed?: boolean }) {
  const { data, error, isPending } = useHealth();
  const overall: HealthState = error ? "Unhealthy" : (data?.status ?? "Degraded");
  const summary = error ? "Cannot reach the API" : isPending ? "Checking…" : ROLLUP[overall];

  if (collapsed) {
    return (
      <div className="flex justify-center px-2 py-3">
        <span title={summary} className={cn("size-2 rounded-full", DOT[overall])} />
        <span className="sr-only">{summary}</span>
      </div>
    );
  }

  return (
    <div className="border-hairline bg-console mx-3 mb-3 rounded-lg border p-3">
      <p className="text-fg flex items-center gap-2 text-xs font-medium">
        <span aria-hidden className={cn("size-2 rounded-full", DOT[overall])} />
        System status
      </p>
      <p className="text-muted mt-1 mb-3 text-[11px]">{summary}</p>

      {data && (
        <ul className="space-y-2">
          {data.checks.map((check) => (
            <li key={check.name} className="flex items-center justify-between gap-2 text-[11px]">
              <span className="text-muted truncate" title={check.description ?? undefined}>
                {LABEL[check.name] ?? check.name}
              </span>
              <span className={TEXT[check.status]}>{check.status}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
