"use client";

import { AlertTriangle, CheckCircle2, Loader2 } from "lucide-react";
import Link from "next/link";
import { useSearchParams, type ReadonlyURLSearchParams } from "next/navigation";
import { Suspense, useEffect, useRef, useState } from "react";

import { buttonVariants } from "@/components/ui/Button";
import { Panel } from "@/components/ui/Panel";
import { connectChannel } from "@/lib/api/publishing";
import { clearOAuthAttempt, oauthRedirectUri, readOAuthAttempt } from "@/lib/oauth";
import { PLATFORM_LABEL, type PublishPlatformValue } from "@/types/publishing";

type Outcome =
  | { kind: "working"; platform: PublishPlatformValue; code: string }
  | { kind: "done"; channelName: string; platform: PublishPlatformValue }
  | { kind: "error"; message: string };

function inspect(params: ReadonlyURLSearchParams): Outcome {
  const denied = params.get("error");
  if (denied) return { kind: "error", message: `The platform refused the request: ${denied}` };

  const code = params.get("code");
  const state = params.get("state");
  if (!code || !state) {
    return { kind: "error", message: "The callback is missing its code — start again." };
  }

  const attempt = readOAuthAttempt();
  if (!attempt) {
    return { kind: "error", message: "No connection was in progress in this browser." };
  }
  if (attempt.state !== state) {
    return {
      kind: "error",
      message: "The callback did not match the request that started it — nothing was connected.",
    };
  }

  return { kind: "working", platform: attempt.platform, code };
}

export default function OAuthCallbackPage() {
  return (
    <Suspense fallback={<Shell>Finishing the connection…</Shell>}>
      <CallbackHandler />
    </Suspense>
  );
}

function CallbackHandler() {
  const params = useSearchParams();
  const [outcome, setOutcome] = useState<Outcome>(() => inspect(params));
  const exchanged = useRef(false);

  useEffect(() => {
    if (outcome.kind !== "working" || exchanged.current) return;
    exchanged.current = true;

    const { platform, code } = outcome;
    connectChannel({ platform, code, redirectUri: oauthRedirectUri() })
      .then((connection) => {
        clearOAuthAttempt();
        setOutcome({ kind: "done", channelName: connection.channelName, platform });
      })
      .catch((error: Error) => setOutcome({ kind: "error", message: error.message }));
  }, [outcome]);

  if (outcome.kind === "working") {
    return (
      <Shell>
        <Loader2 aria-hidden className="size-4 animate-spin" />
        Exchanging the authorization code…
      </Shell>
    );
  }

  if (outcome.kind === "error") {
    return (
      <Shell tone="error">
        <AlertTriangle aria-hidden className="size-4 shrink-0" />
        {outcome.message}
      </Shell>
    );
  }

  return (
    <Shell tone="success">
      <CheckCircle2 aria-hidden className="size-4 shrink-0" />
      Connected {PLATFORM_LABEL[outcome.platform]} — {outcome.channelName}
    </Shell>
  );
}

function Shell({
  children,
  tone = "neutral",
}: {
  children: React.ReactNode;
  tone?: "neutral" | "success" | "error";
}) {
  return (
    <div className="mx-auto max-w-lg px-6 py-10">
      <Panel title="Connecting account">
        <p
          className={
            tone === "error"
              ? "text-red flex items-center gap-2 text-sm"
              : tone === "success"
                ? "text-green flex items-center gap-2 text-sm"
                : "text-muted flex items-center gap-2 text-sm"
          }
        >
          {children}
        </p>
        <Link
          href="/settings/keys"
          className={`${buttonVariants({ variant: "secondary", size: "sm" })} mt-4`}
        >
          Back to publishing keys
        </Link>
      </Panel>
    </div>
  );
}
