import { Check, X } from "lucide-react";

import { STEP_META } from "@/lib/pipeline";
import { cn } from "@/lib/utils";
import type { JobStep, StepStatus } from "@/types/job";

function formatDuration(ms: number | null) {
  if (ms == null) return "—";
  if (ms < 1000) return `${ms}ms`;
  const seconds = ms / 1000;
  if (seconds < 60) return `${seconds.toFixed(1)}s`;
  const m = Math.floor(seconds / 60);
  const s = Math.round(seconds % 60);
  return `${m}:${s.toString().padStart(2, "0")}`;
}

const STATUS_LABEL: Record<StepStatus, string> = {
  Completed: "Completed",
  Running: "Processing",
  Failed: "Failed",
  Skipped: "Skipped",
  Pending: "Waiting",
};

function Marker({ status, index }: { status: StepStatus; index: number }) {
  const base = "grid size-7 shrink-0 place-items-center rounded-full border text-xs font-medium";
  switch (status) {
    case "Completed":
      return (
        <span className={cn(base, "border-green/50 bg-green/15 text-green")}>
          <Check aria-hidden className="size-3.5" />
        </span>
      );
    case "Running":
      return (
        <span className={cn(base, "border-amber bg-amber/20 text-amber animate-pulse")}>
          {index}
        </span>
      );
    case "Failed":
      return (
        <span className={cn(base, "border-red/50 bg-red/15 text-red")}>
          <X aria-hidden className="size-3.5" />
        </span>
      );
    default:
      return <span className={cn(base, "border-hairline text-muted/70")}>{index}</span>;
  }
}

export function PipelineStepper({ steps }: { steps: JobStep[] }) {
  if (steps.length === 0) {
    return <p className="text-muted text-sm">No pipeline steps recorded yet.</p>;
  }

  return (
    <ol>
      {steps.map((step, index) => {
        const meta = STEP_META[step.stepType];
        const last = index === steps.length - 1;
        return (
          <li key={step.stepType} className="relative flex gap-3">
            {!last && (
              <span
                aria-hidden
                className={cn(
                  "absolute top-7 left-3.5 h-full w-px -translate-x-1/2",
                  step.status === "Completed" ? "bg-green/30" : "bg-hairline",
                )}
              />
            )}
            <Marker status={step.status} index={index + 1} />
            <div className="flex min-w-0 flex-1 items-start justify-between gap-3 pb-5">
              <div className="min-w-0">
                <p className="text-fg text-sm leading-6">{meta.label}</p>
                <p className={cn("text-xs", step.status === "Failed" ? "text-red" : "text-muted")}>
                  {step.status === "Failed" && step.errorMessage
                    ? step.errorMessage
                    : STATUS_LABEL[step.status]}
                  {step.retryCount > 0 && ` · retry ${step.retryCount}`}
                </p>
              </div>
              <span className="timecode text-muted/70 shrink-0 pt-0.5 text-[11px]">
                {formatDuration(step.durationMs)}
              </span>
            </div>
          </li>
        );
      })}
    </ol>
  );
}
