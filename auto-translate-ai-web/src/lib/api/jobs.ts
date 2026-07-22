import { apiFetch } from "@/lib/api/client";
import type { CreateJobInput, JobDetail, JobSummary, PagedResult } from "@/types/job";

export function listJobs(page = 1, pageSize = 20) {
  return apiFetch<PagedResult<JobSummary>>(`/api/jobs?page=${page}&pageSize=${pageSize}`);
}

export function getJob(id: string) {
  return apiFetch<JobDetail>(`/api/jobs/${id}`);
}

export function cancelJob(id: string) {
  return apiFetch<void>(`/api/jobs/${id}/cancel`, { method: "POST" });
}

export function confirmJob(id: string) {
  return apiFetch<{ jobId: string; jobStatus: string }>(`/api/jobs/${id}/confirm`, {
    method: "POST",
  });
}

export function reopenJob(id: string) {
  return apiFetch<{ jobId: string; jobStatus: string }>(`/api/jobs/${id}/reopen`, {
    method: "POST",
  });
}

export function createJob(input: CreateJobInput) {
  return apiFetch<{ jobId: string }>("/api/jobs", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
}

export const jobKeys = {
  all: ["jobs"] as const,
  list: (page: number, pageSize: number) => [...jobKeys.all, "list", page, pageSize] as const,
  detail: (id: string) => [...jobKeys.all, "detail", id] as const,
};
