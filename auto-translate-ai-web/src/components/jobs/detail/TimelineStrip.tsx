import { Play } from "lucide-react";

import { Panel } from "@/components/ui/Panel";

const BARS = Array.from({ length: 120 }, (_, i) => 0.2 + Math.abs(Math.sin(i * 0.7)) * 0.8);

export function TimelineStrip() {
  return (
    <Panel title="Timeline">
      <div className="flex items-center gap-3">
        <button
          type="button"
          disabled
          aria-label="Play (available in Phase 3)"
          className="border-hairline text-muted/50 grid size-9 shrink-0 cursor-not-allowed place-items-center rounded-full border"
        >
          <Play aria-hidden className="size-4" />
        </button>

        <div className="flex h-12 flex-1 items-center gap-px overflow-hidden opacity-40">
          {BARS.map((height, index) => (
            <span
              key={index}
              className="bg-cyan w-full rounded-full"
              style={{ height: `${height * 100}%` }}
            />
          ))}
        </div>
      </div>
      <p className="text-muted/70 mt-2 text-[11px]">
        Playback and scrubbing arrive with the review screen in Phase 3.
      </p>
    </Panel>
  );
}
