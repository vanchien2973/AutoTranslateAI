"use client";

import {
  KeyRound,
  LayoutDashboard,
  LayoutTemplate,
  ListVideo,
  PanelLeftClose,
  PanelLeftOpen,
  Plus,
  Settings2,
  SlidersHorizontal,
  type LucideIcon,
} from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

import { LevelMeter } from "@/components/layout/LevelMeter";
import { SystemStatus } from "@/components/layout/SystemStatus";
import { useJobs } from "@/hooks/useJobs";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/store/uiStore";
import { isActive } from "@/types/job";

interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
}

const PRIMARY: NavItem = { href: "/", label: "Dashboard", icon: LayoutDashboard };

const NAV_GROUPS: { heading: string; items: NavItem[] }[] = [
  {
    heading: "Jobs",
    items: [
      { href: "/jobs", label: "All jobs", icon: ListVideo },
      { href: "/jobs/new", label: "New job", icon: Plus },
      { href: "/templates", label: "Templates", icon: LayoutTemplate },
    ],
  },
  {
    heading: "Settings",
    items: [
      { href: "/settings/providers", label: "Providers", icon: SlidersHorizontal },
      { href: "/settings/keys", label: "API keys", icon: KeyRound },
      { href: "/settings/preferences", label: "Preferences", icon: Settings2 },
    ],
  },
];

export function Sidebar() {
  const pathname = usePathname();
  const open = useUiStore((state) => state.sidebarOpen);
  const toggleSidebar = useUiStore((state) => state.toggleSidebar);
  const { data } = useJobs();
  const onAir = data?.items.some((job) => isActive(job.status)) ?? false;

  return (
    <aside
      className={cn(
        "border-hairline bg-panel flex shrink-0 flex-col border-r transition-[width] duration-200",
        open ? "w-64" : "w-16",
      )}
    >
      <div
        className={cn(
          "border-hairline flex h-14 shrink-0 items-center gap-2 border-b",
          open ? "px-4" : "justify-center px-0",
        )}
      >
        <Link href="/" className="flex min-w-0 items-center gap-2.5">
          <LevelMeter active={onAir} />
          {open && (
            <span className="font-display text-fg truncate text-sm font-semibold tracking-tight">
              AutoTranslate<span className="text-cyan">AI</span>
            </span>
          )}
        </Link>

        {open && (
          <button
            type="button"
            onClick={toggleSidebar}
            aria-label="Collapse sidebar"
            className="text-muted hover:bg-console hover:text-fg ml-auto rounded-md p-1.5 transition-colors"
          >
            <PanelLeftClose aria-hidden className="size-4" />
          </button>
        )}
      </div>

      <nav className="flex-1 overflow-y-auto px-2 py-4">
        <NavLink item={PRIMARY} active={pathname === PRIMARY.href} collapsed={!open} featured />

        {NAV_GROUPS.map((group) => (
          <div key={group.heading} className="mt-5">
            {open ? (
              <p className="text-muted mb-2 px-2 text-[11px] tracking-widest uppercase">
                {group.heading}
              </p>
            ) : (
              <div aria-hidden className="border-hairline mx-2 mb-2 border-t" />
            )}
            <ul className="space-y-0.5">
              {group.items.map((item) => (
                <li key={item.href}>
                  <NavLink item={item} active={pathname === item.href} collapsed={!open} />
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>

      <SystemStatus collapsed={!open} />

      <div
        className={cn(
          "border-hairline flex h-10 shrink-0 items-center border-t",
          open ? "justify-between px-4" : "justify-center px-0",
        )}
      >
        {open && <span className="timecode text-muted/60 text-[11px]">v1.0.0</span>}
        {!open && (
          <button
            type="button"
            onClick={toggleSidebar}
            aria-label="Expand sidebar"
            title="Expand sidebar"
            className="text-muted hover:bg-console hover:text-fg rounded-md p-1.5 transition-colors"
          >
            <PanelLeftOpen aria-hidden className="size-4" />
          </button>
        )}
      </div>
    </aside>
  );
}

function NavLink({
  item,
  active,
  collapsed,
  featured = false,
}: {
  item: NavItem;
  active: boolean;
  collapsed: boolean;
  featured?: boolean;
}) {
  const Icon = item.icon;

  return (
    <Link
      href={item.href}
      title={collapsed ? item.label : undefined}
      aria-current={active ? "page" : undefined}
      className={cn(
        "flex items-center gap-2.5 rounded-md px-2 py-2 text-sm transition-colors",
        collapsed && "justify-center",
        active
          ? featured
            ? "border-cyan/30 bg-cyan/10 text-cyan border"
            : "bg-cyan/10 text-cyan"
          : "text-muted hover:bg-console hover:text-fg",
      )}
    >
      <Icon aria-hidden className="size-4 shrink-0" />
      {!collapsed && <span className="truncate">{item.label}</span>}
    </Link>
  );
}
