import type { PublishPlatformValue } from "@/types/publishing";

const STATE_KEY = "ata.oauth.state";
const PLATFORM_KEY = "ata.oauth.platform";

export function oauthRedirectUri() {
  return `${window.location.origin}/settings/keys/callback`;
}

export function rememberOAuthAttempt(platform: PublishPlatformValue, state: string) {
  sessionStorage.setItem(STATE_KEY, state);
  sessionStorage.setItem(PLATFORM_KEY, String(platform));
}

export function readOAuthAttempt(): { platform: PublishPlatformValue; state: string } | null {
  if (typeof window === "undefined") return null;

  const state = sessionStorage.getItem(STATE_KEY);
  const platform = sessionStorage.getItem(PLATFORM_KEY);

  if (!state || platform === null) return null;
  return { platform: Number(platform) as PublishPlatformValue, state };
}

export function clearOAuthAttempt() {
  sessionStorage.removeItem(STATE_KEY);
  sessionStorage.removeItem(PLATFORM_KEY);
}
