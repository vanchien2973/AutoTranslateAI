"use client";

import { AlertTriangle, Plus, RefreshCw } from "lucide-react";
import Link from "next/link";

import { JobCard } from "@/components/jobs/JobCard";
import { Button, buttonVariants } from "@/components/ui/Button";
import { useJobs } from "@/hooks/useJobs";
import { ApiError, API_BASE_URL } from "@/lib/api/client";

export default function DashboardPage() {
  const { data, error, isPending, isFetching, refetch } = useJobs();

  return (
    <div className="mx-auto max-w-6xl px-6 py-6">
      <div className="mb-6 flex items-end justify-between gap-4">
        <div>
          <h1 className="text-fg text-xl font-semibold tracking-tight">Jobs</h1>
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

      {isPending && <JobGridSkeleton />}

      {error && <ErrorState error={error} onRetry={() => refetch()} />}

      {data && data.items.length === 0 && <EmptyState />}

      {data && data.items.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {data.items.map((job) => (
            <JobCard key={job.id} job={job} />
          ))}
        </div>
      )}
    </div>
  );
}

function JobGridSkeleton() {
  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {[0, 1, 2].map((index) => (
        <div
          key={index}
          className="border-hairline bg-panel h-[124px] animate-pulse rounded-lg border"
        />
      ))}
    </div>
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
