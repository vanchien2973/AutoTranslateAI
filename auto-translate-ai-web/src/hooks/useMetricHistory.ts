"use client";

import { useEffect, useRef, useState } from "react";
import type { JobMetrics } from "@/types/job";

export interface MetricHistory {
  cpu: number[];
  memPercent: number[];
}

const MAX_POINTS = 30;

export function useMetricHistory(metrics: JobMetrics | null): MetricHistory {
  const [history, setHistory] = useState<MetricHistory>({ cpu: [], memPercent: [] });
  const lastSeen = useRef<JobMetrics | null>(null);

  useEffect(() => {
    if (!metrics || metrics === lastSeen.current) return;
    lastSeen.current = metrics;

    const memPercent =
      metrics.memoryTotalBytes > 0 ? (metrics.memoryUsedBytes / metrics.memoryTotalBytes) * 100 : 0;

    setHistory((prev) => ({
      cpu: [...prev.cpu, metrics.cpuPercent].slice(-MAX_POINTS),
      memPercent: [...prev.memPercent, memPercent].slice(-MAX_POINTS),
    }));
  }, [metrics]);

  return history;
}
