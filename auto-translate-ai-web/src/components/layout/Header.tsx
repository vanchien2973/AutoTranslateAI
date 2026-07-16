"use client";

import { Bell, ChevronDown, PanelLeftOpen } from "lucide-react";
import Link from "next/link";

import { cn } from "@/lib/utils";
import { useUiStore } from "@/store/uiStore";

export function Header() {
  const open = useUiStore((state) => state.sidebarOpen);
  const toggleSidebar = useUiStore((state) => state.toggleSidebar);

  return (
    <header className="border-hairline bg-panel flex h-14 shrink-0 items-center justify-between gap-4 border-b px-4">
      <div className="flex min-w-0 items-center gap-3">
        {!open && (
          <button
            type="button"
            onClick={toggleSidebar}
            aria-label="Expand sidebar"
            className="text-muted hover:bg-console hover:text-fg rounded-md p-1.5 transition-colors"
          >
            <PanelLeftOpen aria-hidden className="size-4" />
          </button>
        )}

        <nav aria-label="Breadcrumb" className="flex min-w-0 items-center gap-2 text-sm">
          <Link href="/" className="text-muted hover:text-fg transition-colors">
            Jobs
          </Link>
        </nav>
      </div>

      <div className="flex items-center gap-2">
        <button
          type="button"
          aria-label="Notifications"
          className="text-muted hover:bg-console hover:text-fg rounded-md p-2 transition-colors"
        >
          <Bell aria-hidden className="size-4" />
        </button>

        <button
          type="button"
          className={cn(
            "border-hairline bg-console flex items-center gap-2 rounded-md border px-2 py-1.5",
            "hover:border-cyan/40 text-left transition-colors",
          )}
        >
          <span
            aria-hidden
            className="bg-cyan/15 text-cyan grid size-7 place-items-center rounded-full text-[11px] font-semibold"
          >
            CV
          </span>
          <span className="hidden leading-tight sm:block">
            <span className="text-fg block text-xs">Chien Van</span>
            <span className="text-muted block text-[11px]">Admin</span>
          </span>
          <ChevronDown aria-hidden className="text-muted size-4" />
        </button>
      </div>
    </header>
  );
}
