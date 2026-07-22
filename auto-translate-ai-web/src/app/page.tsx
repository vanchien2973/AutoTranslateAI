"use client";

import { AlertTriangle, ArrowRight, Plus, RefreshCw } from "lucide-react";
import Link from "next/link";

import { JobCard } from "@/components/jobs/JobCard";
import { Button, buttonVariants } from "@/components/ui/Button";
import { Panel } from "@/components/ui/Panel";
import { OVERVIEW_PAGE_SIZE, useJobs } from "@/hooks/useJobs";
import { ApiError, API_BASE_URL } from "@/lib/api/client";
import { stepLabel } from "@/lib/pipeline";
import { formatRelativeTime } from "@/lib/utils";
import { isActive, jobTitle, type JobSummary } from "@/types/job";

const RECENT_COUNT = 6;

export default function DashboardPage() {
  const { data, error, isPending, isFetching, refetch } = useJobs(1, OVERVIEW_PAGE_SIZE);

  const jobs = data?.items ?? [];
  const needsReview = jobs.filter((job) => job.status === "AwaitingReview");
  const running = jobs.filter((job) => isActive(job.status));
  const failed = jobs.filter((job) => job.status === "Failed");
  // Counts describe what was actually fetched — say so when there is more behind it.
  const partial = data ? data.totalCount > jobs.length : false;

  return (
    <div className="mx-auto max-w-6xl space-y-4 px-6 py-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-fg text-xl font-semibold tracking-tight">Dashboard</h1>
          <p className="text-muted mt-1 text-sm">
            {data
              ? `${data.totalCount} job${data.totalCount === 1 ? "" : "s"} · ${API_BASE_URL}`
              : API_BASE_URL}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="icon"
            aria-label="Refresh"
            onClick={() => refetch()}
            disabled={isFetching}
          >
            <RefreshCw aria-hidden className={isFetching ? "animate-spin" : undefined} />
          </Button>
          <Link href="/jobs/new" className={buttonVariants({ variant: "primary" })}>
            <Plus aria-hidden />
            New job
          </Link>
        </div>
      </div>

      {error && <ErrorState error={error} onRetry={() => refetch()} />}

      {isPending && (
        <div className="grid gap-3 sm:grid-cols-4">
          {[0, 1, 2, 3].map((index) => (
            <div
              key={index}
              className="border-hairline bg-panel h-20 animate-pulse rounded-lg border"
            />
          ))}
        </div>
      )}

      {data && data.items.length === 0 && <EmptyState />}

      {data && data.items.length > 0 && (
        <>
          <div className="grid gap-3 sm:grid-cols-4">
            <Tile label="Needs review" value={needsReview.length} tone="text-cyan" />
            <Tile label="Running" value={running.length} tone="text-amber" />
            <Tile
              label="Completed"
              value={jobs.filter((job) => job.status === "Completed").length}
              tone="text-green"
            />
            <Tile label="Failed" value={failed.length} tone="text-red" />
          </div>

          {partial && (
            <p className="text-muted/70 text-[11px]">
              Counts cover the {jobs.length} most recent jobs of {data.totalCount}.
            </p>
          )}

          {/* The review pause is the whole point of the pipeline — surface it above everything else. */}
          {needsReview.length > 0 && (
            <Panel
              title="Waiting for you"
              subtitle="These paused after translation so you can check the transcript."
            >
              <ul className="space-y-2">
                {needsReview.map((job) => (
                  <li
                    key={job.id}
                    className="border-hairline bg-console flex items-center justify-between gap-3 rounded-md border p-3"
                  >
                    <div className="min-w-0">
                      <p className="text-fg truncate text-sm">{jobTitle(job)}</p>
                      <p className="timecode text-muted mt-0.5 text-[11px]">
                        ready {formatRelativeTime(job.reviewReadyAt ?? job.createdAt)}
                      </p>
                    </div>
                    <Link
                      href={`/jobs/${job.id}/review`}
                      className={buttonVariants({ variant: "primary", size: "sm" })}
                    >
                      Review
                      <ArrowRight aria-hidden />
                    </Link>
                  </li>
                ))}
              </ul>
            </Panel>
          )}

          {running.length > 0 && (
            <Panel title="Running now">
              <ul className="space-y-2">
                {running.map((job) => (
                  <RunningRow key={job.id} job={job} />
                ))}
              </ul>
            </Panel>
          )}

          <Panel
            title="Recent jobs"
            actions={
              <Link href="/jobs" className="text-cyan text-xs hover:underline">
                View all
              </Link>
            }
          >
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {jobs.slice(0, RECENT_COUNT).map((job) => (
                <JobCard key={job.id} job={job} />
              ))}
            </div>
          </Panel>
        </>
      )}
    </div>
  );
}

function Tile({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <div className="border-hairline bg-panel rounded-lg border p-3">
      <p className="text-muted text-[11px] tracking-wide uppercase">{label}</p>
      <p className={`timecode mt-1 text-2xl ${value === 0 ? "text-muted/40" : tone}`}>{value}</p>
    </div>
  );
}

function RunningRow({ job }: { job: JobSummary }) {
  return (
    <li className="border-hairline bg-console rounded-md border p-3">
      <div className="flex items-center justify-between gap-3">
        <Link href={`/jobs/${job.id}`} className="text-fg hover:text-cyan min-w-0 truncate text-sm">
          {jobTitle(job)}
        </Link>
        <span className="timecode text-muted shrink-0 text-[11px]">
          {stepLabel(job.currentStep)} · {job.progressPercent}%
        </span>
      </div>
      <div className="bg-panel mt-2 h-1 overflow-hidden rounded-full">
        <div
          className="bg-amber h-full rounded-full transition-[width] duration-500"
          style={{ width: `${job.progressPercent}%` }}
        />
      </div>
    </li>
  );
}

function EmptyState() {
  return (
    <div className="border-hairline bg-panel rounded-lg border border-dashed p-12 text-center">
      <h2 className="text-fg text-base font-medium">No jobs yet</h2>
      <p className="text-muted mx-auto mt-1 mb-5 max-w-md text-sm">
        Point the pipeline at a video URL and it will transcribe, translate, and dub it — pausing
        for your review before the render.
      </p>
      <Link href="/jobs/new" className={buttonVariants({ variant: "primary" })}>
        <Plus aria-hidden />
        Create the first job
      </Link>
    </div>
  );
}

function ErrorState({ error, onRetry }: { error: Error; onRetry: () => void }) {
  const unauthorized = error instanceof ApiError && error.isUnauthorized;

  return (
    <div className="border-red/30 bg-red/5 rounded-lg border p-6">
      <p className="text-red flex items-center gap-2 text-sm font-medium">
        <AlertTriangle aria-hidden className="size-4" />
        {unauthorized ? "The API rejected this key" : "Could not load jobs"}
      </p>
      <p className="text-muted mt-2 text-sm">{error.message}</p>
      {unauthorized && (
        <p className="text-muted mt-2 text-sm">
          Set <code className="timecode text-fg">NEXT_PUBLIC_API_KEY</code> in{" "}
          <code className="timecode text-fg">.env.local</code> to the admin password the API is
          configured with, then restart the dev server.
        </p>
      )}
      <Button variant="secondary" size="sm" className="mt-4" onClick={onRetry}>
        Try again
      </Button>
    </div>
  );
}
