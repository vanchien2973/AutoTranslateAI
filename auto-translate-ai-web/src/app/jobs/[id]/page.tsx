"use client";

import { AlertTriangle, ArrowLeft } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";

import { PipelineStepper } from "@/components/jobs/PipelineStepper";
import { JobHeaderBar } from "@/components/jobs/detail/JobHeaderBar";
import { JobInfoPanel } from "@/components/jobs/detail/JobInfoPanel";
import { ReviewAssistantPanel } from "@/components/jobs/detail/ReviewAssistantPanel";
import { SegmentsPanel } from "@/components/jobs/detail/SegmentsPanel";
import { TimelineStrip } from "@/components/jobs/detail/TimelineStrip";
import { VisualizerPanel } from "@/components/jobs/detail/VisualizerPanel";
import { Panel } from "@/components/ui/Panel";
import { useCancelJob, useJob } from "@/hooks/useJob";
import { useJobProgress } from "@/hooks/useJobProgress";
import { isActive, type JobStatus, type StepType } from "@/types/job";

export default function JobDetailPage() {
  const { id } = useParams<{ id: string }>();
  const live = useJobProgress(id);
  const { data: job, error, isPending } = useJob(id, live.connection);
  const cancel = useCancelJob(id);

  const status: JobStatus | undefined = live.progress?.status ?? job?.status;
  const currentStep: StepType | null = live.progress?.currentStep ?? job?.currentStep ?? null;
  const percent = live.progress?.progressPercent ?? job?.progressPercent ?? 0;
  const active = status ? isActive(status) : false;
  const elapsed = useElapsed(job?.startedAt ?? job?.createdAt ?? null, active);

  if (isPending) {
    return <p className="text-muted px-6 py-6 text-sm">Loading job…</p>;
  }

  if (error || !job || !status) {
    return (
      <div className="mx-auto max-w-6xl px-6 py-6">
        <BackLink />
        <p className="text-red mt-4 flex items-center gap-2 text-sm">
          <AlertTriangle aria-hidden className="size-4" />
          {error?.message ?? "Job not found."}
        </p>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-[1400px] space-y-4 px-4 py-4 xl:px-6">
      <div className="flex items-center justify-between">
        <BackLink />
        <ConnectionDot state={live.connection} />
      </div>

      <JobHeaderBar
        job={job}
        status={status}
        currentStep={currentStep}
        percent={percent}
        elapsedSeconds={elapsed}
        onCancel={() => cancel.mutate()}
        cancelling={cancel.isPending}
      />

      <div className="grid gap-4 xl:grid-cols-[300px_1fr_300px]">
        <Panel title="Pipeline">
          <PipelineStepper steps={job.steps} />
        </Panel>

        <VisualizerPanel
          currentStep={currentStep}
          percent={percent}
          metrics={live.metrics}
          elapsedSeconds={elapsed}
          active={active}
        />

        <JobInfoPanel job={job} />
      </div>

      <TimelineStrip />

      <div className="grid gap-4 xl:grid-cols-[1fr_360px]">
        <SegmentsPanel jobId={id} enabled={job.segmentCount > 0} />
        <ReviewAssistantPanel />
      </div>

      {status === "Failed" && job.errorMessage && (
        <p className="border-red/30 bg-red/5 text-red flex items-start gap-2 rounded-md border p-3 text-sm">
          <AlertTriangle aria-hidden className="mt-0.5 size-4 shrink-0" />
          {job.errorMessage}
        </p>
      )}
    </div>
  );
}

function BackLink() {
  return (
    <Link href="/" className="text-muted hover:text-fg inline-flex items-center gap-1.5 text-sm">
      <ArrowLeft aria-hidden className="size-4" />
      All jobs
    </Link>
  );
}

function ConnectionDot({ state }: { state: ReturnType<typeof useJobProgress>["connection"] }) {
  const connected = state === "connected";
  const label =
    state === "connected"
      ? "Live"
      : state === "reconnecting"
        ? "Reconnecting…"
        : state === "connecting"
          ? "Connecting…"
          : "Offline — polling";

  return (
    <span
      className={
        connected
          ? "text-green flex items-center gap-1.5 text-[11px]"
          : "text-muted flex items-center gap-1.5 text-[11px]"
      }
    >
      <span
        className={connected ? "bg-green size-1.5 rounded-full" : "bg-muted size-1.5 rounded-full"}
      />
      {label}
    </span>
  );
}

/** Ticks once a second while active so elapsed/ETA stay live without a server round-trip. */
function useElapsed(start: string | null, active: boolean) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (!active) return;
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [active]);

  if (!start) return 0;
  return Math.max(0, (now - new Date(start).getTime()) / 1000);
}
