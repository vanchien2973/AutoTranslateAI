"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, useSyncExternalStore, type ReactNode } from "react";

import { Header } from "@/components/layout/Header";
import { Sidebar } from "@/components/layout/Sidebar";
import { hasSession } from "@/lib/api/client";

const LOGIN_PATH = "/login";

function subscribe(onChange: () => void) {
  window.addEventListener("storage", onChange);
  return () => window.removeEventListener("storage", onChange);
}

export function AppFrame({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const signedIn = useSyncExternalStore(subscribe, hasSession, () => false);

  const isLogin = pathname === LOGIN_PATH;

  useEffect(() => {
    if (isLogin || signedIn) return;

    const next = pathname && pathname !== "/" ? `?next=${encodeURIComponent(pathname)}` : "";
    router.replace(`${LOGIN_PATH}${next}`);
  }, [isLogin, signedIn, pathname, router]);

  if (isLogin) return <>{children}</>;

  if (!signedIn) return null;

  return (
    <div className="flex h-full overflow-hidden">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <Header />
        <main className="flex-1 overflow-y-auto">{children}</main>
      </div>
    </div>
  );
}
