import { apiFetch } from "@/lib/api/client";

export const LOGO_MAX_BYTES = 2 * 1024 * 1024;
export const LOGO_CONTENT_TYPES = ["image/png", "image/jpeg", "image/webp"];

export function uploadLogo(file: File) {
  const body = new FormData();
  body.append("file", file);

  return apiFetch<{ storageKey: string }>("/api/media/logo", { method: "POST", body });
}

export function validateLogoFile(file: File): string | null {
  if (!LOGO_CONTENT_TYPES.includes(file.type)) {
    return "The logo must be a PNG, JPEG, or WebP image.";
  }
  if (file.size > LOGO_MAX_BYTES) {
    return "The logo must be 2 MB or smaller.";
  }
  return null;
}
