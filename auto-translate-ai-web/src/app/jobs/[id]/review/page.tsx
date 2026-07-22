"use client";
import { AlertTriangle, ArrowLeft, Check, RefreshCw, Save } from "lucide-react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { ReviewChatPanel } from "@/components/review/ReviewChatPanel";
import { SegmentTable } from "@/components/review/SegmentTable";
import { toEdit, type SegmentDraft } from "@/components/review/SegmentRow";
import { WaveformTimeline } from "@/components/waveform/WaveformTimeline";
import { Button } from "@/components/ui/Button";
import { Panel } from "@/components/ui/Panel";
import { useConfirmJob, useJob } from "@/hooks/useJob";
import { useJobProgress } from "@/hooks/useJobProgress";
import { useVoices } from "@/hooks/useVoices";
import {
  isConflict,
  useBulkUpdateSegments,
  useSegments,
  useUpdateSegment,
  useUpdateSegmentTiming,
} from "@/hooks/useSegments";
import type { SegmentEdit } from "@/types/job";
import type { EditProposal } from "@/types/review";

export default function ReviewPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const live = useJobProgress(id);
  const { data: job, error: jobError, isPending: jobPending } = useJob(id, live.connection);

  const reviewable = job?.status === "AwaitingReview";
  const {
    data: segments,
    error: segmentsError,
    isPending: segmentsPending,
  } = useSegments(id, Boolean(job) && reviewable);

  const updateText = useUpdateSegment(id);
  const updateTiming = useUpdateSegmentTiming(id);
  const bulkUpdate = useBulkUpdateSegments(id);
  const confirm = useConfirmJob(id);

  const { data: voices } = useVoices(job?.audioLanguage ?? "", Boolean(job?.audioLanguage));

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [scrollToIndex, setScrollToIndex] = useState<number | null>(null);
  const [drafts, setDrafts] = useState<Map<string, SegmentDraft>>(new Map());

  useEffect(() => {
    if (job && !reviewable) router.replace(`/jobs/${id}`);
  }, [job, reviewable, router, id]);

  const setDraft = useCallback((segmentId: string, draft: SegmentDraft | undefined) => {
    setDrafts((current) => {
      const next = new Map(current);
      if (draft) next.set(segmentId, draft);
      else next.delete(segmentId);
      return next;
    });
  }, []);

  function saveOne(segmentId: string, edit: SegmentEdit) {
    updateText.mutate({ segmentId, edit }, { onSuccess: () => setDraft(segmentId, undefined) });
  }

  function saveAll() {
    const edits = [...drafts].map(([segmentId, draft]) => ({ segmentId, ...toEdit(draft) }));
    if (edits.length > 0) bulkUpdate.mutate(edits, { onSuccess: () => setDrafts(new Map()) });
  }

  function focusProposal(proposal: EditProposal) {
    setSelectedId(proposal.segmentId);
    const index = segments?.findIndex((segment) => segment.id === proposal.segmentId) ?? -1;
    if (index >= 0) setScrollToIndex(index);
  }

  if (jobPending) return <p className="text-muted px-6 py-6 text-sm">Loading job…</p>;

  if (jobError || !job) {
    return (
      <div className="mx-auto max-w-6xl px-6 py-6">
        <BackLink id={id} />
        <p className="text-red mt-4 flex items-center gap-2 text-sm">
          <AlertTriangle aria-hidden className="size-4" />
          {jobError?.message ?? "Job not found."}
        </p>
      </div>
    );
  }

  if (!reviewable) {
    return (
      <p className="text-muted px-6 py-6 text-sm">This job isn’t awaiting review — redirecting…</p>
    );
  }

  const saveError = updateText.error ?? updateTiming.error ?? bulkUpdate.error;
  const savingId = updateText.isPending
    ? updateText.variables.segmentId
    : updateTiming.isPending
      ? updateTiming.variables.segmentId
      : null;

  return (
    <div className="flex h-full min-h-0 flex-col gap-3 px-4 py-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <BackLink id={id} />
          <h1 className="text-fg mt-1 text-lg font-semibold tracking-tight">
            Review — job {id.slice(0, 8)}
          </h1>
          <p className="text-muted text-xs">
            {job.segmentCount} segments · {job.editedSegmentCount} edited
          </p>
        </div>

        <div className="flex items-center gap-2">
          {drafts.size > 0 && (
            <Button variant="secondary" onClick={saveAll} disabled={bulkUpdate.isPending}>
              <Save aria-hidden />
              {bulkUpdate.isPending ? "Saving…" : `Save all (${drafts.size})`}
            </Button>
          )}
          <Button
            size="lg"
            onClick={() =>
              confirm.mutate(undefined, { onSuccess: () => router.push(`/jobs/${id}`) })
            }
            disabled={confirm.isPending || drafts.size > 0}
            title={drafts.size > 0 ? "Save your edits first" : undefined}
          >
            <Check aria-hidden />
            {confirm.isPending ? "Starting…" : "Confirm & process"}
          </Button>
        </div>
      </div>

      {saveError && <SaveError message={saveError.message} conflict={isConflict(saveError)} />}
      {confirm.error && (
        <SaveError message={confirm.error.message} conflict={isConflict(confirm.error)} />
      )}

      {segments && segments.length > 0 && (
        <WaveformTimeline
          segments={segments}
          selectedId={selectedId}
          onSelect={setSelectedId}
          savingId={updateTiming.isPending ? updateTiming.variables.segmentId : null}
          onCommit={(segmentId, timing) => updateTiming.mutate({ segmentId, ...timing })}
        />
      )}

      <div className="grid min-h-0 flex-1 gap-3 xl:grid-cols-[1fr_360px]">
        <Panel bodyClassName="flex min-h-0 flex-col p-0" className="min-h-0">
          {segmentsError ? (
            <p className="text-red p-8 text-center text-sm">{segmentsError.message}</p>
          ) : segmentsPending || !segments ? (
            <p className="text-muted p-8 text-center text-sm">Loading segments…</p>
          ) : segments.length === 0 ? (
            <p className="text-muted p-8 text-center text-sm">This job has no segments.</p>
          ) : (
            <SegmentTable
              segments={segments}
              drafts={drafts}
              selectedId={selectedId}
              scrollToIndex={scrollToIndex}
              onSelect={setSelectedId}
              onDraftChange={setDraft}
              savingId={savingId}
              onSaveText={saveOne}
              onSaveTiming={(segmentId, timing) => updateTiming.mutate({ segmentId, ...timing })}
              voices={voices ?? []}
              onAssignVoice={(segmentId, assignedVoice) =>
                updateText.mutate({ segmentId, edit: { assignedVoice } })
              }
            />
          )}
        </Panel>

        <ReviewChatPanel jobId={id} onFocusSegment={focusProposal} />
      </div>
    </div>
  );
}

function BackLink({ id }: { id: string }) {
  return (
    <Link
      href={`/jobs/${id}`}
      className="text-muted hover:text-fg inline-flex items-center gap-1.5 text-sm"
    >
      <ArrowLeft aria-hidden className="size-4" />
      Job detail
    </Link>
  );
}

function SaveError({ message, conflict }: { message: string; conflict: boolean }) {
  return (
    <p className="border-red/30 bg-red/5 text-red flex items-center gap-2 rounded-md border p-3 text-sm">
      <AlertTriangle aria-hidden className="size-4 shrink-0" />
      {conflict ? (
        <>
          This segment changed somewhere else since you loaded it. Reload to get the latest text,
          then re-apply your edit.
          <button
            type="button"
            onClick={() => window.location.reload()}
            className="text-fg ml-2 inline-flex items-center gap-1 underline"
          >
            <RefreshCw aria-hidden className="size-3.5" />
            Reload
          </button>
        </>
      ) : (
        message
      )}
    </p>
  );
}
