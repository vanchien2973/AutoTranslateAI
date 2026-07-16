import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatTimecode(seconds: number) {
  const clamped = Math.max(0, seconds);
  const hh = Math.floor(clamped / 3600);
  const mm = Math.floor((clamped % 3600) / 60);
  const ss = Math.floor(clamped % 60);
  const ms = Math.round((clamped % 1) * 1000);

  const pad = (value: number, size = 2) => value.toString().padStart(size, "0");
  return `${pad(hh)}:${pad(mm)}:${pad(ss)}.${pad(ms, 3)}`;
}

const RELATIVE_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["year", 365 * 24 * 3600],
  ["month", 30 * 24 * 3600],
  ["day", 24 * 3600],
  ["hour", 3600],
  ["minute", 60],
];

const relativeFormat = new Intl.RelativeTimeFormat("en", { numeric: "auto" });

export function formatBytes(bytes: number) {
  if (bytes <= 0) return "0 GB";
  const gb = bytes / 1024 ** 3;
  if (gb >= 1) return `${gb.toFixed(1)} GB`;
  return `${(bytes / 1024 ** 2).toFixed(0)} MB`;
}

export function formatClock(totalSeconds: number) {
  const s = Math.max(0, Math.round(totalSeconds));
  const hh = Math.floor(s / 3600);
  const mm = Math.floor((s % 3600) / 60);
  const ss = s % 60;
  const pad = (n: number) => n.toString().padStart(2, "0");
  return hh > 0 ? `${hh}:${pad(mm)}:${pad(ss)}` : `${mm}:${pad(ss)}`;
}

export function formatRelativeTime(iso: string, now: Date = new Date()) {
  const elapsed = (new Date(iso).getTime() - now.getTime()) / 1000;

  for (const [unit, seconds] of RELATIVE_UNITS) {
    if (Math.abs(elapsed) >= seconds) {
      return relativeFormat.format(Math.round(elapsed / seconds), unit);
    }
  }

  return relativeFormat.format(Math.round(elapsed), "second");
}
