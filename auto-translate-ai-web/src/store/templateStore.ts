import { create } from "zustand";
import { persist } from "zustand/middleware";

import type { BgmModeValue, SubtitleModeValue, VoiceGenderValue } from "@/types/job";

export interface JobTemplate {
  id: string;
  name: string;
  audioLanguage: string;
  subtitleLanguage: string;
  enableDubbing: boolean;
  voiceGender: VoiceGenderValue;
  subtitleMode: SubtitleModeValue;
  bgmMode: BgmModeValue;
}

export type JobTemplateInput = Omit<JobTemplate, "id">;

interface TemplateState {
  templates: JobTemplate[];
  save: (template: JobTemplateInput) => void;
  remove: (id: string) => void;
}

export const useTemplateStore = create<TemplateState>()(
  persist(
    (set) => ({
      templates: [],
      save: (template) =>
        set((state) => ({
          templates: [
            ...state.templates.filter((existing) => existing.name !== template.name),
            { ...template, id: crypto.randomUUID() },
          ],
        })),
      remove: (id) =>
        set((state) => ({ templates: state.templates.filter((template) => template.id !== id) })),
    }),
    { name: "ata.templates" },
  ),
);
