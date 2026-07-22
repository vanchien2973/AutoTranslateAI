import { apiFetch } from "@/lib/api/client";
import type {
  ChannelConnection,
  PlatformCredential,
  PublishPlatformValue,
  PublishResult,
  PublishTarget,
  SeoMetadata,
} from "@/types/publishing";

const PLATFORM_ROUTE: Record<PublishPlatformValue, string> = {
  0: "YouTube",
  1: "Facebook",
  2: "TikTok",
};

export function listCredentials() {
  return apiFetch<PlatformCredential[]>("/api/publishing/credentials");
}

export function setCredential(
  platform: PublishPlatformValue,
  body: { clientId: string; clientSecret: string; defaultRedirectUri?: string | null },
) {
  return apiFetch<PlatformCredential>(`/api/publishing/credentials/${PLATFORM_ROUTE[platform]}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
}

export function listChannels() {
  return apiFetch<ChannelConnection[]>("/api/publishing/channels");
}

export function getAuthUrl(platform: PublishPlatformValue, redirectUri: string, state: string) {
  const query = new URLSearchParams({
    platform: PLATFORM_ROUTE[platform],
    redirectUri,
    state,
  });
  return apiFetch<{ url: string; state: string }>(`/api/publishing/channels/auth-url?${query}`);
}

export function connectChannel(body: {
  platform: PublishPlatformValue;
  code: string;
  redirectUri: string;
}) {
  return apiFetch<ChannelConnection>("/api/publishing/channels/connect", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
}

export function listPublishResults(jobId: string) {
  return apiFetch<PublishResult[]>(`/api/jobs/${jobId}/publish`);
}

export function publishJob(jobId: string, targets: PublishTarget[]) {
  return apiFetch<{ jobId: string; status: string }>(`/api/jobs/${jobId}/publish`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ targets }),
  });
}

export function generateSeo(jobId: string) {
  return apiFetch<SeoMetadata>(`/api/jobs/${jobId}/seo`, { method: "POST" });
}

export const publishingKeys = {
  credentials: ["publishing", "credentials"] as const,
  channels: ["publishing", "channels"] as const,
  results: (jobId: string) => ["publishing", "results", jobId] as const,
};
