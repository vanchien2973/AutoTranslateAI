"use client";

import { Loader2, Send, Sparkles } from "lucide-react";
import { useEffect, useLayoutEffect, useRef, useState, type FormEvent } from "react";

import { ProposalCard } from "@/components/review/ProposalCard";
import { Badge } from "@/components/ui/Badge";
import { Panel } from "@/components/ui/Panel";
import { useChatHistory, useReviewChat } from "@/hooks/useReviewChat";
import { cn } from "@/lib/utils";
import { ChatRole, type EditProposal } from "@/types/review";

export function ReviewChatPanel({
  jobId,
  onFocusSegment,
  canEdit = true,
}: {
  jobId: string;
  onFocusSegment: (proposal: EditProposal) => void;
  canEdit?: boolean;
}) {
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage } = useChatHistory(jobId, true);
  const { proposals, chat, apply, dismiss } = useReviewChat(jobId);
  const [draft, setDraft] = useState("");
  const [pending, setPending] = useState<string | null>(null);

  const scrollRef = useRef<HTMLDivElement>(null);
  const topSentinelRef = useRef<HTMLDivElement>(null);
  const bottomRef = useRef<HTMLDivElement>(null);
  const heightBeforePrepend = useRef<number | null>(null);
  const messageCount = data?.messages.length ?? 0;

  useEffect(() => {
    const sentinel = topSentinelRef.current;
    if (!sentinel || !hasNextPage) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting || isFetchingNextPage) return;
        heightBeforePrepend.current = scrollRef.current?.scrollHeight ?? null;
        fetchNextPage();
      },
      { root: scrollRef.current, rootMargin: "80px" },
    );

    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  useLayoutEffect(() => {
    const element = scrollRef.current;
    const previousHeight = heightBeforePrepend.current;
    if (!element || previousHeight === null) return;

    element.scrollTop += element.scrollHeight - previousHeight;
    heightBeforePrepend.current = null;
  }, [messageCount]);

  useEffect(() => {
    if (heightBeforePrepend.current !== null) return;
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [proposals, chat.isPending, pending]);

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    const message = draft.trim();
    if (!message || chat.isPending) return;
    setDraft("");
    setPending(message);
    chat.mutate(message, { onSettled: () => setPending(null) });
  }

  return (
    <Panel
      className="flex min-h-0 flex-col"
      bodyClassName="flex min-h-0 flex-1 flex-col p-0"
      title="AI Review Assistant"
      actions={
        <Badge tone="cyan" dot={false} className="text-[10px]">
          Beta
        </Badge>
      }
    >
      <div ref={scrollRef} className="min-h-0 flex-1 space-y-3 overflow-auto p-3">
        <div ref={topSentinelRef} />

        {isFetchingNextPage && (
          <p className="text-muted flex items-center justify-center gap-1.5 text-[11px]">
            <Loader2 aria-hidden className="size-3 animate-spin" />
            Loading older messages…
          </p>
        )}

        {messageCount === 0 && !chat.isPending && (
          <div className="text-muted flex flex-col items-center gap-2 py-8 text-center">
            <Sparkles aria-hidden className="text-muted/50 size-6" />
            <p className="text-fg text-sm">
              {canEdit ? "Ask about the translation" : "No conversation yet"}
            </p>
            <p className="text-xs">
              {canEdit
                ? "It reads the transcript and suggests edits you can apply one by one."
                : "The assistant opens while the job is awaiting review."}
            </p>
          </div>
        )}

        {data?.messages.map((message, index) => (
          <div
            key={index}
            className={cn(
              "max-w-[85%] rounded-lg px-3 py-2 text-sm",
              message.role === ChatRole.User ? "bg-cyan/15 text-fg ml-auto" : "bg-console text-fg",
            )}
          >
            {message.content}
          </div>
        ))}

        {pending && (
          <div className="bg-cyan/15 text-fg ml-auto max-w-[85%] rounded-lg px-3 py-2 text-sm">
            {pending}
          </div>
        )}

        {chat.isPending && <p className="text-muted text-xs">Thinking…</p>}

        {chat.error && <p className="text-red text-xs">{chat.error.message}</p>}

        {proposals.map((proposal) => (
          <ProposalCard
            key={proposal.proposalId}
            proposal={proposal}
            applying={apply.isPending && apply.variables === proposal.proposalId}
            onApply={() => apply.mutate(proposal.proposalId)}
            onDismiss={() => dismiss(proposal.proposalId)}
            onFocusSegment={() => onFocusSegment(proposal)}
          />
        ))}

        {apply.error && <p className="text-red text-xs">{apply.error.message}</p>}

        <div ref={bottomRef} />
      </div>

      {canEdit ? (
        <>
          <form
            onSubmit={onSubmit}
            className="border-hairline flex items-center gap-2 border-t p-3"
          >
            <input
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              placeholder="Ask the assistant about this translation…"
              className="border-hairline bg-console text-fg placeholder:text-muted/60 focus:border-cyan/60 h-9 flex-1 rounded-md border px-3 text-sm"
            />
            <button
              type="submit"
              disabled={chat.isPending || !draft.trim()}
              aria-label="Send"
              className="bg-cyan text-console grid size-9 shrink-0 place-items-center rounded-md disabled:opacity-40"
            >
              <Send aria-hidden className="size-4" />
            </button>
          </form>

          <p className="text-muted/60 px-3 pb-2 text-[10px]">
            The assistant can be wrong — check its suggestions before applying.
          </p>
        </>
      ) : (
        <p className="text-muted border-hairline border-t px-3 py-2.5 text-[11px]">
          Editing is closed — the assistant only runs while the job is awaiting review.
        </p>
      )}
    </Panel>
  );
}
