export const PublishPlatform = { YouTube: 0, Facebook: 1, TikTok: 2 } as const;
export type PublishPlatformValue = (typeof PublishPlatform)[keyof typeof PublishPlatform];

export const PublishStatus = { Pending: 0, Publishing: 1, Published: 2, Failed: 3 } as const;
export type PublishStatusValue = (typeof PublishStatus)[keyof typeof PublishStatus];

export const PLATFORM_LABEL: Record<PublishPlatformValue, string> = {
  [PublishPlatform.YouTube]: "YouTube",
  [PublishPlatform.Facebook]: "Facebook",
  [PublishPlatform.TikTok]: "TikTok",
};

export interface PlatformCredential {
  platform: PublishPlatformValue;
  clientId: string;
  hasSecret: boolean;
  defaultRedirectUri: string | null;
  updatedAt: string | null;
}

export interface ChannelConnection {
  id: string;
  platform: PublishPlatformValue;
  channelId: string;
  channelName: string;
  isExpired: boolean;
  connectedAt: string;
}

export interface PublishResult {
  id: string;
  platform: PublishPlatformValue;
  status: PublishStatusValue;
  externalId: string | null;
  url: string | null;
  error: string | null;
  createdAt: string;
  publishedAt: string | null;
}

export interface PublishTarget {
  platform: PublishPlatformValue;
  connectionId?: string | null;
  title?: string | null;
  description?: string | null;
  tags?: string[] | null;
}

export interface SeoMetadata {
  title: string;
  description: string;
  tags: string[];
}
