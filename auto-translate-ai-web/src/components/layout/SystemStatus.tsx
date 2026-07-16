import { cn } from "@/lib/utils";

type Health = "healthy" | "degraded" | "down";

const HEALTH_DOT: Record<Health, string> = {
  healthy: "bg-green",
  degraded: "bg-amber",
  down: "bg-red",
};

const HEALTH_TEXT: Record<Health, string> = {
  healthy: "text-green",
  degraded: "text-amber",
  down: "text-red",
};

const HEALTH_LABEL: Record<Health, string> = {
  healthy: "Healthy",
  degraded: "Degraded",
  down: "Down",
};

const SERVICES: { name: string; health: Health }[] = [
  { name: "API Server", health: "healthy" },
  { name: "TTS (Azure)", health: "healthy" },
  { name: "LLM (OpenAI)", health: "healthy" },
  { name: "Storage (R2)", health: "healthy" },
];

function rollup(services: { health: Health }[]): Health {
  if (services.some((service) => service.health === "down")) return "down";
  if (services.some((service) => service.health === "degraded")) return "degraded";
  return "healthy";
}

const ROLLUP_LABEL: Record<Health, string> = {
  healthy: "All systems operational",
  degraded: "Degraded performance",
  down: "Service outage",
};

export function SystemStatus({ collapsed = false }: { collapsed?: boolean }) {
  const overall = rollup(SERVICES);

  if (collapsed) {
    return (
      <div className="flex justify-center px-2 py-3">
        <span
          title={ROLLUP_LABEL[overall]}
          className={cn("size-2 rounded-full", HEALTH_DOT[overall])}
        />
        <span className="sr-only">{ROLLUP_LABEL[overall]}</span>
      </div>
    );
  }

  return (
    <div className="border-hairline bg-console mx-3 mb-3 rounded-lg border p-3">
      <p className="text-fg flex items-center gap-2 text-xs font-medium">
        <span aria-hidden className={cn("size-2 rounded-full", HEALTH_DOT[overall])} />
        System status
      </p>
      <p className="text-muted mt-1 mb-3 text-[11px]">{ROLLUP_LABEL[overall]}</p>

      <ul className="space-y-2">
        {SERVICES.map((service) => (
          <li key={service.name} className="flex items-center justify-between gap-2 text-[11px]">
            <span className="text-muted">{service.name}</span>
            <span className={HEALTH_TEXT[service.health]}>{HEALTH_LABEL[service.health]}</span>
          </li>
        ))}
      </ul>

      <p className="text-muted/60 mt-3 text-[11px]">Sample data — health checks not wired yet.</p>
    </div>
  );
}
