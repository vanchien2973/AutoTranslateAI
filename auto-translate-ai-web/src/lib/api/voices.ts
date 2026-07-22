import { apiFetch } from "@/lib/api/client";
import type { VoiceGenderValue } from "@/types/job";

export interface VoiceInfo {
  voiceId: string;
  languageCode: string;
  gender: VoiceGenderValue;
  displayName: string;
}

export function listAudioLanguages() {
  return apiFetch<string[]>("/api/voices/languages");
}

export function listVoices(language: string) {
  return apiFetch<VoiceInfo[]>(`/api/voices?language=${encodeURIComponent(language)}`);
}

export function listTranslationLanguages() {
  return apiFetch<string[]>("/api/languages/translation");
}

export const voiceKeys = {
  languages: ["voices", "languages"] as const,
  translationLanguages: ["languages", "translation"] as const,
  byLanguage: (language: string) => ["voices", "list", language] as const,
};
