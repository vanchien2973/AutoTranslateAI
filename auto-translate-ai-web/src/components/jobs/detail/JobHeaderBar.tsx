import { X } from "lucide-react";

import { StatusBadge } from "@/components/jobs/StatusBadge";
import { Button } from "@/components/ui/Button";
import { stepLabel } from "@/lib/pipeline";
import { formatClock } from "@/lib/utils";
import { isActive, type JobDetail, type JobStatus, type StepType } from "@/types/job";

function stepPosition(job: JobDetail, currentStep: StepType | null) {
  const total = job.steps.length;
  const index = currentStep ? job.steps.findIndex((step) => step.stepType === currentStep) : -1;
  const done = job.steps.filter((s) => s.status === "Completed" || s.status === "Skipped").length;
  const position = index >= 0 ? index + 1 : Math.min(total, done + 1);
  return { position, total };
}

export function JobHeaderBar({
  job,
  status,
  currentStep,
  percent,
  elapsedSeconds,
  onCancel,
  cancelling,
}: {
  job: JobDetail;
  status: JobStatus;
  currentStep: StepType | null;
  percent: number;
  elapsedSeconds: number;
  onCancel: () => void;
  cancelling: boolean;
}) {
  const active = isActive(status);
  const { position, total } = stepPosition(job, currentStep);

  return (
    <div className="border-hairline bg-panel flex flex-wrap items-center justify-between gap-4 rounded-lg border p-4">
      <div className="min-w-0">
        <h1 className="text-fg truncate text-lg font-semibold tracking-tight">
          Job {job.id.slice(0, 8)}
        </h1>
        <div className="timecode text-muted mt-1.5 flex flex-wrap items-center gap-x-4 gap-y-1 text-[11px]">
          <span>ID: {job.id.slice(0, 8)}</span>
          <span>Created {new Date(job.createdAt).toLocaleString()}</span>
          <span>{job.segmentCount} segments</span>
        </div>
      </div>

      <div className="flex items-center gap-5">
        {active && (
          <div className="text-right">
            <p className="timecode text-fg text-xl leading-none">{formatClock(elapsedSeconds)}</p>
            <p className="text-muted text-[11px]">Elapsed</p>
          </div>
        )}

        <div className="w-44">
          <div className="flex items-center justify-between gap-2">
            <StatusBadge status={status} />
            <span className="timecode text-fg text-sm">{percent}%</span>
          </div>
          <div className="bg-console mt-2 h-1 overflow-hidden rounded-full">
            <div
              className="bg-amber h-full rounded-full transition-[width] duration-500"
              style={{ width: `${percent}%` }}
            />
          </div>
          {active && (
            <p className="text-muted mt-1.5 text-[11px]">
              Step {position} of {total} — {stepLabel(currentStep)}
            </p>
          )}
        </div>

        {active && (
          <Button variant="danger" size="sm" onClick={onCancel} disabled={cancelling}>
            <X aria-hidden />
            {cancelling ? "Cancelling…" : "Cancel job"}
          </Button>
        )}
      </div>
    </div>
  );
}
