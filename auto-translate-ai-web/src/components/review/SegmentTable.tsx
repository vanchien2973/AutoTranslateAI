"use client";
import { useVirtualizer } from "@tanstack/react-virtual";
import { useEffect, useRef } from "react";
import { SegmentRow, type SegmentDraft } from "@/components/review/SegmentRow";
import type { VoiceInfo } from "@/lib/api/voices";
import type { Segment, SegmentEdit } from "@/types/job";

const ESTIMATED_ROW_HEIGHT = 108;

export interface SegmentTableProps {
  segments: Segment[];
  drafts: Map<string, SegmentDraft>;
  selectedId: string | null;
  scrollToIndex: number | null;
  voices: VoiceInfo[];
  onSelect: (segmentId: string) => void;
  onDraftChange: (segmentId: string, draft: SegmentDraft | undefined) => void;
  onSaveText: (segmentId: string, edit: SegmentEdit) => void;
  onSaveTiming: (segmentId: string, timing: { startTime: number; endTime: number }) => void;
  onAssignVoice: (segmentId: string, voiceId: string) => void;
  savingId: string | null;
}

export function SegmentTable({
  segments,
  drafts,
  selectedId,
  scrollToIndex,
  voices,
  onSelect,
  onDraftChange,
  onSaveText,
  onSaveTiming,
  onAssignVoice,
  savingId,
}: SegmentTableProps) {
  const scrollRef = useRef<HTMLDivElement>(null);

  const virtualizer = useVirtualizer({
    count: segments.length,
    getScrollElement: () => scrollRef.current,
    estimateSize: () => ESTIMATED_ROW_HEIGHT,
    overscan: 8,
    getItemKey: (index) => segments[index].id,
  });

  const scrollTo = virtualizer.scrollToIndex;
  useEffect(() => {
    if (scrollToIndex != null) scrollTo(scrollToIndex, { align: "center" });
  }, [scrollToIndex, scrollTo]);

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="border-hairline text-muted hidden gap-3 border-b px-3 py-2 text-[11px] uppercase lg:grid lg:grid-cols-[2.5rem_7rem_1fr_1fr_6rem_5.5rem]">
        <span>#</span>
        <span>Time</span>
        <span>Source</span>
        <span>Dub / subtitle</span>
        <span>Voice</span>
        <span>State</span>
      </div>

      <div ref={scrollRef} className="min-h-0 flex-1 overflow-auto">
        <div style={{ height: virtualizer.getTotalSize(), position: "relative" }}>
          {virtualizer.getVirtualItems().map((item) => {
            const segment = segments[item.index];
            return (
              <div
                key={item.key}
                ref={virtualizer.measureElement}
                data-index={item.index}
                style={{
                  position: "absolute",
                  top: 0,
                  left: 0,
                  width: "100%",
                  transform: `translateY(${item.start}px)`,
                }}
              >
                <SegmentRow
                  segment={segment}
                  draft={drafts.get(segment.id)}
                  selected={segment.id === selectedId}
                  voices={voices}
                  onSelect={() => onSelect(segment.id)}
                  onDraftChange={(draft) => onDraftChange(segment.id, draft)}
                  onSaveText={(edit) => onSaveText(segment.id, edit)}
                  onSaveTiming={(timing) => onSaveTiming(segment.id, timing)}
                  onAssignVoice={(voiceId) => onAssignVoice(segment.id, voiceId)}
                  saving={savingId === segment.id}
                />
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
