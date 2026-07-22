"use client";

import { AlertTriangle, ChevronLeft, ChevronRight, Plus } from "lucide-react";
import Link from "next/link";
import { useState } from "react";

import { StatusBadge } from "@/components/jobs/StatusBadge";
import { buttonVariants } from "@/components/ui/Button";
import { Panel } from "@/components/ui/Panel";
import { useJobs } from "@/hooks/useJobs";
import { stepLabel } from "@/lib/pipeline";
import { cn, formatRelativeTime } from "@/lib/utils";
import { isActive, jobTitle, type JobSummary } from "@/types/job";

const PAGE_SIZES = [20, 50, 100];

export default function AllJobsPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const { data, error, isPending } = useJobs(page, pageSize);

  function changePageSize(size: number) {
    setPageSize(size);
    setPage(1);
  }

  const from = data && data.totalCount > 0 ? (page - 1) * pageSize + 1 : 0;
  const to = data ? Math.min(page * pageSize, data.totalCount) : 0;

  return (
    <div className="mx-auto max-w-6xl space-y-4 px-6 py-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-fg text-xl font-semibold tracking-tight">All jobs</h1>
          <p className="text-muted mt-1 text-sm">
            {data ? `${data.totalCount} job${data.totalCount === 1 ? "" : "s"}, newest first` : "…"}
          </p>
        </div>
        <Link href="/jobs/new" className={buttonVariants({ variant: "primary" })}>
          <Plus aria-hidden />
          New job
        </Link>
      </div>

      {error && (
        <p className="border-red/30 bg-red/5 text-red flex items-start gap-2 rounded-md border p-3 text-sm">
          <AlertTriangle aria-hidden className="mt-0.5 size-4 shrink-0" />
          {error.message}
        </p>
      )}

      {isPending && <p className="text-muted text-sm">Loading jobs…</p>}

      {data && data.items.length === 0 && (
        <div className="border-hairline bg-panel rounded-lg border border-dashed p-12 text-center">
          <h2 className="text-fg text-base font-medium">No jobs yet</h2>
          <p className="text-muted mx-auto mt-1 mb-5 max-w-md text-sm">
            Point the pipeline at a video URL and it will transcribe, translate, and dub it.
          </p>
          <Link href="/jobs/new" className={buttonVariants({ variant: "primary" })}>
            <Plus aria-hidden />
            Create the first job
          </Link>
        </div>
      )}

      {data && data.items.length > 0 && (
        <Panel bodyClassName="p-0">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[52rem] text-sm">
              <thead className="text-muted text-left text-[11px] uppercase">
                <tr className="border-hairline border-b">
                  <th className="px-4 py-2.5 font-medium">Job</th>
                  <th className="w-32 px-4 py-2.5 font-medium">Status</th>
                  <th className="w-40 px-4 py-2.5 font-medium">Step</th>
                  <th className="w-40 px-4 py-2.5 font-medium">Progress</th>
                  <th className="w-32 px-4 py-2.5 font-medium">Created</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((job) => (
                  <JobRow key={job.id} job={job} />
                ))}
              </tbody>
            </table>
          </div>

          <div className="border-hairline text-muted flex flex-wrap items-center justify-between gap-3 border-t px-4 py-2.5 text-xs">
            <div className="flex items-center gap-2">
              <label htmlFor="page-size">Rows</label>
              <select
                id="page-size"
                value={pageSize}
                onChange={(event) => changePageSize(Number(event.target.value))}
                className="border-hairline bg-console text-fg rounded border px-1.5 py-1 text-xs"
              >
                {PAGE_SIZES.map((size) => (
                  <option key={size} value={size}>
                    {size}
                  </option>
                ))}
              </select>
              <span className="timecode ml-2">
                {from}–{to} of {data.totalCount}
              </span>
            </div>

            <Pagination page={page} totalPages={data.totalPages} onChange={setPage} />
          </div>
        </Panel>
      )}
    </div>
  );
}

function JobRow({ job }: { job: JobSummary }) {
  return (
    <tr className="border-hairline/60 hover:bg-console/40 border-b last:border-b-0">
      <td className="px-4 py-3">
        <Link href={`/jobs/${job.id}`} className="text-fg hover:text-cyan block truncate">
          {jobTitle(job)}
        </Link>
        <span className="timecode text-muted/60 text-[11px]">{job.id.slice(0, 8)}</span>
      </td>
      <td className="px-4 py-3">
        <StatusBadge status={job.status} />
      </td>
      <td className="text-muted px-4 py-3 text-xs">{stepLabel(job.currentStep)}</td>
      <td className="px-4 py-3">
        {job.status === "Failed" && job.errorMessage ? (
          <span className="text-red line-clamp-1 text-xs" title={job.errorMessage}>
            {job.errorMessage}
          </span>
        ) : (
          <div className="flex items-center gap-2">
            <div className="bg-console h-1 flex-1 overflow-hidden rounded-full">
              <div
                className={cn(
                  "h-full rounded-full",
                  job.status === "Completed"
                    ? "bg-green"
                    : isActive(job.status)
                      ? "bg-amber"
                      : "bg-slate",
                )}
                style={{ width: `${job.progressPercent}%` }}
              />
            </div>
            <span className="timecode text-muted w-8 shrink-0 text-right text-[11px]">
              {job.progressPercent}%
            </span>
          </div>
        )}
      </td>
      <td className="timecode text-muted px-4 py-3 text-[11px] whitespace-nowrap">
        {formatRelativeTime(job.createdAt)}
      </td>
    </tr>
  );
}

function Pagination({
  page,
  totalPages,
  onChange,
}: {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}) {
  if (totalPages <= 1) return null;

  const pages = pageWindow(page, totalPages);

  return (
    <nav className="flex items-center gap-1" aria-label="Pagination">
      <button
        type="button"
        disabled={page <= 1}
        onClick={() => onChange(page - 1)}
        aria-label="Previous page"
        className="hover:text-fg rounded p-1 disabled:opacity-30"
      >
        <ChevronLeft aria-hidden className="size-4" />
      </button>

      {pages.map((entry, index) =>
        entry === null ? (
          <span key={`gap-${index}`} className="text-muted/40 px-1">
            …
          </span>
        ) : (
          <button
            key={entry}
            type="button"
            onClick={() => onChange(entry)}
            aria-current={entry === page ? "page" : undefined}
            className={cn(
              "timecode min-w-7 rounded px-2 py-1 transition-colors",
              entry === page ? "bg-cyan/15 text-cyan" : "hover:text-fg",
            )}
          >
            {entry}
          </button>
        ),
      )}

      <button
        type="button"
        disabled={page >= totalPages}
        onClick={() => onChange(page + 1)}
        aria-label="Next page"
        className="hover:text-fg rounded p-1 disabled:opacity-30"
      >
        <ChevronRight aria-hidden className="size-4" />
      </button>
    </nav>
  );
}

function pageWindow(page: number, totalPages: number): (number | null)[] {
  const shown = new Set([1, totalPages, page, page - 1, page + 1]);
  const pages = [...shown]
    .filter((value) => value >= 1 && value <= totalPages)
    .sort((a, b) => a - b);

  return pages.flatMap((value, index) =>
    index > 0 && value - pages[index - 1] > 1 ? [null, value] : [value],
  );
}
