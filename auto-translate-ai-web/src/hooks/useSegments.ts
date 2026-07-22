"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { ApiError } from "@/lib/api/client";
import {
  bulkUpdateSegments,
  listAllSegments,
  segmentKeys,
  updateSegment,
  updateSegmentTiming,
} from "@/lib/api/segments";
import { jobKeys } from "@/lib/api/jobs";
import type { Segment, SegmentEdit } from "@/types/job";

export function useSegments(jobId: string, enabled = true) {
  return useQuery({
    queryKey: segmentKeys.all(jobId),
    queryFn: () => listAllSegments(jobId),
    enabled,
    retry: (failureCount, error) =>
      error instanceof ApiError && (error.isUnauthorized || error.status === 404)
        ? false
        : failureCount < 1,
  });
}

function replaceSegment(queryClient: ReturnType<typeof useQueryClient>, jobId: string) {
  return (updated: Segment) => {
    queryClient.setQueryData<Segment[]>(segmentKeys.all(jobId), (current) =>
      current?.map((segment) => (segment.id === updated.id ? updated : segment)),
    );
    queryClient.invalidateQueries({ queryKey: jobKeys.detail(jobId) });
  };
}

export function useUpdateSegment(jobId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ segmentId, edit }: { segmentId: string; edit: SegmentEdit }) =>
      updateSegment(jobId, segmentId, edit),
    onSuccess: replaceSegment(queryClient, jobId),
  });
}

export function useBulkUpdateSegments(jobId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (edits: (SegmentEdit & { segmentId: string })[]) =>
      bulkUpdateSegments(jobId, edits),
    onSuccess: (segments: Segment[]) => {
      queryClient.setQueryData<Segment[]>(segmentKeys.all(jobId), segments);
      queryClient.invalidateQueries({ queryKey: jobKeys.detail(jobId) });
    },
  });
}

export function useUpdateSegmentTiming(jobId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      segmentId,
      startTime,
      endTime,
    }: {
      segmentId: string;
      startTime: number;
      endTime: number;
    }) => updateSegmentTiming(jobId, segmentId, { startTime, endTime }),
    onSuccess: replaceSegment(queryClient, jobId),
  });
}

export function isConflict(error: unknown) {
  return error instanceof ApiError && error.status === 409;
}
