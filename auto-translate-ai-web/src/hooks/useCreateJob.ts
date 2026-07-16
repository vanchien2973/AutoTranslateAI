"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { createJob, jobKeys, listAudioLanguages } from "@/lib/api/jobs";
import type { CreateJobInput } from "@/types/job";

export function useCreateJob() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateJobInput) => createJob(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: jobKeys.all }),
  });
}

export function useAudioLanguages() {
  return useQuery({
    queryKey: jobKeys.audioLanguages,
    queryFn: listAudioLanguages,
    staleTime: 10 * 60_000,
  });
}
