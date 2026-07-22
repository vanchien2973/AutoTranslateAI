import { apiFetch } from "@/lib/api/client";
import type { PagedResult, Segment, SegmentEdit } from "@/types/job";

const MAX_PAGE_SIZE = 100;

export function listSegments(jobId: string, page = 1, pageSize = MAX_PAGE_SIZE) {
  return apiFetch<PagedResult<Segment>>(
    `/api/jobs/${jobId}/segments?page=${page}&pageSize=${pageSize}`,
  );
}

export async function listAllSegments(jobId: string): Promise<Segment[]> {
  const first = await listSegments(jobId, 1);
  const segments = [...first.items];

  for (let page = 2; page <= first.totalPages; page++) {
    const next = await listSegments(jobId, page);
    segments.push(...next.items);
  }

  return segments;
}

export function updateSegment(jobId: string, segmentId: string, edit: SegmentEdit) {
  return apiFetch<Segment>(`/api/jobs/${jobId}/segments/${segmentId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(edit),
  });
}

export function bulkUpdateSegments(
  jobId: string,
  segments: (SegmentEdit & { segmentId: string })[],
) {
  return apiFetch<Segment[]>(`/api/jobs/${jobId}/segments`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ segments }),
  });
}

export function updateSegmentTiming(
  jobId: string,
  segmentId: string,
  timing: { startTime: number; endTime: number },
) {
  return apiFetch<Segment>(`/api/jobs/${jobId}/segments/${segmentId}/timing`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(timing),
  });
}

export const segmentKeys = {
  all: (jobId: string) => ["segments", jobId] as const,
};
