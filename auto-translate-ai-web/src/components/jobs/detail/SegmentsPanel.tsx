"use client";
import Link from "next/link";
import { useState } from "react";
import { Badge, type BadgeTone } from "@/components/ui/Badge";
import { buttonVariants } from "@/components/ui/Button";
import { Panel } from "@/components/ui/Panel";
import { useSegments } from "@/hooks/useSegments";
import { cn, formatTimecode } from "@/lib/utils";
import type { Segment } from "@/types/job";

const TABS = ["Segments", "Translations", "Pronunciation", "Media"] as const;
type Tab = (typeof TABS)[number];

const PREVIEW_ROWS = 100;

function segmentStatus(segment: Segment): { label: string; tone: BadgeTone } {
  if (segment.isEdited) return { label: "Edited", tone: "cyan" };
  if (segment.subtitleText && segment.subtitleText !== segment.originalText) {
    return { label: "Translated", tone: "green" };
  }
  return { label: "Pending", tone: "slate" };
}

export function SegmentsPanel({
  jobId,
  enabled,
  reviewable,
}: {
  jobId: string;
  enabled: boolean;
  reviewable: boolean;
}) {
  const [tab, setTab] = useState<Tab>("Segments");
  const { data, isPending, error } = useSegments(jobId, enabled);
  const showsTable = tab === "Segments" || tab === "Translations";

  return (
    <Panel
      bodyClassName="p-0"
      actions={
        reviewable && (
          <Link
            href={`/jobs/${jobId}/review`}
            className={buttonVariants({ variant: "primary", size: "sm" })}
          >
            Open review
          </Link>
        )
      }
    >
      <div className="border-hairline flex items-center gap-1 border-b px-2">
        {TABS.map((item) => (
          <button
            key={item}
            type="button"
            onClick={() => setTab(item)}
            className={cn(
              "relative px-3 py-2.5 text-sm transition-colors",
              tab === item ? "text-cyan" : "text-muted hover:text-fg",
            )}
          >
            {item}
            {tab === item && <span className="bg-cyan absolute inset-x-3 -bottom-px h-0.5" />}
          </button>
        ))}
      </div>

      {!showsTable ? (
        <p className="text-muted p-8 text-center text-sm">{tab} view isn’t built yet.</p>
      ) : !enabled ? (
        <p className="text-muted p-8 text-center text-sm">
          Segments appear here after transcription.
        </p>
      ) : error ? (
        <p className="text-red p-8 text-center text-sm">{error.message}</p>
      ) : isPending || !data ? (
        <p className="text-muted p-8 text-center text-sm">Loading segments…</p>
      ) : (
        <PreviewTable segments={data} translationsFirst={tab === "Translations"} jobId={jobId} />
      )}
    </Panel>
  );
}

function PreviewTable({
  segments,
  translationsFirst,
  jobId,
}: {
  segments: Segment[];
  translationsFirst: boolean;
  jobId: string;
}) {
  const rows = segments.slice(0, PREVIEW_ROWS);

  return (
    <>
      <div className="max-h-[420px] overflow-auto">
        <table className="w-full text-sm">
          <thead className="bg-panel text-muted sticky top-0 z-10 text-left text-[11px] uppercase">
            <tr className="border-hairline border-b">
              <th className="w-10 px-3 py-2 font-medium">#</th>
              <th className="px-3 py-2 font-medium">Start</th>
              <th className="px-3 py-2 font-medium">End</th>
              <th className="px-3 py-2 font-medium">
                {translationsFirst ? "Translation" : "Source"}
              </th>
              <th className="px-3 py-2 font-medium">
                {translationsFirst ? "Source" : "Translation"}
              </th>
              <th className="px-3 py-2 font-medium">Speaker</th>
              <th className="px-3 py-2 font-medium">Status</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((segment) => {
              const status = segmentStatus(segment);
              const source = segment.originalText;
              const translation = segment.subtitleText || segment.ttsText;
              return (
                <tr key={segment.id} className="border-hairline/60 hover:bg-console/40 border-b">
                  <td className="text-muted px-3 py-2.5 align-top">{segment.segmentIndex + 1}</td>
                  <td className="timecode text-muted px-3 py-2.5 align-top text-xs whitespace-nowrap">
                    {formatTimecode(segment.startTime)}
                  </td>
                  <td className="timecode text-muted px-3 py-2.5 align-top text-xs whitespace-nowrap">
                    {formatTimecode(segment.endTime)}
                  </td>
                  <td className="text-fg max-w-xs px-3 py-2.5 align-top">
                    {translationsFirst ? translation : source}
                  </td>
                  <td
                    className={cn(
                      "max-w-xs px-3 py-2.5 align-top",
                      translationsFirst ? "text-fg" : "text-cyan",
                    )}
                  >
                    {translationsFirst ? source : translation}
                  </td>
                  <td className="text-muted px-3 py-2.5 align-top text-xs">
                    {segment.speakerLabel ?? "—"}
                  </td>
                  <td className="px-3 py-2.5 align-top">
                    <Badge tone={status.tone}>{status.label}</Badge>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <div className="border-hairline text-muted flex items-center justify-between border-t px-4 py-2.5 text-xs">
        <span className="timecode">
          {rows.length} of {segments.length}
        </span>
        <Link href={`/jobs/${jobId}/review`} className="text-cyan hover:underline">
          Open review to edit
        </Link>
      </div>
    </>
  );
}
