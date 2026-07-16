"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import type { ConnectionState } from "@/hooks/useJobProgress";
import { ApiError } from "@/lib/api/client";
import { cancelJob, getJob, jobKeys } from "@/lib/api/jobs";
import { isActive, type JobDetail } from "@/types/job";

export function useJob(id: string, connection: ConnectionState) {
  return useQuery({
    queryKey: jobKeys.detail(id),
    queryFn: () => getJob(id),
    retry: (failureCount, error) =>
      error instanceof ApiError && (error.isUnauthorized || error.status === 404)
        ? false
        : failureCount < 2,
    refetchInterval: (query) => pollInterval(query.state.data, connection),
  });
}

function pollInterval(job: JobDetail | undefined, connection: ConnectionState) {
  if (!job || !isActive(job.status)) return false;
  return connection === "connected" ? 15_000 : 4_000;
}

export function useCancelJob(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => cancelJob(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: jobKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: jobKeys.all });
    },
  });
}
