"use client";
import { AlertTriangle, ExternalLink, Send, Sparkles } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { Badge, type BadgeTone } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Field } from "@/components/ui/Field";
import { Input } from "@/components/ui/Input";
import { Panel } from "@/components/ui/Panel";
import {
  useChannels,
  useGenerateSeo,
  usePublishJob,
  usePublishResults,
} from "@/hooks/usePublishing";
import { cn, formatRelativeTime } from "@/lib/utils";
import type { JobStatus } from "@/types/job";
import {
  PLATFORM_LABEL,
  PublishStatus,
  type PublishPlatformValue,
  type PublishStatusValue,
} from "@/types/publishing";

const STATUS_TONE: Record<PublishStatusValue, BadgeTone> = {
  [PublishStatus.Pending]: "slate",
  [PublishStatus.Publishing]: "amber",
  [PublishStatus.Published]: "green",
  [PublishStatus.Failed]: "red",
};

const STATUS_LABEL: Record<PublishStatusValue, string> = {
  [PublishStatus.Pending]: "Pending",
  [PublishStatus.Publishing]: "Publishing",
  [PublishStatus.Published]: "Published",
  [PublishStatus.Failed]: "Failed",
};

export function PublishResultsPanel({ jobId, status }: { jobId: string; status: JobStatus }) {
  const publishing = status === "Publishing";
  const { data: results } = usePublishResults(jobId, publishing);
  const [composing, setComposing] = useState(false);

  const anyFailed = results?.some((result) => result.status === PublishStatus.Failed) ?? false;

  return (
    <Panel
      title="Publishing"
      actions={
        status === "Completed" && !composing ? (
          <Button size="sm" variant="secondary" onClick={() => setComposing(true)}>
            <Send aria-hidden />
            Publish now
          </Button>
        ) : undefined
      }
    >
      {anyFailed && (
        <p className="border-red/30 bg-red/5 text-red mb-3 flex items-start gap-2 rounded-md border p-2.5 text-xs">
          <AlertTriangle aria-hidden className="mt-0.5 size-3.5 shrink-0" />
          Some uploads failed. The rendered video is fine — reconnect the account and publish again.
        </p>
      )}

      {composing && <PublishForm jobId={jobId} onDone={() => setComposing(false)} />}

      {!results?.length ? (
        <p className="text-muted text-sm">
          {publishing ? "Uploading…" : "This job hasn’t been published anywhere yet."}
        </p>
      ) : (
        <ul className="space-y-2">
          {results.map((result) => (
            <li
              key={result.id}
              className="border-hairline bg-console flex items-start justify-between gap-3 rounded-md border p-3"
            >
              <div className="min-w-0">
                <p className="text-fg text-sm">
                  {PLATFORM_LABEL[result.platform as PublishPlatformValue]}
                </p>
                {result.url ? (
                  <a
                    href={result.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-cyan mt-0.5 inline-flex items-center gap-1 text-xs hover:underline"
                  >
                    {result.url}
                    <ExternalLink aria-hidden className="size-3" />
                  </a>
                ) : result.error ? (
                  <p className="text-red mt-0.5 text-xs">{result.error}</p>
                ) : null}
                <p className="text-muted/60 mt-0.5 text-[11px]">
                  {formatRelativeTime(result.publishedAt ?? result.createdAt)}
                </p>
              </div>
              <Badge tone={STATUS_TONE[result.status]}>{STATUS_LABEL[result.status]}</Badge>
            </li>
          ))}
        </ul>
      )}
    </Panel>
  );
}

function PublishForm({ jobId, onDone }: { jobId: string; onDone: () => void }) {
  const { data: channels } = useChannels();
  const publish = usePublishJob(jobId);
  const seo = useGenerateSeo(jobId);

  const [selected, setSelected] = useState<string[]>([]);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [tags, setTags] = useState<string[]>([]);

  function submit() {
    const targets = selected.map((connectionId) => {
      const channel = channels!.find((item) => item.id === connectionId)!;
      return {
        platform: channel.platform,
        connectionId,
        title: title.trim() || undefined,
        description: description.trim() || undefined,
        tags: tags.length > 0 ? tags : undefined,
      };
    });

    publish.mutate(targets, { onSuccess: onDone });
  }

  if (!channels?.length) {
    return (
      <p className="border-hairline bg-console text-muted mb-3 rounded-md border p-3 text-xs">
        No accounts connected —{" "}
        <Link href="/settings/keys" className="text-cyan hover:underline">
          connect one first
        </Link>
        .
      </p>
    );
  }

  return (
    <div className="border-hairline bg-console mb-3 space-y-3 rounded-md border p-3">
      <div>
        <span className="text-muted mb-1.5 block text-xs tracking-wide uppercase">Accounts</span>
        <ul className="space-y-1">
          {channels.map((channel) => {
            const isSelected = selected.includes(channel.id);
            return (
              <li key={channel.id}>
                <button
                  type="button"
                  onClick={() =>
                    setSelected((current) =>
                      isSelected
                        ? current.filter((id) => id !== channel.id)
                        : [...current, channel.id],
                    )
                  }
                  className={cn(
                    "w-full truncate rounded px-2 py-1.5 text-left text-xs transition-colors",
                    isSelected ? "bg-cyan/15 text-cyan" : "text-muted hover:text-fg",
                  )}
                >
                  {PLATFORM_LABEL[channel.platform as PublishPlatformValue]} · {channel.channelName}
                </button>
              </li>
            );
          })}
        </ul>
      </div>

      <Field label="Title">
        <Input value={title} maxLength={100} onChange={(event) => setTitle(event.target.value)} />
      </Field>

      <Field label="Description">
        <textarea
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          rows={2}
          className="border-hairline bg-console text-fg focus:border-cyan/60 w-full resize-y rounded border px-2 py-1 text-sm"
        />
      </Field>

      {tags.length > 0 && <p className="text-muted text-[11px]">Tags: {tags.join(", ")}</p>}

      {(publish.error ?? seo.error) && (
        <p className="text-red text-xs">{(publish.error ?? seo.error)?.message}</p>
      )}

      <div className="flex flex-wrap items-center gap-2">
        {/* Needs the transcript, so it only works once Phase 1 has run — which it has by now. */}
        <Button
          size="sm"
          variant="secondary"
          disabled={seo.isPending}
          onClick={() =>
            seo.mutate(undefined, {
              onSuccess: (metadata) => {
                setTitle(metadata.title);
                setDescription(metadata.description);
                setTags(metadata.tags);
              },
            })
          }
        >
          <Sparkles aria-hidden />
          {seo.isPending ? "Writing…" : "Generate with AI"}
        </Button>

        <Button size="sm" onClick={submit} disabled={publish.isPending || selected.length === 0}>
          <Send aria-hidden />
          {publish.isPending ? "Queuing…" : "Publish"}
        </Button>
        <Button size="sm" variant="ghost" onClick={onDone}>
          Cancel
        </Button>
      </div>
    </div>
  );
}
