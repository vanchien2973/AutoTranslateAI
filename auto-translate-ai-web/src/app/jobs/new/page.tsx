"use client";
import { AlertTriangle } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import {
  AutoPublishPicker,
  toAutoPublishTargets,
  type AutoPublishSelection,
} from "@/components/jobs/AutoPublishPicker";
import { LogoPicker, type LogoSelection } from "@/components/jobs/LogoPicker";
import { Button } from "@/components/ui/Button";
import { Choice } from "@/components/ui/Choice";
import { Dropdown, type DropdownOption } from "@/components/ui/Dropdown";
import { Field } from "@/components/ui/Field";
import { Input } from "@/components/ui/Input";
import { Toggle } from "@/components/ui/Toggle";
import { useCreateJob } from "@/hooks/useCreateJob";
import { useAudioLanguages, useTranslationLanguages, useVoices } from "@/hooks/useVoices";
import { useTemplateStore } from "@/store/templateStore";
import { useUiStore } from "@/store/uiStore";
import {
  BgmMode,
  SubtitleMode,
  VoiceGender,
  type BgmModeValue,
  type SubtitleModeValue,
  type VoiceGenderValue,
} from "@/types/job";

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
  const { data: audioLanguages, isPending: audioLanguagesPending } = useAudioLanguages();
  const { data: subtitleLanguages, isPending: subtitleLanguagesPending } =
    useTranslationLanguages();
  const { mutate, isPending, error } = useCreateJob();

  const jobDefaults = useUiStore((state) => state.jobDefaults);
  const templates = useTemplateStore((state) => state.templates);
  const saveTemplate = useTemplateStore((state) => state.save);

  const [sourceUrl, setSourceUrl] = useState("");
  const [audioLanguage, setAudioLanguage] = useState(jobDefaults.audioLanguage);
  const [subtitleLanguage, setSubtitleLanguage] = useState(jobDefaults.subtitleLanguage);
  const [enableDubbing, setEnableDubbing] = useState(jobDefaults.enableDubbing);
  const [voiceGender, setVoiceGender] = useState<VoiceGenderValue>(VoiceGender.Female);
  const [subtitleMode, setSubtitleMode] = useState<SubtitleModeValue>(SubtitleMode.Softsub);
  const [bgmMode, setBgmMode] = useState<BgmModeValue>(BgmMode.DemucsAI);
  const [logo, setLogo] = useState<LogoSelection | null>(null);
  const [autoPublish, setAutoPublish] = useState<AutoPublishSelection | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const { data: voices, isPending: voicesPending } = useVoices(audioLanguage, enableDubbing);
  const selectedVoice = voices?.find((voice) => voice.gender === voiceGender);

  const audioOptions = languageOptions(audioLanguages ?? []);
  const subtitleOptions = languageOptions(subtitleLanguages ?? []);
  const subtitlesOn = subtitleMode !== SubtitleMode.None;
  const doesNothing = !enableDubbing && !subtitlesOn;

  function applyTemplate(id: string) {
    const template = templates.find((item) => item.id === id);
    if (!template) return;

    setAudioLanguage(template.audioLanguage);
    setSubtitleLanguage(template.subtitleLanguage);
    setEnableDubbing(template.enableDubbing);
    setVoiceGender(template.voiceGender);
    setSubtitleMode(template.subtitleMode);
    setBgmMode(template.bgmMode);
  }

  function onSaveTemplate() {
    const name = window.prompt("Name this template");
    if (!name?.trim()) return;

    saveTemplate({
      name: name.trim(),
      audioLanguage,
      subtitleLanguage,
      enableDubbing,
      voiceGender,
      subtitleMode,
      bgmMode,
    });
  }

  function onSubmit(event: FormEvent) {
    event.preventDefault();

    const trimmed = sourceUrl.trim();
    if (!trimmed) {
      setFormError("Paste the video URL you want processed.");
      return;
    }
    if (!/^https?:\/\//i.test(trimmed)) {
      setFormError("The pipeline downloads the video over HTTP — the URL must start with http(s).");
      return;
    }
    if (doesNothing) {
      setFormError(
        "Turn on dubbing or add a subtitle track — otherwise this job has nothing to do.",
      );
      return;
    }
    if (enableDubbing && audioLanguages && !audioLanguages.includes(audioLanguage)) {
      setFormError(`No text-to-speech voice is available for ${audioLanguage}.`);
      return;
    }
    if (subtitlesOn && subtitleLanguages && !subtitleLanguages.includes(subtitleLanguage)) {
      setFormError(`Subtitles can't be translated into ${subtitleLanguage}.`);
      return;
    }
    setFormError(null);

    mutate(
      {
        sourceUrl: trimmed,
        audioLanguage: enableDubbing ? audioLanguage : undefined,
        subtitleLanguage: subtitlesOn ? subtitleLanguage : undefined,
        enableDubbing,
        voiceGender: enableDubbing ? voiceGender : undefined,
        subtitleMode,
        bgmMode,
        autoPublishTargets: toAutoPublishTargets(autoPublish),
        logoStorageKey: logo?.storageKey,
        logoPosition: logo?.position,
        logoScalePercent: logo?.scalePercent,
        logoMargin: logo?.margin,
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
        {templates.length > 0 && (
          <Field
            label="Start from a template"
            hint="Fills the settings below; you can still change them."
          >
            <Dropdown
              options={templates.map((template) => ({ value: template.id, label: template.name }))}
              value={null}
              placeholder="Choose a template…"
              onChange={applyTemplate}
            />
          </Field>
        )}

        <Field
          label="Video URL"
          htmlFor="sourceUrl"
          hint="YouTube or any URL yt-dlp can fetch. The spoken language is detected during transcription."
        >
          <Input
            id="sourceUrl"
            value={sourceUrl}
            onChange={(event) => setSourceUrl(event.target.value)}
            placeholder="https://www.youtube.com/watch?v=…"
            invalid={Boolean(formError) && !sourceUrl.trim()}
            autoFocus
          />
        </Field>

        <Toggle
          checked={enableDubbing}
          onChange={setEnableDubbing}
          label="Dub the audio"
          hint="Off keeps the original voice track — only subtitles get translated."
        />

        {enableDubbing ? (
          <div className="grid gap-5 sm:grid-cols-2">
            <Field
              label="Audio language"
              hint={
                audioLanguagesPending ? "Loading…" : "Only languages the TTS provider can speak."
              }
            >
              <Dropdown
                options={audioOptions}
                value={audioLanguage}
                onChange={setAudioLanguage}
                disabled={audioLanguagesPending}
              />
            </Field>

            <Field
              label="Voice"
              hint={
                voicesPending
                  ? "Loading voices…"
                  : (selectedVoice?.displayName ?? "No voice for this language.")
              }
            >
              <Choice
                name="Voice"
                value={voiceGender}
                onChange={setVoiceGender}
                disabled={voicesPending || !voices?.length}
                options={(voices ?? []).map((voice) => ({
                  value: voice.gender,
                  label: voice.gender === VoiceGender.Female ? "Female" : "Male",
                  hint: voice.voiceId.split("-").pop(),
                }))}
              />
            </Field>
          </div>
        ) : (
          <p className="border-hairline bg-panel text-muted rounded-md border p-3 text-xs">
            The original audio is kept as-is, so no voice or audio language is needed.
          </p>
        )}

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

        {subtitlesOn && (
          <Field
            label="Subtitle language"
            hint="Independent of the spoken language — e.g. keep English audio with Vietnamese subs."
          >
            <Dropdown
              options={subtitleOptions}
              value={subtitleLanguage}
              onChange={setSubtitleLanguage}
              disabled={subtitleLanguagesPending}
            />
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

        <LogoPicker value={logo} onChange={setLogo} />

        <AutoPublishPicker value={autoPublish} onChange={setAutoPublish} />

        {(formError ?? error) && (
          <p className="border-red/30 bg-red/5 text-red flex items-start gap-2 rounded-md border p-3 text-sm">
            <AlertTriangle aria-hidden className="mt-0.5 size-4 shrink-0" />
            {formError ?? error?.message}
          </p>
        )}

        <div className="flex items-center gap-3 pt-1">
          <Button type="submit" disabled={isPending || doesNothing}>
            {isPending ? "Starting…" : "Start processing"}
          </Button>
          <Button type="button" variant="secondary" onClick={onSaveTemplate}>
            Save as template
          </Button>
          <Button type="button" variant="ghost" onClick={() => router.push("/")}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
