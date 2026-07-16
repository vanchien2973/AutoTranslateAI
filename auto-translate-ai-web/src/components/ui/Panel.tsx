import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

export function Panel({
  title,
  subtitle,
  actions,
  bodyClassName,
  className,
  children,
}: {
  title?: string;
  subtitle?: string;
  actions?: ReactNode;
  bodyClassName?: string;
  className?: string;
  children: ReactNode;
}) {
  return (
    <section className={cn("border-hairline bg-panel flex flex-col rounded-lg border", className)}>
      {(title || actions) && (
        <header className="border-hairline flex items-center justify-between gap-3 border-b px-4 py-3">
          <div className="min-w-0">
            {title && <h2 className="text-fg truncate text-sm font-medium">{title}</h2>}
            {subtitle && <p className="text-muted mt-0.5 truncate text-xs">{subtitle}</p>}
          </div>
          {actions && <div className="flex shrink-0 items-center gap-1">{actions}</div>}
        </header>
      )}
      <div className={cn("flex-1", bodyClassName ?? "p-4")}>{children}</div>
    </section>
  );
}
