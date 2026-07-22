import Link from "next/link";

import { StatusBadge } from "@/components/jobs/StatusBadge";
import { cn, formatRelativeTime } from "@/lib/utils";
import { isActive, jobTitle, type JobStatus, type JobSummary } from "@/types/job";

const BAR_TONE: Partial<Record<JobStatus, string>> = {
  Failed: "bg-red",
  Completed: "bg-green",
  AwaitingReview: "bg-cyan",
};

export function JobCard({ job }: { job: JobSummary }) {
  const barTone = BAR_TONE[job.status] ?? (isActive(job.status) ? "bg-amber" : "bg-slate");

  return (
    <Link
      href={`/jobs/${job.id}`}
      className="group border-hairline bg-panel hover:border-cyan/40 flex flex-col gap-3 rounded-lg border p-4 transition-colors"
    >
      <div className="flex items-start justify-between gap-3">
        <h3 className="text-fg group-hover:text-cyan line-clamp-2 text-sm leading-snug font-medium">
          {jobTitle(job)}
        </h3>
        <StatusBadge status={job.status} />
      </div>

      <dl className="timecode text-muted flex items-center gap-3 text-[11px]">
        <div>
          <dt className="sr-only">Job id</dt>
          <dd>{job.id.slice(0, 8)}</dd>
        </div>
        <div>
          <dt className="sr-only">Created</dt>
          <dd>{formatRelativeTime(job.createdAt)}</dd>
        </div>
        <div className="ml-auto">
          <dt className="sr-only">Current step</dt>
          <dd>{job.currentStep ?? "—"}</dd>
        </div>
      </dl>

      {job.status === "Failed" && job.errorMessage ? (
        <p className="text-red line-clamp-2 text-xs">{job.errorMessage}</p>
      ) : (
        <div className="space-y-1.5">
          <div className="bg-console h-1 overflow-hidden rounded-full">
            <div
              className={cn("h-full rounded-full transition-[width] duration-500", barTone)}
              style={{ width: `${job.progressPercent}%` }}
            />
          </div>
          <p className="timecode text-muted text-[11px]">{job.progressPercent}%</p>
        </div>
      )}
    </Link>
  );
}
