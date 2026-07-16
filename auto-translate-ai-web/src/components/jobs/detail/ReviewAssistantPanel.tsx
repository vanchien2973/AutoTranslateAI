import { Send, Sparkles } from "lucide-react";

import { Badge } from "@/components/ui/Badge";
import { Panel } from "@/components/ui/Panel";

export function ReviewAssistantPanel() {
  return (
    <Panel
      className="min-h-[320px]"
      bodyClassName="flex flex-col p-0"
      title="AI Review Assistant"
      actions={
        <Badge tone="cyan" dot={false} className="text-[10px]">
          Beta
        </Badge>
      }
    >
      <div className="flex flex-1 flex-col items-center justify-center gap-2 px-6 text-center">
        <Sparkles aria-hidden className="text-muted/50 size-6" />
        <p className="text-fg text-sm">Chat assistant arrives in Phase 3</p>
        <p className="text-muted text-xs">
          It will suggest edits to the transcript and apply them on your approval.
        </p>
      </div>

      <div className="border-hairline flex items-center gap-2 border-t p-3">
        <input
          disabled
          placeholder="Ask the assistant about this translation…"
          className="border-hairline bg-console text-muted/60 h-9 flex-1 rounded-md border px-3 text-sm"
        />
        <button
          type="button"
          disabled
          aria-label="Send"
          className="bg-cyan/40 text-console grid size-9 shrink-0 cursor-not-allowed place-items-center rounded-md"
        >
          <Send aria-hidden className="size-4" />
        </button>
      </div>
    </Panel>
  );
}
