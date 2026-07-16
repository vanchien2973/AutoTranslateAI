import { apiFetch } from "@/lib/api/client";
import type { PagedResult, Segment } from "@/types/job";

export function listSegments(jobId: string, page = 1, pageSize = 50) {
  return apiFetch<PagedResult<Segment>>(
    `/api/jobs/${jobId}/segments?page=${page}&pageSize=${pageSize}`,
  );
}

export const segmentKeys = {
  all: (jobId: string) => ["segments", jobId] as const,
  list: (jobId: string, page: number, pageSize: number) =>
    [...segmentKeys.all(jobId), page, pageSize] as const,
};
