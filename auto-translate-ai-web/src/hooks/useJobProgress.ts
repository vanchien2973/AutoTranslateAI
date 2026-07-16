"use client";

import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef, useState } from "react";

import { API_BASE_URL, getApiKey } from "@/lib/api/client";
import { jobKeys } from "@/lib/api/jobs";
import type { JobMetrics, JobProgress } from "@/types/job";

export type ConnectionState = "connecting" | "connected" | "reconnecting" | "disconnected";

export interface JobLiveState {
  progress: JobProgress | null;
  metrics: JobMetrics | null;
  connection: ConnectionState;
}

export function useJobProgress(jobId: string): JobLiveState {
  const queryClient = useQueryClient();
  const [progress, setProgress] = useState<JobProgress | null>(null);
  const [metrics, setMetrics] = useState<JobMetrics | null>(null);
  const [connection, setConnection] = useState<ConnectionState>("connecting");
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    let disposed = false;

    const hub = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/jobs`, {
        accessTokenFactory: () => getApiKey() ?? "",
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = hub;

    hub.on("ReceiveProgress", (payload: JobProgress) => {
      setProgress(payload);
      queryClient.invalidateQueries({ queryKey: jobKeys.detail(jobId) });
    });

    hub.on("ReceiveMetrics", (payload: JobMetrics) => setMetrics(payload));

    hub.onreconnecting(() => !disposed && setConnection("reconnecting"));
    hub.onreconnected(async () => {
      if (disposed) return;
      setConnection("connected");
      await hub.invoke("Subscribe", jobId).catch(() => undefined);
      queryClient.invalidateQueries({ queryKey: jobKeys.detail(jobId) });
    });
    hub.onclose(() => !disposed && setConnection("disconnected"));

    (async () => {
      try {
        await hub.start();
        if (disposed) return;
        await hub.invoke("Subscribe", jobId);
        setConnection("connected");
      } catch {
        if (!disposed) setConnection("disconnected");
      }
    })();

    return () => {
      disposed = true;
      connectionRef.current = null;
      if (hub.state !== HubConnectionState.Disconnected) {
        hub.invoke("Unsubscribe", jobId).catch(() => undefined);
      }
      hub.stop().catch(() => undefined);
    };
  }, [jobId, queryClient]);

  return { progress, metrics, connection };
}
