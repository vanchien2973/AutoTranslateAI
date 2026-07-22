"use client";
import { Dropdown } from "@/components/ui/Dropdown";
import { Field } from "@/components/ui/Field";
import { Panel } from "@/components/ui/Panel";
import { Toggle } from "@/components/ui/Toggle";
import { useAudioLanguages, useTranslationLanguages } from "@/hooks/useVoices";
import { useUiStore } from "@/store/uiStore";

const languageNames = new Intl.DisplayNames(["en"], { type: "language" });

export default function PreferencesPage() {
  const forceReducedMotion = useUiStore((state) => state.forceReducedMotion);
  const setForceReducedMotion = useUiStore((state) => state.setForceReducedMotion);
  const jobDefaults = useUiStore((state) => state.jobDefaults);
  const setJobDefaults = useUiStore((state) => state.setJobDefaults);

  const { data: audioLanguages } = useAudioLanguages();
  const { data: subtitleLanguages } = useTranslationLanguages();

  const options = (codes: string[] | undefined) =>
    (codes ?? []).map((code) => ({
      value: code,
      label: languageNames.of(code) ?? code,
      hint: code,
    }));

  return (
    <div className="mx-auto max-w-2xl space-y-4 px-6 py-6">
      <div>
        <h1 className="text-fg text-xl font-semibold tracking-tight">Preferences</h1>
        <p className="text-muted mt-1 text-sm">
          These live in this browser only — they are not shared with other machines or users.
        </p>
      </div>

      <Panel title="Motion">
        <Toggle
          checked={forceReducedMotion}
          onChange={setForceReducedMotion}
          label="Reduce motion"
          hint="Stops the 3D visualizer and the pulsing status dots. Your OS setting already does this; use the switch to turn it on just here."
        />
      </Panel>

      <Panel title="New job defaults" subtitle="What the create-job form starts with.">
        <div className="space-y-4">
          <Toggle
            checked={jobDefaults.enableDubbing}
            onChange={(enableDubbing) => setJobDefaults({ enableDubbing })}
            label="Dub the audio by default"
            hint="Off starts new jobs as subtitle-only."
          />

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Audio language">
              <Dropdown
                options={options(audioLanguages)}
                value={jobDefaults.audioLanguage}
                onChange={(audioLanguage) => setJobDefaults({ audioLanguage })}
              />
            </Field>
            <Field label="Subtitle language">
              <Dropdown
                options={options(subtitleLanguages)}
                value={jobDefaults.subtitleLanguage}
                onChange={(subtitleLanguage) => setJobDefaults({ subtitleLanguage })}
              />
            </Field>
          </div>
        </div>
      </Panel>
    </div>
  );
}
