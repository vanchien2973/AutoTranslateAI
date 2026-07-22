"use client";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createJob, jobKeys } from "@/lib/api/jobs";
import type { CreateJobInput } from "@/types/job";

export function useCreateJob() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateJobInput) => createJob(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: jobKeys.all }),
  });
}
