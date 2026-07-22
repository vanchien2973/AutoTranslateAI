"use client";

import { motion, useReducedMotion } from "framer-motion";
import { Check, Clock, Undo2, X } from "lucide-react";
import { useState } from "react";

import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import type { VoiceInfo } from "@/lib/api/voices";
import { cn, formatTimecode } from "@/lib/utils";
import type { Segment, SegmentEdit } from "@/types/job";

/** Draft text for one row. Absent fields mean "untouched" — they are never sent to the API. */
export interface SegmentDraft {
  audioText?: string;
  subtitleText?: string;
}

export interface SegmentRowProps {
  segment: Segment;
  draft: SegmentDraft | undefined;
  selected: boolean;
  voices: VoiceInfo[];
  onSelect: () => void;
  onDraftChange: (draft: SegmentDraft | undefined) => void;
  onSaveText: (edit: SegmentEdit) => void;
  onSaveTiming: (timing: { startTime: number; endTime: number }) => void;
  onAssignVoice: (voiceId: string) => void;
  saving: boolean;
}

export function SegmentRow({
  segment,
  draft,
  selected,
  voices,
  onSelect,
  onDraftChange,
  onSaveText,
  onSaveTiming,
  onAssignVoice,
  saving,
}: SegmentRowProps) {
  const [timingOpen, setTimingOpen] = useState(false);
  const reducedMotion = useReducedMotion();

  const audioText = draft?.audioText ?? segment.ttsText;
  const subtitleText = draft?.subtitleText ?? segment.subtitleText;
  const dirty = draft !== undefined;

  function edit(next: SegmentDraft) {
    const merged: SegmentDraft = {
      audioText: next.audioText ?? draft?.audioText,
      subtitleText: next.subtitleText ?? draft?.subtitleText,
    };

    if (merged.audioText === segment.ttsText) delete merged.audioText;
    if (merged.subtitleText === segment.subtitleText) delete merged.subtitleText;

    onDraftChange(
      merged.audioText === undefined && merged.subtitleText === undefined ? undefined : merged,
    );
  }

  return (
    <motion.div
      layout
      transition={{ duration: reducedMotion ? 0 : 0.15 }}
      onClick={onSelect}
      className={cn(
        "border-hairline/60 grid cursor-pointer gap-3 border-b px-3 py-2.5",
        "grid-cols-1 lg:grid-cols-[2.5rem_7rem_1fr_1fr_6rem_5.5rem]",
        selected ? "bg-console" : "hover:bg-console/40",
      )}
    >
      {/* Below lg this is one compact meta row; `lg:contents` dissolves it so the grid columns line
          up again, and lg:order-* restores the column order the header expects. */}
      <div className="flex flex-wrap items-center gap-3 lg:contents">
        <span className="text-muted timecode text-xs lg:order-1 lg:pt-2">
          {segment.segmentIndex + 1}
        </span>

        <div className="lg:order-2 lg:pt-1.5">
          <button
            type="button"
            onClick={(event) => {
              event.stopPropagation();
              setTimingOpen((open) => !open);
            }}
            className="timecode text-muted hover:text-cyan flex items-center gap-1 text-[11px]"
            title="Adjust timing"
          >
            <Clock aria-hidden className="size-3" />
            {formatTimecode(segment.startTime)}
          </button>
          <p className="timecode text-muted/60 mt-0.5 text-[11px]">
            {formatTimecode(segment.endTime)}
          </p>
          {timingOpen && (
            <TimingEditor
              segment={segment}
              saving={saving}
              onCancel={() => setTimingOpen(false)}
              onSave={(timing) => {
                onSaveTiming(timing);
                setTimingOpen(false);
              }}
            />
          )}
        </div>

        <div className="lg:order-5 lg:pt-1.5" onClick={(event) => event.stopPropagation()}>
          <select
            value={segment.assignedVoice ?? ""}
            onChange={(event) => onAssignVoice(event.target.value)}
            disabled={saving || voices.length === 0}
            aria-label={`Voice for segment ${segment.segmentIndex + 1}`}
            className="border-hairline bg-console text-fg focus:border-cyan/60 w-full rounded border px-1.5 py-1 text-[11px] disabled:opacity-50"
          >
            {/* Empty value clears the assignment: the API falls back to the job's default voice. */}
            <option value="">Default</option>
            {voices.map((voice) => (
              <option key={voice.voiceId} value={voice.voiceId}>
                {voice.gender === 0 ? "Female" : "Male"} · {voice.voiceId.split("-").pop()}
              </option>
            ))}
          </select>
          {segment.speakerLabel && (
            <p className="text-muted/70 mt-1 truncate text-[10px]">{segment.speakerLabel}</p>
          )}
        </div>

        <div className="lg:order-6 lg:pt-1.5">
          {segment.isEdited ? <Badge tone="cyan">Edited</Badge> : <Badge tone="slate">AI</Badge>}
          {segment.needsTtsRegenerate && <p className="text-amber mt-1 text-[10px]">re-synth</p>}
        </div>
      </div>

      <p className="text-muted text-sm leading-snug lg:order-3 lg:pt-1.5">{segment.originalText}</p>

      <div className="space-y-1.5 lg:order-4" onClick={(event) => event.stopPropagation()}>
        <textarea
          value={audioText}
          onChange={(event) => edit({ audioText: event.target.value })}
          rows={2}
          aria-label={`Dubbed text for segment ${segment.segmentIndex + 1}`}
          className="border-hairline bg-console text-cyan focus:border-cyan/60 w-full resize-y rounded border px-2 py-1 text-sm"
        />
        <textarea
          value={subtitleText}
          onChange={(event) => edit({ subtitleText: event.target.value })}
          rows={1}
          aria-label={`Subtitle text for segment ${segment.segmentIndex + 1}`}
          className="border-hairline bg-console text-fg focus:border-cyan/60 w-full resize-y rounded border px-2 py-1 text-xs"
        />
        {dirty && (
          <div className="flex items-center gap-1.5">
            <Button size="sm" onClick={() => onSaveText(toEdit(draft))} disabled={saving}>
              <Check aria-hidden />
              Save
            </Button>
            <Button size="sm" variant="ghost" onClick={() => onDraftChange(undefined)}>
              <Undo2 aria-hidden />
              Reset
            </Button>
          </div>
        )}
      </div>
    </motion.div>
  );
}

export function toEdit(draft: SegmentDraft): SegmentEdit {
  return {
    ...(draft.audioText !== undefined && { audioTextEdited: draft.audioText }),
    ...(draft.subtitleText !== undefined && { subtitleTextEdited: draft.subtitleText }),
  };
}

function TimingEditor({
  segment,
  saving,
  onSave,
  onCancel,
}: {
  segment: Segment;
  saving: boolean;
  onSave: (timing: { startTime: number; endTime: number }) => void;
  onCancel: () => void;
}) {
  const [start, setStart] = useState(segment.startTime.toFixed(3));
  const [end, setEnd] = useState(segment.endTime.toFixed(3));

  const startValue = Number(start);
  const endValue = Number(end);
  const invalid =
    !Number.isFinite(startValue) || !Number.isFinite(endValue) || endValue <= startValue;

  return (
    <div
      className="border-hairline bg-panel mt-2 space-y-1.5 rounded border p-2"
      onClick={(event) => event.stopPropagation()}
    >
      <label className="text-muted block text-[10px] uppercase">Start (s)</label>
      <input
        value={start}
        onChange={(event) => setStart(event.target.value)}
        className="timecode border-hairline bg-console text-fg w-full rounded border px-1.5 py-1 text-[11px]"
      />
      <label className="text-muted block text-[10px] uppercase">End (s)</label>
      <input
        value={end}
        onChange={(event) => setEnd(event.target.value)}
        className="timecode border-hairline bg-console text-fg w-full rounded border px-1.5 py-1 text-[11px]"
      />
      {invalid && <p className="text-red text-[10px]">End must be greater than start.</p>}
      <div className="flex gap-1">
        <Button
          size="sm"
          disabled={invalid || saving}
          onClick={() => onSave({ startTime: startValue, endTime: endValue })}
        >
          <Check aria-hidden />
        </Button>
        <Button size="sm" variant="ghost" onClick={onCancel}>
          <X aria-hidden />
        </Button>
      </div>
    </div>
  );
}
