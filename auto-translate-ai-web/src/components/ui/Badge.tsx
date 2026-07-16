import { cva, type VariantProps } from "class-variance-authority";
import type { HTMLAttributes } from "react";

import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium tracking-wide uppercase",
  {
    variants: {
      tone: {
        amber: "border-amber/30 bg-amber/10 text-amber",
        cyan: "border-cyan/30 bg-cyan/10 text-cyan",
        green: "border-green/30 bg-green/10 text-green",
        red: "border-red/30 bg-red/10 text-red",
        slate: "border-hairline bg-panel text-muted",
      },
    },
    defaultVariants: { tone: "slate" },
  },
);

export type BadgeTone = NonNullable<VariantProps<typeof badgeVariants>["tone"]>;

export type BadgeProps = HTMLAttributes<HTMLSpanElement> &
  VariantProps<typeof badgeVariants> & {
    dot?: boolean;
    pulse?: boolean;
  };

export function Badge({
  className,
  tone,
  dot = true,
  pulse = false,
  children,
  ...props
}: BadgeProps) {
  return (
    <span className={cn(badgeVariants({ tone }), className)} {...props}>
      {dot && (
        <span
          aria-hidden
          className={cn("size-1.5 rounded-full bg-current", pulse && "animate-pulse")}
        />
      )}
      {children}
    </span>
  );
}
