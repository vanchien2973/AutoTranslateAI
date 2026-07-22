"use client";
import { useMutation } from "@tanstack/react-query";
import { uploadLogo } from "@/lib/api/media";

export function useUploadLogo() {
  return useMutation({
    mutationFn: (file: File) => uploadLogo(file),
  });
}
