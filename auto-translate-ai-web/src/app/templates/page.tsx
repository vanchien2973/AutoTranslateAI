"use client";
import { Plus, Trash2 } from "lucide-react";
import Link from "next/link";
import { Badge } from "@/components/ui/Badge";
import { Button, buttonVariants } from "@/components/ui/Button";
import { Panel } from "@/components/ui/Panel";
import { useTemplateStore } from "@/store/templateStore";
import { BgmMode, SubtitleMode, VoiceGender } from "@/types/job";

const SUBTITLE_LABEL: Record<number, string> = {
  [SubtitleMode.None]: "No subtitles",
  [SubtitleMode.Hardsub]: "Hardsub",
  [SubtitleMode.Softsub]: "Softsub",
};

const BGM_LABEL: Record<number, string> = {
  [BgmMode.DemucsAI]: "Separate music",
  [BgmMode.Duck]: "Duck music",
  [BgmMode.None]: "Voice only",
};

export default function TemplatesPage() {
  const templates = useTemplateStore((state) => state.templates);
  const remove = useTemplateStore((state) => state.remove);

  return (
    <div className="mx-auto max-w-3xl space-y-4 px-6 py-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-fg text-xl font-semibold tracking-tight">Templates</h1>
          <p className="text-muted mt-1 text-sm">
            Reusable job settings. Save one from the new-job form; they stay in this browser.
          </p>
        </div>
        <Link href="/jobs/new" className={buttonVariants({ variant: "primary" })}>
          <Plus aria-hidden />
          New job
        </Link>
      </div>

      {templates.length === 0 ? (
        <div className="border-hairline bg-panel rounded-lg border border-dashed p-12 text-center">
          <h2 className="text-fg text-base font-medium">No templates yet</h2>
          <p className="text-muted mx-auto mt-1 mb-5 max-w-md text-sm">
            Set up a job the way you like it, then use “Save as template” on the form to reuse those
            settings next time.
          </p>
          <Link href="/jobs/new" className={buttonVariants({ variant: "primary" })}>
            Set up a job
          </Link>
        </div>
      ) : (
        <Panel>
          <ul className="space-y-2">
            {templates.map((template) => (
              <li
                key={template.id}
                className="border-hairline bg-console flex items-start justify-between gap-3 rounded-md border p-3"
              >
                <div className="min-w-0">
                  <p className="text-fg text-sm">{template.name}</p>
                  <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
                    <Badge tone="slate" dot={false}>
                      {template.enableDubbing
                        ? `Dub ${template.audioLanguage} · ${
                            template.voiceGender === VoiceGender.Female ? "Female" : "Male"
                          }`
                        : "Original audio"}
                    </Badge>
                    <Badge tone="slate" dot={false}>
                      {SUBTITLE_LABEL[template.subtitleMode]}
                      {template.subtitleMode !== SubtitleMode.None &&
                        ` · ${template.subtitleLanguage}`}
                    </Badge>
                    <Badge tone="slate" dot={false}>
                      {BGM_LABEL[template.bgmMode]}
                    </Badge>
                  </div>
                </div>

                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => remove(template.id)}
                  aria-label={`Delete template ${template.name}`}
                >
                  <Trash2 aria-hidden />
                </Button>
              </li>
            ))}
          </ul>
        </Panel>
      )}
    </div>
  );
}
