"use client";

import dynamic from "next/dynamic";

import { MetricCards } from "@/components/processing/MetricCards";
import { Panel } from "@/components/ui/Panel";
import { useMetricHistory } from "@/hooks/useMetricHistory";
import { usePrefersReducedMotion } from "@/hooks/usePrefersReducedMotion";
import { STEP_META } from "@/lib/pipeline";
import type { JobMetrics, StepType } from "@/types/job";

const ProcessingVisualizer = dynamic(() => import("@/components/processing/ProcessingVisualizer"), {
  ssr: false,
  loading: () => <div className="h-[220px]" />,
});

export function VisualizerPanel({
  currentStep,
  percent,
  metrics,
  elapsedSeconds,
  active,
}: {
  currentStep: StepType | null;
  percent: number;
  metrics: JobMetrics | null;
  elapsedSeconds: number;
  active: boolean;
}) {
  const reducedMotion = usePrefersReducedMotion();
  const history = useMetricHistory(metrics);
  const meta = currentStep ? STEP_META[currentStep] : null;

  return (
    <Panel className="min-w-0" bodyClassName="p-4 space-y-4">
      <div>
        <h2 className="text-fg text-sm font-medium">{meta?.label ?? "Idle"}</h2>
        <p className="text-muted mt-0.5 text-xs">
          {active ? (meta?.description ?? "Processing the job.") : "Nothing is running right now."}
        </p>
      </div>

      <ProcessingVisualizer percent={percent} animate={active && !reducedMotion} />

      <MetricCards
        metrics={metrics}
        history={history}
        elapsedSeconds={elapsedSeconds}
        percent={percent}
      />
    </Panel>
  );
}
