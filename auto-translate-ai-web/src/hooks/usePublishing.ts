"use client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError } from "@/lib/api/client";
import { jobKeys } from "@/lib/api/jobs";
import {
  connectChannel,
  generateSeo,
  listChannels,
  listCredentials,
  listPublishResults,
  publishingKeys,
  publishJob,
  setCredential,
} from "@/lib/api/publishing";
import type { PublishPlatformValue, PublishTarget } from "@/types/publishing";

export function useCredentials() {
  return useQuery({
    queryKey: publishingKeys.credentials,
    queryFn: listCredentials,
    retry: (count, error) => !(error instanceof ApiError && error.isUnauthorized) && count < 1,
  });
}

export function useSetCredential() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: {
      platform: PublishPlatformValue;
      clientId: string;
      clientSecret: string;
      defaultRedirectUri?: string | null;
    }) => setCredential(input.platform, input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: publishingKeys.credentials }),
  });
}

export function useChannels() {
  return useQuery({
    queryKey: publishingKeys.channels,
    queryFn: listChannels,
    retry: (count, error) => !(error instanceof ApiError && error.isUnauthorized) && count < 1,
  });
}

export function useConnectChannel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: connectChannel,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: publishingKeys.channels }),
  });
}

export function usePublishResults(jobId: string, publishing: boolean) {
  return useQuery({
    queryKey: publishingKeys.results(jobId),
    queryFn: () => listPublishResults(jobId),
    refetchInterval: publishing ? 5_000 : false,
    retry: (count, error) => !(error instanceof ApiError && error.isUnauthorized) && count < 1,
  });
}

export function usePublishJob(jobId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (targets: PublishTarget[]) => publishJob(jobId, targets),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: publishingKeys.results(jobId) });
      queryClient.invalidateQueries({ queryKey: jobKeys.detail(jobId) });
    },
  });
}

export function useGenerateSeo(jobId: string) {
  return useMutation({ mutationFn: () => generateSeo(jobId) });
}
