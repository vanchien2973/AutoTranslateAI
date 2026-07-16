import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";

import { cn } from "@/lib/utils";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 rounded-md text-sm font-medium whitespace-nowrap transition-colors disabled:pointer-events-none disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        primary: "bg-cyan text-console hover:bg-cyan/90",
        secondary: "border border-hairline bg-panel text-fg hover:border-cyan/50",
        ghost: "text-muted hover:bg-panel hover:text-fg",
        danger: "border border-red/40 text-red hover:bg-red/10",
      },
      size: {
        sm: "h-8 px-3",
        md: "h-9 px-4",
        lg: "h-11 px-6 text-base",
        icon: "size-9",
      },
    },
    defaultVariants: { variant: "primary", size: "md" },
  },
);

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants>;

export function Button({ className, variant, size, ...props }: ButtonProps) {
  return <button className={cn(buttonVariants({ variant, size }), className)} {...props} />;
}

export { buttonVariants };
