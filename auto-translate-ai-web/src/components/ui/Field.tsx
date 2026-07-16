import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

export function Field({
  label,
  hint,
  error,
  htmlFor,
  children,
  className,
}: {
  label: string;
  hint?: string;
  error?: string;
  htmlFor?: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("space-y-1.5", className)}>
      <label htmlFor={htmlFor} className="text-muted block text-xs tracking-wide uppercase">
        {label}
      </label>
      {children}
      {error ? (
        <p className="text-red text-xs">{error}</p>
      ) : (
        hint && <p className="text-muted/70 text-xs">{hint}</p>
      )}
    </div>
  );
}
