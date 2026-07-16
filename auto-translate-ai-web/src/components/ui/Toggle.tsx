"use client";

import { cn } from "@/lib/utils";

export function Toggle({
  checked,
  onChange,
  label,
  hint,
}: {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label: string;
  hint?: string;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      onClick={() => onChange(!checked)}
      className="border-hairline bg-console hover:border-cyan/40 flex w-full items-center justify-between gap-4 rounded-md border px-3 py-2.5 text-left transition-colors"
    >
      <span>
        <span className="text-fg block text-sm">{label}</span>
        {hint && <span className="text-muted block text-xs">{hint}</span>}
      </span>
      <span
        aria-hidden
        className={cn(
          "relative h-5 w-9 shrink-0 rounded-full transition-colors",
          checked ? "bg-cyan" : "bg-hairline",
        )}
      >
        <span
          className={cn(
            "bg-console absolute top-0.5 size-4 rounded-full transition-[left]",
            checked ? "left-[18px]" : "left-0.5",
          )}
        />
      </span>
    </button>
  );
}
