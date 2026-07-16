"use client";

import { AlertTriangle } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";

import { Button } from "@/components/ui/Button";
import { Choice } from "@/components/ui/Choice";
import { Dropdown, type DropdownOption } from "@/components/ui/Dropdown";
import { Field } from "@/components/ui/Field";
import { Input } from "@/components/ui/Input";
import { Toggle } from "@/components/ui/Toggle";
import { useAudioLanguages, useCreateJob } from "@/hooks/useCreateJob";
import {
  BgmMode,
  SubtitleMode,
  VoiceGender,
  type BgmModeValue,
  type SubtitleModeValue,
  type VoiceGenderValue,
} from "@/types/job";

const FALLBACK_LANGUAGES = ["vi", "en"];

const languageNames = new Intl.DisplayNames(["en"], { type: "language" });

function languageOptions(codes: string[]): DropdownOption[] {
  return codes.map((code) => ({
    value: code,
    label: languageNames.of(code) ?? code,
    hint: code,
  }));
}

export default function NewJobPage() {
  const router = useRouter();
  const { data: languages } = useAudioLanguages();
  const { mutate, isPending, error } = useCreateJob();

  const [sourceUrl, setSourceUrl] = useState("");
  const [audioLanguage, setAudioLanguage] = useState("vi");
  const [subtitleLanguage, setSubtitleLanguage] = useState("vi");
  const [enableDubbing, setEnableDubbing] = useState(true);
  const [voiceGender, setVoiceGender] = useState<VoiceGenderValue>(VoiceGender.Female);
  const [subtitleMode, setSubtitleMode] = useState<SubtitleModeValue>(SubtitleMode.Softsub);
  const [bgmMode, setBgmMode] = useState<BgmModeValue>(BgmMode.DemucsAI);
  const [urlError, setUrlError] = useState<string | null>(null);

  const options = languageOptions(languages ?? FALLBACK_LANGUAGES);
  const subtitlesOff = subtitleMode === SubtitleMode.None;

  function onSubmit(event: FormEvent) {
    event.preventDefault();

    const trimmed = sourceUrl.trim();
    if (!trimmed) {
      setUrlError("Paste the video URL you want dubbed.");
      return;
    }
    if (!/^https?:\/\//i.test(trimmed)) {
      setUrlError("The pipeline downloads the video over HTTP — the URL must start with http(s).");
      return;
    }
    setUrlError(null);

    mutate(
      {
        sourceUrl: trimmed,
        audioLanguage,
        subtitleLanguage: subtitlesOff ? undefined : subtitleLanguage,
        enableDubbing,
        voiceGender,
        subtitleMode,
        bgmMode,
      },
      { onSuccess: () => router.push("/") },
    );
  }

  return (
    <div className="mx-auto max-w-2xl px-6 py-6">
      <h1 className="text-fg text-xl font-semibold tracking-tight">New job</h1>
      <p className="text-muted mt-1 mb-6 text-sm">
        The pipeline transcribes, translates, and dubs the video, then pauses so you can edit the
        transcript before the render.
      </p>

      <form onSubmit={onSubmit} className="space-y-5">
        <Field
          label="Video URL"
          htmlFor="sourceUrl"
          error={urlError ?? undefined}
          hint="YouTube or any URL yt-dlp can fetch. The source language is detected during transcription."
        >
          <Input
            id="sourceUrl"
            value={sourceUrl}
            onChange={(event) => setSourceUrl(event.target.value)}
            placeholder="https://www.youtube.com/watch?v=…"
            invalid={Boolean(urlError)}
            autoFocus
          />
        </Field>

        <Toggle
          checked={enableDubbing}
          onChange={setEnableDubbing}
          label="Dub the audio"
          hint="Off keeps the original voice track and only adds subtitles."
        />

        <div className="grid gap-5 sm:grid-cols-2">
          <Field
            label="Audio language"
            hint={
              enableDubbing
                ? "Only languages the TTS provider can speak."
                : "Used as the translation target for subtitles."
            }
          >
            <Dropdown options={options} value={audioLanguage} onChange={setAudioLanguage} />
          </Field>

          {enableDubbing && (
            <Field label="Voice">
              <Choice
                name="Voice gender"
                value={voiceGender}
                onChange={setVoiceGender}
                options={[
                  { value: VoiceGender.Female, label: "Female" },
                  { value: VoiceGender.Male, label: "Male" },
                ]}
              />
            </Field>
          )}
        </div>

        <Field label="Subtitles">
          <Choice
            name="Subtitle mode"
            value={subtitleMode}
            onChange={setSubtitleMode}
            options={[
              { value: SubtitleMode.Softsub, label: "Softsub", hint: "Toggleable track" },
              { value: SubtitleMode.Hardsub, label: "Hardsub", hint: "Burned in" },
              { value: SubtitleMode.None, label: "None", hint: "No subtitles" },
            ]}
          />
        </Field>

        {!subtitlesOff && (
          <Field
            label="Subtitle language"
            hint="Can differ from the spoken language — e.g. English audio with Vietnamese subs."
          >
            <Dropdown options={options} value={subtitleLanguage} onChange={setSubtitleLanguage} />
          </Field>
        )}

        <Field
          label="Background music"
          hint="Demucs separates vocals from the music so the original score survives the dub."
        >
          <Choice
            name="BGM mode"
            value={bgmMode}
            onChange={setBgmMode}
            options={[
              { value: BgmMode.DemucsAI, label: "Separate", hint: "Demucs AI" },
              { value: BgmMode.Duck, label: "Duck", hint: "Lower under speech" },
              { value: BgmMode.None, label: "Drop", hint: "Voice only" },
            ]}
          />
        </Field>

        {error && (
          <p className="border-red/30 bg-red/5 text-red flex items-start gap-2 rounded-md border p-3 text-sm">
            <AlertTriangle aria-hidden className="mt-0.5 size-4 shrink-0" />
            {error.message}
          </p>
        )}

        <div className="flex items-center gap-3 pt-1">
          <Button type="submit" disabled={isPending}>
            {isPending ? "Starting…" : "Start processing"}
          </Button>
          <Button type="button" variant="ghost" onClick={() => router.push("/")}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
