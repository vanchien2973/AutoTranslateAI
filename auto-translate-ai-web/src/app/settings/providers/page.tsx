"use client";
import { useQuery } from "@tanstack/react-query";
import { AlertTriangle } from "lucide-react";
import { Panel } from "@/components/ui/Panel";
import { useHealth } from "@/hooks/useHealth";
import { apiFetch } from "@/lib/api/client";
import type { HealthState } from "@/lib/api/health";
import { cn } from "@/lib/utils";

interface Providers {
  tts: string;
  speechToText: string;
  translation: string;
  storage: string;
}

const TEXT: Record<HealthState, string> = {
  Healthy: "text-green",
  Degraded: "text-amber",
  Unhealthy: "text-red",
};

const HEALTH_CHECK: Record<keyof Providers, string | null> = {
  tts: null,
  speechToText: null,
  translation: null,
  storage: "r2",
};

const ROLE_LABEL: Record<keyof Providers, string> = {
  speechToText: "Speech to text",
  translation: "Translation",
  tts: "Text to speech",
  storage: "Storage",
};

export default function ProvidersPage() {
  const { data: providers, error } = useQuery({
    queryKey: ["providers"],
    queryFn: () => apiFetch<Providers>("/api/providers"),
    staleTime: 10 * 60_000,
  });
  const { data: health } = useHealth();

  const roles = Object.keys(ROLE_LABEL) as (keyof Providers)[];

  return (
    <div className="mx-auto max-w-3xl space-y-4 px-6 py-6">
      <div>
        <h1 className="text-fg text-xl font-semibold tracking-tight">Providers</h1>
        <p className="text-muted mt-1 text-sm">
          The external services this instance is wired to right now.
        </p>
      </div>

      {error && (
        <p className="border-red/30 bg-red/5 text-red flex items-start gap-2 rounded-md border p-3 text-sm">
          <AlertTriangle aria-hidden className="mt-0.5 size-4 shrink-0" />
          {error.message}
        </p>
      )}

      <Panel>
        <ul className="space-y-2">
          {roles.map((role) => {
            const checkName = HEALTH_CHECK[role];
            const check = checkName ? health?.checks.find((item) => item.name === checkName) : null;

            return (
              <li
                key={role}
                className="border-hairline bg-console flex items-center justify-between gap-3 rounded-md border p-3"
              >
                <div className="min-w-0">
                  <p className="text-muted text-[11px] tracking-wide uppercase">
                    {ROLE_LABEL[role]}
                  </p>
                  <p className="text-fg text-sm">{providers?.[role] ?? "—"}</p>
                </div>
                {check ? (
                  <span
                    className={cn("text-xs", TEXT[check.status])}
                    title={check.description ?? undefined}
                  >
                    {check.status}
                  </span>
                ) : (
                  <span className="text-muted/50 text-xs">no probe</span>
                )}
              </li>
            );
          })}
        </ul>

        <p className="text-muted/70 mt-4 text-xs">
          Providers are chosen at startup from the <code className="timecode">Providers</code>{" "}
          section of <code className="timecode">appsettings.json</code>. To switch one, edit that
          file and restart the API and worker — there is no runtime toggle by design.
        </p>
      </Panel>
    </div>
  );
}
