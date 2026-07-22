"use client";

import { ChevronDown, LogOut, PanelLeftOpen } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";

import { setApiKey } from "@/lib/api/client";
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

      <UserMenu />
    </header>
  );
}

function UserMenu() {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (event: MouseEvent) => {
      if (!ref.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onPointerDown);
    return () => document.removeEventListener("mousedown", onPointerDown);
  }, [open]);

  function signOut() {
    setApiKey(null);
    router.replace("/login");
  }

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        onClick={() => setOpen((current) => !current)}
        aria-haspopup="menu"
        aria-expanded={open}
        className={cn(
          "border-hairline bg-console flex items-center gap-2 rounded-md border px-2 py-1.5",
          "hover:border-cyan/40 text-left transition-colors",
        )}
      >
        <span
          aria-hidden
          className="bg-cyan/15 text-cyan grid size-7 place-items-center rounded-full text-[11px] font-semibold"
        >
          AD
        </span>
        <span className="text-fg hidden text-xs sm:block">Admin</span>
        <ChevronDown aria-hidden className="text-muted size-4" />
      </button>

      {open && (
        <div
          role="menu"
          className="border-hairline bg-panel absolute right-0 z-20 mt-1 w-40 rounded-md border py-1 shadow-lg shadow-black/40"
        >
          <button
            type="button"
            role="menuitem"
            onClick={signOut}
            className="text-muted hover:bg-console hover:text-fg flex w-full items-center gap-2 px-3 py-2 text-left text-sm"
          >
            <LogOut aria-hidden className="size-4" />
            Sign out
          </button>
        </div>
      )}
    </div>
  );
}
