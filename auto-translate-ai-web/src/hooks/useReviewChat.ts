"use client";

import { useInfiniteQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { jobKeys } from "@/lib/api/jobs";
import { applyProposal, getChatHistory, reviewKeys, sendChat } from "@/lib/api/review";
import { segmentKeys } from "@/lib/api/segments";
import type { Segment } from "@/types/job";
import type { EditProposal } from "@/types/review";

const PAGE_SIZE = 50;
const NEWEST = 0;

export function useChatHistory(jobId: string, enabled: boolean) {
  return useInfiniteQuery({
    queryKey: reviewKeys.history(jobId),
    enabled,
    initialPageParam: NEWEST,
    queryFn: async ({ pageParam }) => {
      if (pageParam !== NEWEST) return getChatHistory(jobId, pageParam, PAGE_SIZE);
      const probe = await getChatHistory(jobId, 1, PAGE_SIZE);
      const last = Math.max(1, probe.messages.totalPages);
      return last === 1 ? probe : getChatHistory(jobId, last, PAGE_SIZE);
    },
    getNextPageParam: (lastPage) =>
      lastPage.messages.page > 1 ? lastPage.messages.page - 1 : undefined,
    select: (data) => ({
      messages: [...data.pages].reverse().flatMap((page) => page.messages.items),
      total: data.pages[0]?.messages.totalCount ?? 0,
    }),
  });
}

export function useReviewChat(jobId: string) {
  const queryClient = useQueryClient();
  const [proposals, setProposals] = useState<EditProposal[]>([]);

  const chat = useMutation({
    mutationFn: (userMessage: string) => sendChat(jobId, userMessage),
    onSuccess: (response) => {
      setProposals((current) => [...current, ...response.proposals]);
      queryClient.invalidateQueries({ queryKey: reviewKeys.history(jobId) });
    },
  });

  const apply = useMutation({
    mutationFn: (proposalId: string) => applyProposal(jobId, proposalId),
    onSuccess: (updated: Segment, proposalId) => {
      queryClient.setQueryData<Segment[]>(segmentKeys.all(jobId), (current) =>
        current?.map((segment) => (segment.id === updated.id ? updated : segment)),
      );
      queryClient.invalidateQueries({ queryKey: jobKeys.detail(jobId) });
      dismiss(proposalId);
    },
  });

  function dismiss(proposalId: string) {
    setProposals((current) => current.filter((proposal) => proposal.proposalId !== proposalId));
  }

  return { proposals, chat, apply, dismiss };
}
