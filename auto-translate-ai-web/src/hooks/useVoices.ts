"use client";

import { useQuery } from "@tanstack/react-query";

import {
  listAudioLanguages,
  listTranslationLanguages,
  listVoices,
  voiceKeys,
} from "@/lib/api/voices";

const CATALOG_STALE_TIME = 10 * 60_000;

export function useAudioLanguages() {
  return useQuery({
    queryKey: voiceKeys.languages,
    queryFn: listAudioLanguages,
    staleTime: CATALOG_STALE_TIME,
  });
}

export function useTranslationLanguages() {
  return useQuery({
    queryKey: voiceKeys.translationLanguages,
    queryFn: listTranslationLanguages,
    staleTime: CATALOG_STALE_TIME,
  });
}

export function useVoices(language: string, enabled: boolean) {
  return useQuery({
    queryKey: voiceKeys.byLanguage(language),
    queryFn: () => listVoices(language),
    enabled: enabled && Boolean(language),
    staleTime: CATALOG_STALE_TIME,
  });
}
