import { ArrowRight, Check, X } from "lucide-react";

import { Button } from "@/components/ui/Button";
import { diffWords, type DiffPart } from "@/lib/diff";
import { EditTarget, type EditProposal } from "@/types/review";

const TARGET_LABEL: Record<number, string> = {
  [EditTarget.AudioText]: "Dubbed text",
  [EditTarget.SubtitleText]: "Subtitle",
};

function DiffLine({ parts, tone }: { parts: DiffPart[]; tone: "removed" | "added" }) {
  return (
    <p className="text-sm leading-snug">
      {parts.map((part, index) =>
        part.op === "same" ? (
          <span key={index} className="text-muted">
            {part.text}
          </span>
        ) : (
          <span
            key={index}
            className={
              tone === "removed"
                ? "bg-red/15 text-red rounded-sm line-through decoration-1"
                : "bg-green/15 text-green rounded-sm"
            }
          >
            {part.text}
          </span>
        ),
      )}
    </p>
  );
}

export function ProposalCard({
  proposal,
  onApply,
  onDismiss,
  onFocusSegment,
  applying,
}: {
  proposal: EditProposal;
  onApply: () => void;
  onDismiss: () => void;
  onFocusSegment: () => void;
  applying: boolean;
}) {
  const diff = diffWords(proposal.currentText, proposal.proposedText);

  return (
    <div className="border-cyan/30 bg-console rounded-md border p-2.5">
      <button
        type="button"
        onClick={onFocusSegment}
        className="text-muted hover:text-cyan text-[11px] underline-offset-2 hover:underline"
      >
        Segment {proposal.segmentIndex + 1} · {TARGET_LABEL[proposal.target] ?? "Text"}
      </button>

      <div className="mt-2 space-y-1.5">
        <DiffLine parts={diff.before} tone="removed" />
        <div className="text-muted/50 flex items-center gap-1">
          <ArrowRight aria-hidden className="size-3" />
        </div>
        <DiffLine parts={diff.after} tone="added" />
      </div>

      <p className="text-muted mt-2 text-[11px] italic">{proposal.reason}</p>

      <div className="mt-2.5 flex items-center gap-1.5">
        <Button size="sm" onClick={onApply} disabled={applying}>
          <Check aria-hidden />
          {applying ? "Applying…" : "Apply"}
        </Button>
        <Button size="sm" variant="ghost" onClick={onDismiss} disabled={applying}>
          <X aria-hidden />
          Dismiss
        </Button>
      </div>
    </div>
  );
}
