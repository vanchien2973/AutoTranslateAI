"use client";
import { AlertTriangle, LogIn } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState, type FormEvent } from "react";
import { LevelMeter } from "@/components/layout/LevelMeter";
import { Button } from "@/components/ui/Button";
import { Field } from "@/components/ui/Field";
import { Input } from "@/components/ui/Input";
import { login } from "@/lib/api/auth";
import { ApiError, hasSession, setApiKey } from "@/lib/api/client";

export default function LoginPage() {
  return (
    <Suspense fallback={null}>
      <LoginForm />
    </Suspense>
  );
}

function LoginForm() {
  const router = useRouter();
  const params = useSearchParams();
  const next = params.get("next") || "/";

  const [email, setEmail] = useState("admin@gmail.com");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  useEffect(() => {
    if (hasSession()) router.replace(next);
  }, [next, router]);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setPending(true);

    try {
      const { token } = await login(email.trim(), password);
      setApiKey(token);
      router.replace(next);
    } catch (caught) {
      setError(
        caught instanceof ApiError && caught.status === 401
          ? "That email and password don’t match."
          : caught instanceof Error
            ? caught.message
            : "Could not sign in.",
      );
      setPending(false);
    }
  }

  return (
    <div className="bg-console flex min-h-full items-center justify-center px-6 py-12">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex items-center gap-2.5">
          <LevelMeter active />
          <span className="font-display text-fg text-lg font-semibold tracking-tight">
            AutoTranslate<span className="text-cyan">AI</span>
          </span>
        </div>

        <div className="border-hairline bg-panel rounded-lg border p-6">
          <h1 className="text-fg text-lg font-semibold tracking-tight">Sign in</h1>
          <p className="text-muted mt-1 mb-5 text-sm">
            Enter the admin credentials to open the console.
          </p>

          <form onSubmit={onSubmit} className="space-y-4">
            <Field label="Email" htmlFor="email">
              <Input
                id="email"
                type="email"
                autoComplete="username"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </Field>

            <Field label="Password" htmlFor="password">
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoFocus
              />
            </Field>

            {error && (
              <p className="border-red/30 bg-red/5 text-red flex items-start gap-2 rounded-md border p-2.5 text-xs">
                <AlertTriangle aria-hidden className="mt-0.5 size-3.5 shrink-0" />
                {error}
              </p>
            )}

            <Button type="submit" className="w-full" disabled={pending || !password}>
              <LogIn aria-hidden />
              {pending ? "Signing in…" : "Sign in"}
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
}
