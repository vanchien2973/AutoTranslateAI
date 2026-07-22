"use client";
import { AlertTriangle } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Choice } from "@/components/ui/Choice";
import { Panel } from "@/components/ui/Panel";
import { getUsage, usageKeys, type UsageByDay, type UsageByKey } from "@/lib/api/usage";

const RANGES = [7, 30, 90];

function money(value: number) {
  if (value === 0) return "$0";
  return value < 0.01 ? `$${value.toFixed(4)}` : `$${value.toFixed(2)}`;
}

export default function UsagePage() {
  const [days, setDays] = useState(30);
  const { data, error, isPending } = useQuery({
    queryKey: usageKeys.summary(days),
    queryFn: () => getUsage(days),
  });

  const summary = data?.summary;

  return (
    <div className="mx-auto max-w-4xl space-y-4 px-6 py-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-fg text-xl font-semibold tracking-tight">Usage &amp; cost</h1>
          <p className="text-muted mt-1 text-sm">
            What the paid APIs have actually charged for transcription, translation, and speech.
          </p>
        </div>
        <div className="w-56">
          <Choice
            name="Range"
            value={days}
            onChange={setDays}
            options={RANGES.map((value) => ({ value, label: `${value} days` }))}
          />
        </div>
      </div>

      {error && (
        <p className="border-red/30 bg-red/5 text-red flex items-start gap-2 rounded-md border p-3 text-sm">
          <AlertTriangle aria-hidden className="mt-0.5 size-4 shrink-0" />
          {error.message}
        </p>
      )}

      {isPending && <p className="text-muted text-sm">Loading usage…</p>}

      {summary && (
        <>
          <div className="grid gap-3 sm:grid-cols-2">
            <Panel>
              <p className="text-muted text-[11px] tracking-wide uppercase">Total cost</p>
              <p className="timecode text-fg mt-1 text-2xl">{money(summary.totalCostUsd)}</p>
              <p className="text-muted/70 mt-1 text-[11px]">across the last {days} days</p>
            </Panel>
            <Panel>
              <p className="text-muted text-[11px] tracking-wide uppercase">API calls</p>
              <p className="timecode text-fg mt-1 text-2xl">{summary.callCount}</p>
              <p className="text-muted/70 mt-1 text-[11px]">billable requests</p>
            </Panel>
          </div>

          <Panel title="Spend per day">
            {summary.byDay.length === 0 ? (
              <p className="text-muted text-sm">Nothing billed in this range.</p>
            ) : (
              <DailyBars days={summary.byDay} />
            )}
          </Panel>

          <div className="grid gap-4 sm:grid-cols-2">
            <Panel title="By provider">
              <Breakdown rows={summary.byProvider} total={summary.totalCostUsd} />
            </Panel>
            <Panel title="By operation">
              <Breakdown rows={summary.byOperation} total={summary.totalCostUsd} />
            </Panel>
          </div>
        </>
      )}
    </div>
  );
}

function Breakdown({ rows, total }: { rows: UsageByKey[]; total: number }) {
  if (rows.length === 0) {
    return <p className="text-muted text-sm">No calls recorded.</p>;
  }

  return (
    <ul className="space-y-2.5">
      {rows.map((row) => {
        const share = total > 0 ? (row.costUsd / total) * 100 : 0;
        return (
          <li key={row.key}>
            <div className="flex items-baseline justify-between gap-2 text-xs">
              <span className="text-fg">{row.key}</span>
              <span className="timecode text-muted">
                {money(row.costUsd)} · {row.callCount} calls
              </span>
            </div>
            <div className="bg-console mt-1 h-1 overflow-hidden rounded-full">
              <div className="bg-cyan h-full rounded-full" style={{ width: `${share}%` }} />
            </div>
          </li>
        );
      })}
    </ul>
  );
}

function DailyBars({ days }: { days: UsageByDay[] }) {
  const peak = Math.max(...days.map((day) => day.costUsd), 0);

  return (
    <div className="flex h-32 items-end gap-1 overflow-x-auto">
      {days.map((day) => {
        const height = peak > 0 ? (day.costUsd / peak) * 100 : 0;
        return (
          <div
            key={day.date}
            title={`${day.date} — ${money(day.costUsd)} over ${day.callCount} calls`}
            className="flex min-w-[10px] flex-1 flex-col justify-end"
          >
            <div
              className="bg-cyan/70 hover:bg-cyan rounded-t"
              style={{ height: `${Math.max(height, 2)}%` }}
            />
            <span className="timecode text-muted/50 mt-1 truncate text-center text-[9px]">
              {day.date.slice(5)}
            </span>
          </div>
        );
      })}
    </div>
  );
}
