"use client";

import { cn } from "@/lib/utils";

export interface ChoiceOption<T> {
  value: T;
  label: string;
  hint?: string;
}

export function Choice<T extends string | number | boolean>({
  options,
  value,
  onChange,
  disabled = false,
  name,
}: {
  options: ChoiceOption<T>[];
  value: T;
  onChange: (value: T) => void;
  disabled?: boolean;
  name: string;
}) {
  return (
    <div
      role="radiogroup"
      aria-label={name}
      className={cn(
        "border-hairline bg-console grid gap-1 rounded-md border p-1",
        disabled && "pointer-events-none opacity-50",
      )}
      style={{ gridTemplateColumns: `repeat(${options.length}, minmax(0, 1fr))` }}
    >
      {options.map((option) => {
        const selected = option.value === value;
        return (
          <button
            key={String(option.value)}
            type="button"
            role="radio"
            aria-checked={selected}
            onClick={() => onChange(option.value)}
            className={cn(
              "rounded px-2 py-1.5 text-center text-xs transition-colors",
              selected ? "bg-cyan/15 text-cyan" : "text-muted hover:text-fg",
            )}
          >
            <span className="block font-medium">{option.label}</span>
            {option.hint && (
              <span className="text-muted/70 mt-0.5 block text-[10px]">{option.hint}</span>
            )}
          </button>
        );
      })}
    </div>
  );
}
