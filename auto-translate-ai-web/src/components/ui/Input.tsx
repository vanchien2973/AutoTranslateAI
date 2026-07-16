import type { InputHTMLAttributes } from "react";

import { cn } from "@/lib/utils";

export type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  mono?: boolean;
  invalid?: boolean;
};

export function Input({ className, mono = false, invalid = false, ...props }: InputProps) {
  return (
    <input
      aria-invalid={invalid || undefined}
      className={cn(
        "border-hairline bg-console text-fg h-9 w-full rounded-md border px-3 text-sm",
        "placeholder:text-muted/70 hover:border-cyan/40 transition-colors",
        "disabled:cursor-not-allowed disabled:opacity-50",
        mono && "timecode",
        invalid && "border-red",
        className,
      )}
      {...props}
    />
  );
}
