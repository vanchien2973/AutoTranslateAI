import { Badge, type BadgeTone } from "@/components/ui/Badge";
import { isActive, type JobStatus } from "@/types/job";

const STATUS_TONE: Record<JobStatus, BadgeTone> = {
  Queued: "slate",
  DownloadingMedia: "amber",
  ProcessingPhase1: "amber",
  AwaitingReview: "cyan",
  ConfirmedQueued: "slate",
  ProcessingPhase2: "amber",
  Publishing: "amber",
  Completed: "green",
  Failed: "red",
  Cancelled: "slate",
};

const STATUS_LABEL: Record<JobStatus, string> = {
  Queued: "Queued",
  DownloadingMedia: "Downloading",
  ProcessingPhase1: "Processing",
  AwaitingReview: "Needs review",
  ConfirmedQueued: "Confirmed",
  ProcessingPhase2: "Rendering",
  Publishing: "Publishing",
  Completed: "Completed",
  Failed: "Failed",
  Cancelled: "Cancelled",
};

export function StatusBadge({ status }: { status: JobStatus }) {
  return (
    <Badge tone={STATUS_TONE[status]} pulse={isActive(status)}>
      {STATUS_LABEL[status]}
    </Badge>
  );
}

export { STATUS_TONE, STATUS_LABEL };
