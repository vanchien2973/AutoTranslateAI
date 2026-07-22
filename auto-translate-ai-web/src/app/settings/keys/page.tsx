"use client";
import { AlertTriangle, Check, Link2, RefreshCw } from "lucide-react";
import { useState } from "react";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { Field } from "@/components/ui/Field";
import { Input } from "@/components/ui/Input";
import { Panel } from "@/components/ui/Panel";
import { useChannels, useCredentials, useSetCredential } from "@/hooks/usePublishing";
import { getAuthUrl } from "@/lib/api/publishing";
import { oauthRedirectUri, rememberOAuthAttempt } from "@/lib/oauth";
import { formatRelativeTime } from "@/lib/utils";
import { PLATFORM_LABEL, PublishPlatform, type PublishPlatformValue } from "@/types/publishing";

const PLATFORMS: PublishPlatformValue[] = [
  PublishPlatform.YouTube,
  PublishPlatform.Facebook,
  PublishPlatform.TikTok,
];

export default function PublishingKeysPage() {
  const { data: credentials } = useCredentials();
  const { data: channels, error: channelsError } = useChannels();
  const [connectError, setConnectError] = useState<string | null>(null);

  async function connect(platform: PublishPlatformValue) {
    setConnectError(null);
    try {
      const redirectUri = oauthRedirectUri();
      const state = crypto.randomUUID();
      const response = await getAuthUrl(platform, redirectUri, state);

      rememberOAuthAttempt(platform, response.state);
      window.location.assign(response.url);
    } catch (error) {
      setConnectError(error instanceof Error ? error.message : "Could not start the connection.");
    }
  }

  return (
    <div className="mx-auto max-w-3xl space-y-4 px-6 py-6">
      <div>
        <h1 className="text-fg text-xl font-semibold tracking-tight">Publishing keys</h1>
        <p className="text-muted mt-1 text-sm">
          Register each platform&apos;s app credentials, then connect the accounts you publish to.
        </p>
      </div>

      <Panel title="App credentials" subtitle="From the platform's developer console.">
        <div className="space-y-4">
          {PLATFORMS.map((platform) => (
            <CredentialForm
              key={platform}
              platform={platform}
              configured={credentials?.find((credential) => credential.platform === platform)}
            />
          ))}
        </div>
      </Panel>

      <Panel
        title="Connected accounts"
        subtitle={`Redirect URI to allow-list: ${typeof window === "undefined" ? "" : oauthRedirectUri()}`}
      >
        {connectError && (
          <p className="border-red/30 bg-red/5 text-red mb-3 flex items-start gap-2 rounded-md border p-3 text-sm">
            <AlertTriangle aria-hidden className="mt-0.5 size-4 shrink-0" />
            {connectError}
          </p>
        )}

        {channelsError ? (
          <p className="text-red text-sm">{channelsError.message}</p>
        ) : (
          <ul className="space-y-2">
            {PLATFORMS.map((platform) => {
              const connected = channels?.filter((channel) => channel.platform === platform) ?? [];
              const credential = credentials?.find((item) => item.platform === platform);

              return (
                <li
                  key={platform}
                  className="border-hairline bg-console flex items-center justify-between gap-3 rounded-md border p-3"
                >
                  <div className="min-w-0">
                    <p className="text-fg text-sm">{PLATFORM_LABEL[platform]}</p>
                    {connected.length === 0 ? (
                      <p className="text-muted text-xs">Not connected</p>
                    ) : (
                      connected.map((channel) => (
                        <p
                          key={channel.id}
                          className="text-muted mt-0.5 flex items-center gap-2 text-xs"
                        >
                          {channel.channelName}
                          <span className="text-muted/60">
                            {formatRelativeTime(channel.connectedAt)}
                          </span>
                          {channel.isExpired && <Badge tone="red">Expired</Badge>}
                        </p>
                      ))
                    )}
                  </div>

                  <Button
                    size="sm"
                    variant={connected.length > 0 ? "secondary" : "primary"}
                    onClick={() => connect(platform)}
                    disabled={!credential?.hasSecret}
                    title={credential?.hasSecret ? undefined : "Save the app credentials first"}
                  >
                    {connected.length > 0 ? (
                      <>
                        <RefreshCw aria-hidden />
                        Reconnect
                      </>
                    ) : (
                      <>
                        <Link2 aria-hidden />
                        Connect
                      </>
                    )}
                  </Button>
                </li>
              );
            })}
          </ul>
        )}
      </Panel>
    </div>
  );
}

function CredentialForm({
  platform,
  configured,
}: {
  platform: PublishPlatformValue;
  configured?: { clientId: string; hasSecret: boolean };
}) {
  const save = useSetCredential();
  const [clientId, setClientId] = useState(configured?.clientId ?? "");
  const [clientSecret, setClientSecret] = useState("");

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        save.mutate(
          { platform, clientId: clientId.trim(), clientSecret: clientSecret.trim() },
          { onSuccess: () => setClientSecret("") },
        );
      }}
      className="border-hairline rounded-md border p-3"
    >
      <div className="mb-2 flex items-center justify-between">
        <p className="text-fg text-sm">{PLATFORM_LABEL[platform]}</p>
        {configured?.hasSecret && <Badge tone="green">Configured</Badge>}
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Client ID">
          <Input value={clientId} onChange={(event) => setClientId(event.target.value)} />
        </Field>
        <Field
          label="Client secret"
          hint={configured?.hasSecret ? "Stored — type a new one to replace it." : undefined}
        >
          <Input
            type="password"
            value={clientSecret}
            onChange={(event) => setClientSecret(event.target.value)}
            placeholder={configured?.hasSecret ? "••••••••" : ""}
          />
        </Field>
      </div>

      {save.error && <p className="text-red mt-2 text-xs">{save.error.message}</p>}

      <Button
        type="submit"
        size="sm"
        variant="secondary"
        className="mt-3"
        disabled={save.isPending || !clientId.trim() || !clientSecret.trim()}
      >
        <Check aria-hidden />
        {save.isPending ? "Saving…" : "Save"}
      </Button>
    </form>
  );
}
