"use client";

import Link from "next/link";

import { Field } from "@/components/ui/Field";
import { Input } from "@/components/ui/Input";
import { Toggle } from "@/components/ui/Toggle";
import { useChannels } from "@/hooks/usePublishing";
import { cn } from "@/lib/utils";
import { PLATFORM_LABEL, type PublishPlatformValue } from "@/types/publishing";

export interface AutoPublishSelection {
  channels: { connectionId: string; platform: PublishPlatformValue }[];
  title: string;
  description: string;
}

export function toAutoPublishTargets(selection: AutoPublishSelection | null) {
  if (!selection || selection.channels.length === 0) return undefined;

  return selection.channels.map((channel) => ({
    platform: channel.platform,
    connectionId: channel.connectionId,
    title: selection.title.trim() || undefined,
    description: selection.description.trim() || undefined,
  }));
}

export function AutoPublishPicker({
  value,
  onChange,
}: {
  value: AutoPublishSelection | null;
  onChange: (value: AutoPublishSelection | null) => void;
}) {
  const { data: channels, isPending } = useChannels();
  const enabled = value !== null;

  function toggleChannel(connectionId: string, platform: PublishPlatformValue) {
    if (!value) return;
    const selected = value.channels.some((channel) => channel.connectionId === connectionId);
    onChange({
      ...value,
      channels: selected
        ? value.channels.filter((channel) => channel.connectionId !== connectionId)
        : [...value.channels, { connectionId, platform }],
    });
  }

  return (
    <div className="space-y-3">
      <Toggle
        checked={enabled}
        onChange={(on) => onChange(on ? { channels: [], title: "", description: "" } : null)}
        label="Publish when finished"
        hint="Posts the rendered video automatically once the job completes."
      />

      {enabled &&
        (isPending ? (
          <p className="text-muted text-xs">Loading connected accounts…</p>
        ) : !channels?.length ? (
          <p className="border-hairline bg-panel text-muted rounded-md border p-3 text-xs">
            No accounts connected yet —{" "}
            <Link href="/settings/keys" className="text-cyan hover:underline">
              connect one in publishing keys
            </Link>
            .
          </p>
        ) : (
          <div className="border-hairline bg-console space-y-3 rounded-md border p-3">
            <div>
              <span className="text-muted mb-1.5 block text-xs tracking-wide uppercase">
                Accounts
              </span>
              <ul className="space-y-1">
                {channels.map((channel) => {
                  const selected = value.channels.some((item) => item.connectionId === channel.id);
                  return (
                    <li key={channel.id}>
                      <button
                        type="button"
                        onClick={() => toggleChannel(channel.id, channel.platform)}
                        className={cn(
                          "flex w-full items-center justify-between gap-2 rounded px-2 py-1.5 text-left text-xs transition-colors",
                          selected ? "bg-cyan/15 text-cyan" : "text-muted hover:text-fg",
                        )}
                      >
                        <span className="truncate">
                          {PLATFORM_LABEL[channel.platform as PublishPlatformValue]} ·{" "}
                          {channel.channelName}
                        </span>
                        {channel.isExpired && <span className="text-red">expired</span>}
                      </button>
                    </li>
                  );
                })}
              </ul>
            </div>

            <Field
              label="Title"
              hint="Left blank, the video is posted under a generated name. Max 100 characters."
            >
              <Input
                value={value.title}
                maxLength={100}
                onChange={(event) => onChange({ ...value, title: event.target.value })}
                placeholder="Video title on the platform"
              />
            </Field>

            <Field label="Description">
              <textarea
                value={value.description}
                onChange={(event) => onChange({ ...value, description: event.target.value })}
                rows={2}
                className="border-hairline bg-console text-fg focus:border-cyan/60 w-full resize-y rounded border px-2 py-1 text-sm"
              />
            </Field>
          </div>
        ))}
    </div>
  );
}
