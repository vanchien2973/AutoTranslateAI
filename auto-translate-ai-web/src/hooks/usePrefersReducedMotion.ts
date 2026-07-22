"use client";

import { useSyncExternalStore } from "react";
import { useUiStore } from "@/store/uiStore";

const QUERY = "(prefers-reduced-motion: reduce)";

function subscribe(callback: () => void) {
  const query = window.matchMedia(QUERY);
  query.addEventListener("change", callback);
  return () => query.removeEventListener("change", callback);
}

export function usePrefersReducedMotion() {
  const forced = useUiStore((state) => state.forceReducedMotion);
  const system = useSyncExternalStore(
    subscribe,
    () => window.matchMedia(QUERY).matches,
    () => false,
  );

  return forced || system;
}
