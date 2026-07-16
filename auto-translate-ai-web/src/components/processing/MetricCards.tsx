import { Sparkline } from "@/components/ui/Sparkline";
import type { MetricHistory } from "@/hooks/useMetricHistory";
import { cn, formatBytes, formatClock } from "@/lib/utils";
import type { JobMetrics } from "@/types/job";

function Card({
  label,
  value,
  sub,
  spark,
  muted = false,
}: {
  label: string;
  value: string;
  sub?: string;
  spark?: React.ReactNode;
  muted?: boolean;
}) {
  return (
    <div className="border-hairline bg-console rounded-lg border p-3">
      <p className="text-muted text-[11px] tracking-wide uppercase">{label}</p>
      <div className="mt-1 flex items-end justify-between gap-2">
        <p className={cn("timecode text-xl leading-none", muted ? "text-muted/60" : "text-fg")}>
          {value}
        </p>
        {spark}
      </div>
      {sub && <p className="text-muted/70 mt-1 text-[11px]">{sub}</p>}
    </div>
  );
}

export function MetricCards({
  metrics,
  history,
  elapsedSeconds,
  percent,
}: {
  metrics: JobMetrics | null;
  history: MetricHistory;
  elapsedSeconds: number;
  percent: number;
}) {
  const eta = percent > 0 && percent < 100 ? (elapsedSeconds * (100 - percent)) / percent : null;

  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
      <Card
        label="CPU"
        value={metrics ? `${metrics.cpuPercent.toFixed(0)}%` : "—"}
        sub={metrics ? undefined : "waiting for worker"}
        muted={!metrics}
        spark={<Sparkline values={history.cpu} color="var(--signal-cyan)" />}
      />
      <Card
        label="RAM"
        value={metrics ? formatBytes(metrics.memoryUsedBytes) : "—"}
        sub={metrics ? `of ${formatBytes(metrics.memoryTotalBytes)}` : "waiting for worker"}
        muted={!metrics}
        spark={<Sparkline values={history.memPercent} color="var(--signal-amber)" />}
      />
      <Card label="GPU" value="N/A" sub="not exposed" muted />
      <Card
        label="ETA"
        value={eta != null ? formatClock(eta) : "—"}
        sub={`elapsed ${formatClock(elapsedSeconds)}`}
      />
    </div>
  );
}
