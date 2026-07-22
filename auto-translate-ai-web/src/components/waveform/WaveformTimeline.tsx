"use client";

import { ZoomIn, ZoomOut } from "lucide-react";
import { useRef, useState } from "react";

import { Button } from "@/components/ui/Button";
import { cn, formatClock, formatTimecode } from "@/lib/utils";
import type { Segment } from "@/types/job";

const MIN_DURATION = 0.05;
const NUDGE = 0.1;
const ZOOM_LEVELS = [10, 20, 40, 80, 160];
const DEFAULT_ZOOM_INDEX = 2;
const TRACK_HEIGHT = 72;

type DragEdge = "start" | "end";

interface Draft {
  segmentId: string;
  startTime: number;
  endTime: number;
}

export interface WaveformTimelineProps {
  segments: Segment[];
  selectedId: string | null;
  onSelect: (segmentId: string) => void;
  onCommit: (segmentId: string, timing: { startTime: number; endTime: number }) => void;
  savingId: string | null;
  readOnly?: boolean;
  peaks?: number[];
}

export function WaveformTimeline({
  segments,
  selectedId,
  onSelect,
  onCommit,
  savingId,
  readOnly = false,
  peaks,
}: WaveformTimelineProps) {
  const [zoomIndex, setZoomIndex] = useState(DEFAULT_ZOOM_INDEX);
  const [draft, setDraft] = useState<Draft | null>(null);
  const [playhead, setPlayhead] = useState<number | null>(null);
  const trackRef = useRef<HTMLDivElement>(null);

  const pxPerSecond = ZOOM_LEVELS[zoomIndex];
  const duration = segments.length > 0 ? Math.max(...segments.map((s) => s.endTime)) : 0;
  const width = Math.max(duration * pxPerSecond, 320);

  function boundsFor(index: number) {
    const previous = index > 0 ? segments[index - 1].endTime : 0;
    const next = index < segments.length - 1 ? segments[index + 1].startTime : duration;
    return { min: previous, max: next };
  }

  function timeAt(clientX: number) {
    const rect = trackRef.current?.getBoundingClientRect();
    if (!rect) return 0;
    return Math.max(0, (clientX - rect.left) / pxPerSecond);
  }

  function startDrag(event: React.PointerEvent, segment: Segment, index: number, edge: DragEdge) {
    event.preventDefault();
    event.stopPropagation();
    onSelect(segment.id);

    const { min, max } = boundsFor(index);
    const target = event.currentTarget as HTMLElement;
    target.setPointerCapture(event.pointerId);

    let latest: Draft = {
      segmentId: segment.id,
      startTime: segment.startTime,
      endTime: segment.endTime,
    };

    const onMove = (moveEvent: PointerEvent) => {
      const time = timeAt(moveEvent.clientX);
      latest =
        edge === "start"
          ? { ...latest, startTime: clamp(time, min, latest.endTime - MIN_DURATION) }
          : { ...latest, endTime: clamp(time, latest.startTime + MIN_DURATION, max) };
      setDraft(latest);
    };

    const onUp = () => {
      target.removeEventListener("pointermove", onMove);
      target.removeEventListener("pointerup", onUp);
      setDraft(null);
      if (latest.startTime !== segment.startTime || latest.endTime !== segment.endTime) {
        onCommit(segment.id, { startTime: latest.startTime, endTime: latest.endTime });
      }
    };

    target.addEventListener("pointermove", onMove);
    target.addEventListener("pointerup", onUp);
  }

  function startScrub(event: React.PointerEvent) {
    const target = event.currentTarget as HTMLElement;
    target.setPointerCapture(event.pointerId);
    setPlayhead(clamp(timeAt(event.clientX), 0, duration));

    const onMove = (moveEvent: PointerEvent) =>
      setPlayhead(clamp(timeAt(moveEvent.clientX), 0, duration));
    const onUp = () => {
      target.removeEventListener("pointermove", onMove);
      target.removeEventListener("pointerup", onUp);
    };

    target.addEventListener("pointermove", onMove);
    target.addEventListener("pointerup", onUp);
  }

  function nudge(segment: Segment, index: number, edge: DragEdge, delta: number) {
    const { min, max } = boundsFor(index);
    const timing =
      edge === "start"
        ? {
            startTime: clamp(segment.startTime + delta, min, segment.endTime - MIN_DURATION),
            endTime: segment.endTime,
          }
        : {
            startTime: segment.startTime,
            endTime: clamp(segment.endTime + delta, segment.startTime + MIN_DURATION, max),
          };
    onCommit(segment.id, timing);
  }

  if (segments.length === 0) return null;

  return (
    <div className="border-hairline bg-panel rounded-lg border">
      <div className="border-hairline flex items-center justify-between border-b px-3 py-2">
        <div>
          <h2 className="text-fg text-sm font-medium">Timeline</h2>
          <p className="text-muted text-[11px]">
            {readOnly
              ? "Drag across the track to scrub · retiming opens while the job awaits review"
              : `Scrub the track · drag a block’s edge to retime it · arrow keys nudge ${NUDGE}s`}
          </p>
        </div>
        <div className="flex items-center gap-1">
          <span className="timecode text-muted mr-2 text-[11px]">{formatClock(duration)}</span>
          <Button
            size="icon"
            variant="ghost"
            aria-label="Zoom out"
            disabled={zoomIndex === 0}
            onClick={() => setZoomIndex((index) => Math.max(0, index - 1))}
          >
            <ZoomOut aria-hidden />
          </Button>
          <Button
            size="icon"
            variant="ghost"
            aria-label="Zoom in"
            disabled={zoomIndex === ZOOM_LEVELS.length - 1}
            onClick={() => setZoomIndex((index) => Math.min(ZOOM_LEVELS.length - 1, index + 1))}
          >
            <ZoomIn aria-hidden />
          </Button>
        </div>
      </div>

      <div className="overflow-x-auto p-3">
        <div
          ref={trackRef}
          onPointerDown={startScrub}
          className="relative cursor-text"
          style={{ width, height: TRACK_HEIGHT }}
        >
          {peaks && <Peaks peaks={peaks} width={width} height={TRACK_HEIGHT} />}

          {segments.map((segment, index) => {
            const live = draft?.segmentId === segment.id ? draft : segment;
            const selected = segment.id === selectedId;
            const saving = savingId === segment.id;
            const underPlayhead =
              playhead !== null && playhead >= live.startTime && playhead < live.endTime;

            return (
              <div
                key={segment.id}
                onClick={() => onSelect(segment.id)}
                title={`${formatTimecode(live.startTime)} → ${formatTimecode(live.endTime)}`}
                className={cn(
                  "absolute top-0 flex h-full items-center overflow-hidden rounded border",
                  selected
                    ? "border-amber bg-amber/20 z-10"
                    : underPlayhead
                      ? "border-cyan bg-cyan/25"
                      : "border-cyan/40 bg-cyan/10 hover:bg-cyan/20",
                  saving && "opacity-60",
                )}
                style={{
                  left: live.startTime * pxPerSecond,
                  width: Math.max(2, (live.endTime - live.startTime) * pxPerSecond),
                }}
              >
                {!readOnly && (
                  <Handle
                    edge="start"
                    label={`Segment ${segment.segmentIndex + 1} start`}
                    onPointerDown={(event) => startDrag(event, segment, index, "start")}
                    onNudge={(delta) => nudge(segment, index, "start", delta)}
                  />
                )}
                <span className="text-fg/80 pointer-events-none truncate px-2 text-[10px]">
                  {segment.segmentIndex + 1}
                </span>
                {!readOnly && (
                  <Handle
                    edge="end"
                    label={`Segment ${segment.segmentIndex + 1} end`}
                    onPointerDown={(event) => startDrag(event, segment, index, "end")}
                    onNudge={(delta) => nudge(segment, index, "end", delta)}
                  />
                )}
              </div>
            );
          })}

          {playhead !== null && (
            <div
              aria-hidden
              className="bg-amber pointer-events-none absolute top-0 z-20 h-full w-px"
              style={{ left: playhead * pxPerSecond }}
            >
              <span className="timecode bg-amber text-console absolute -top-0.5 left-0 rounded-sm px-1 text-[10px] whitespace-nowrap">
                {formatTimecode(playhead)}
              </span>
            </div>
          )}
        </div>

        <Ruler duration={duration} pxPerSecond={pxPerSecond} width={width} />
      </div>

      {draft ? (
        <p className="timecode text-amber border-hairline border-t px-3 py-1.5 text-[11px]">
          {formatTimecode(draft.startTime)} → {formatTimecode(draft.endTime)} (
          {(draft.endTime - draft.startTime).toFixed(2)}s)
        </p>
      ) : (
        !peaks && (
          <p className="text-muted/60 border-hairline border-t px-3 py-1.5 text-[10px]">
            Waveform and audio playback need an API that serves the job’s audio — the track shows
            segment timings only.
          </p>
        )
      )}
    </div>
  );
}

function Handle({
  edge,
  label,
  onPointerDown,
  onNudge,
}: {
  edge: DragEdge;
  label: string;
  onPointerDown: (event: React.PointerEvent) => void;
  onNudge: (delta: number) => void;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      onPointerDown={onPointerDown}
      onKeyDown={(event) => {
        if (event.key === "ArrowLeft") {
          event.preventDefault();
          onNudge(-NUDGE);
        } else if (event.key === "ArrowRight") {
          event.preventDefault();
          onNudge(NUDGE);
        }
      }}
      className={cn(
        "hover:bg-amber absolute inset-y-0 w-1.5 cursor-ew-resize bg-transparent",
        // 1.5px of outline is invisible; widen and fill on keyboard focus instead.
        "focus-visible:bg-amber focus-visible:w-2 focus-visible:outline-offset-0",
        edge === "start" ? "left-0" : "right-0",
      )}
    />
  );
}

/** Amplitude layer — only rendered once real peaks exist; never faked. */
function Peaks({ peaks, width, height }: { peaks: number[]; width: number; height: number }) {
  const step = width / peaks.length;
  return (
    <svg
      aria-hidden
      width={width}
      height={height}
      className="pointer-events-none absolute inset-0 opacity-40"
    >
      {peaks.map((peak, index) => (
        <rect
          key={index}
          x={index * step}
          y={(height - peak * height) / 2}
          width={Math.max(1, step - 1)}
          height={Math.max(1, peak * height)}
          className="fill-cyan"
        />
      ))}
    </svg>
  );
}

function Ruler({
  duration,
  pxPerSecond,
  width,
}: {
  duration: number;
  pxPerSecond: number;
  width: number;
}) {
  const step = tickStep(pxPerSecond);
  const ticks = Math.floor(duration / step) + 1;

  return (
    <div className="relative mt-1 h-4" style={{ width }}>
      {Array.from({ length: ticks }, (_, index) => index * step).map((time) => (
        <span
          key={time}
          className="timecode text-muted/60 absolute text-[10px]"
          style={{ left: time * pxPerSecond }}
        >
          {formatClock(time)}
        </span>
      ))}
    </div>
  );
}

/** Keep ~60px between labels so they never collide as you zoom. */
function tickStep(pxPerSecond: number) {
  const candidates = [1, 2, 5, 10, 15, 30, 60, 120, 300];
  return candidates.find((step) => step * pxPerSecond >= 60) ?? 600;
}

function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max);
}
