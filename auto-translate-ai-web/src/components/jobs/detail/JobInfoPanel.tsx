import { Panel } from "@/components/ui/Panel";
import type { JobDetail } from "@/types/job";

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-3 py-1">
      <dt className="text-muted text-xs">{label}</dt>
      <dd className="timecode text-fg text-right text-xs">{value}</dd>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="border-hairline border-b px-4 py-3 last:border-b-0">
      <p className="text-muted mb-1.5 text-[11px] tracking-widest uppercase">{title}</p>
      <dl>{children}</dl>
    </div>
  );
}

const timestamp = (iso: string | null) => (iso ? new Date(iso).toLocaleString() : "—");

export function JobInfoPanel({ job }: { job: JobDetail }) {
  return (
    <Panel bodyClassName="" className="text-sm">
      <Section title="Output">
        <Row label="Dubbing" value={job.enableDubbing ? "On" : "Off (original audio)"} />
        <Row label="Audio" value={job.audioLanguage} />
        <Row label="Subtitles" value={job.subtitleLanguage ?? "None"} />
      </Section>

      <Section title="Review">
        <Row label="Segments" value={String(job.segmentCount)} />
        <Row label="Edited" value={String(job.editedSegmentCount)} />
      </Section>

      <Section title="Delivery">
        <Row label="Status" value={job.downloadUrl ? "Ready" : "Pending render"} />
        {job.downloadUrl && (
          <div className="pt-1">
            <a href={job.downloadUrl} className="text-cyan text-xs hover:underline">
              Download video
            </a>
          </div>
        )}
      </Section>

      <Section title="Providers">
        <Row label="STT" value="whisper.net" />
        <Row label="LLM" value="gpt-4.1-nano" />
        <Row label="TTS" value="Azure Speech" />
        <Row label="Storage" value="Cloudflare R2" />
      </Section>

      <Section title="Timeline">
        <Row label="Created" value={timestamp(job.createdAt)} />
        <Row label="Started" value={timestamp(job.startedAt)} />
        <Row label="Review ready" value={timestamp(job.reviewReadyAt)} />
        <Row label="Completed" value={timestamp(job.completedAt)} />
      </Section>
    </Panel>
  );
}
