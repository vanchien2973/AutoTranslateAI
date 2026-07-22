import { apiFetch } from "@/lib/api/client";
import type { PagedResult, Segment } from "@/types/job";
import type { ChatMessage, ReviewChatResponse } from "@/types/review";

export function sendChat(jobId: string, userMessage: string) {
  return apiFetch<ReviewChatResponse>(`/api/jobs/${jobId}/review/chat`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userMessage }),
  });
}

export function getChatHistory(jobId: string, page = 1, pageSize = 50) {
  return apiFetch<{ messages: PagedResult<ChatMessage> }>(
    `/api/jobs/${jobId}/review/chat?page=${page}&pageSize=${pageSize}`,
  );
}

export function applyProposal(jobId: string, proposalId: string) {
  return apiFetch<Segment>(`/api/jobs/${jobId}/review/chat/${proposalId}/apply`, {
    method: "POST",
  });
}

export const reviewKeys = {
  history: (jobId: string) => ["review", jobId, "history"] as const,
};
