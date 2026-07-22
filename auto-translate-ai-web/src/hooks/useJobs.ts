"use client";
import { useQuery } from "@tanstack/react-query";
import { ApiError } from "@/lib/api/client";
import { jobKeys, listJobs } from "@/lib/api/jobs";
import { isInFlight } from "@/types/job";

export const OVERVIEW_PAGE_SIZE = 100;

export function useJobs(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: jobKeys.list(page, pageSize),
    queryFn: () => listJobs(page, pageSize),

    refetchInterval: (query) =>
      query.state.data?.items.some((job) => isInFlight(job.status)) ? 5_000 : false,
    retry: (failureCount, error) =>
      error instanceof ApiError && error.isUnauthorized ? false : failureCount < 1,
  });
}
