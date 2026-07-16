"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ApiError } from "@/lib/api/client";
import { listSegments, segmentKeys } from "@/lib/api/segments";

const PAGE_SIZE = 50;

export function useSegments(jobId: string, page: number, enabled: boolean) {
  return useQuery({
    queryKey: segmentKeys.list(jobId, page, PAGE_SIZE),
    queryFn: () => listSegments(jobId, page, PAGE_SIZE),
    enabled,
    placeholderData: keepPreviousData,
    retry: (failureCount, error) =>
      error instanceof ApiError && (error.isUnauthorized || error.status === 404)
        ? false
        : failureCount < 1,
  });
}

export const SEGMENTS_PAGE_SIZE = PAGE_SIZE;
