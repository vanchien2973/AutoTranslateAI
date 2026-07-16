"use client";

import { ChevronDown } from "lucide-react";
import { useEffect, useId, useRef, useState } from "react";

import { cn } from "@/lib/utils";

export interface DropdownOption<T extends string = string> {
  value: T;
  label: string;
  hint?: string;
}

export interface DropdownProps<T extends string = string> {
  options: DropdownOption<T>[];
  value: T | null;
  onChange: (value: T) => void;
  placeholder?: string;
  label?: string;
  disabled?: boolean;
  className?: string;
}

export function Dropdown<T extends string = string>({
  options,
  value,
  onChange,
  placeholder = "Select…",
  label,
  disabled = false,
  className,
}: DropdownProps<T>) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const listboxId = useId();
  const selected = options.find((option) => option.value === value) ?? null;

  useEffect(() => {
    if (!open) return;

    const onPointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false);
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };

    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  return (
    <div ref={containerRef} className={cn("relative", className)}>
      {label && (
        <span className="text-muted mb-1.5 block text-xs tracking-wide uppercase">{label}</span>
      )}

      <button
        type="button"
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={open ? listboxId : undefined}
        onClick={() => setOpen((current) => !current)}
        className={cn(
          "border-hairline bg-console flex h-9 w-full items-center justify-between gap-2 rounded-md border px-3 text-sm",
          "hover:border-cyan/40 transition-colors disabled:cursor-not-allowed disabled:opacity-50",
          open && "border-cyan/60",
        )}
      >
        <span className={cn("truncate", selected ? "text-fg" : "text-muted")}>
          {selected?.label ?? placeholder}
        </span>
        <ChevronDown
          aria-hidden
          className={cn("text-muted size-4 transition-transform", open && "rotate-180")}
        />
      </button>

      {open && (
        <ul
          id={listboxId}
          role="listbox"
          className="border-hairline bg-panel absolute z-20 mt-1 max-h-64 w-full overflow-auto rounded-md border py-1 shadow-lg shadow-black/40"
        >
          {options.map((option) => {
            const isSelected = option.value === value;
            return (
              <li key={option.value}>
                <button
                  type="button"
                  role="option"
                  aria-selected={isSelected}
                  onClick={() => {
                    onChange(option.value);
                    setOpen(false);
                  }}
                  className={cn(
                    "flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-sm",
                    "hover:bg-console/60",
                    isSelected ? "text-cyan" : "text-fg",
                  )}
                >
                  <span className="truncate">{option.label}</span>
                  {option.hint && (
                    <span className="timecode text-muted text-xs">{option.hint}</span>
                  )}
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
