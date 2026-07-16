export const JOB_STATUSES = [
  "Queued",
  "DownloadingMedia",
  "ProcessingPhase1",
  "AwaitingReview",
  "ConfirmedQueued",
  "ProcessingPhase2",
  "Publishing",
  "Completed",
  "Failed",
  "Cancelled",
] as const;

export type JobStatus = (typeof JOB_STATUSES)[number];

export const STEP_TYPES = [
  "Download",
  "ExtractAudio",
  "SeparateBgm",
  "Transcribe",
  "Translate",
  "Tts",
  "GenSubtitle",
  "Mix",
  "Render",
  "Upload",
  "Publish",
] as const;

export type StepType = (typeof STEP_TYPES)[number];

export const VoiceGender = { Female: 0, Male: 1 } as const;
export const SubtitleMode = { None: 0, Hardsub: 1, Softsub: 2 } as const;
export const BgmMode = { DemucsAI: 0, Duck: 1, None: 2 } as const;

export type VoiceGenderValue = (typeof VoiceGender)[keyof typeof VoiceGender];
export type SubtitleModeValue = (typeof SubtitleMode)[keyof typeof SubtitleMode];
export type BgmModeValue = (typeof BgmMode)[keyof typeof BgmMode];

export interface CreateJobInput {
  sourceUrl: string;
  audioLanguage?: string;
  subtitleLanguage?: string;
  enableDubbing?: boolean;
  voiceGender?: VoiceGenderValue;
  subtitleMode?: SubtitleModeValue;
  bgmMode?: BgmModeValue;
}

export interface JobSummary {
  id: string;
  status: JobStatus;
  sourceUrl: string | null;
  currentStep: StepType | null;
  progressPercent: number;
  errorMessage: string | null;
  createdAt: string;
  startedAt: string | null;
  reviewReadyAt: string | null;
  completedAt: string | null;
}

export const STEP_STATUSES = ["Pending", "Running", "Completed", "Skipped", "Failed"] as const;
export type StepStatus = (typeof STEP_STATUSES)[number];

export interface JobStep {
  stepType: StepType;
  status: StepStatus;
  phase: number;
  durationMs: number | null;
  retryCount: number;
  errorMessage: string | null;
}

export interface JobDetail {
  id: string;
  status: JobStatus;
  currentStep: StepType | null;
  progressPercent: number;
  errorMessage: string | null;
  outputUrl: string | null;
  downloadUrl: string | null;
  segmentCount: number;
  editedSegmentCount: number;
  createdAt: string;
  startedAt: string | null;
  reviewReadyAt: string | null;
  confirmedAt: string | null;
  completedAt: string | null;
  steps: JobStep[];
}

export interface JobProgress {
  jobId: string;
  status: JobStatus;
  currentStep: StepType | null;
  progressPercent: number;
}

export interface JobMetrics {
  jobId: string;
  cpuPercent: number;
  memoryUsedBytes: number;
  memoryTotalBytes: number;
}

export interface Segment {
  id: string;
  segmentIndex: number;
  startTime: number;
  endTime: number;
  originalText: string;
  subtitleText: string;
  ttsText: string;
  speakerLabel: string | null;
  isEdited: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

const ACTIVE_STATUSES: ReadonlySet<JobStatus> = new Set<JobStatus>([
  "DownloadingMedia",
  "ProcessingPhase1",
  "ProcessingPhase2",
  "Publishing",
]);

export function isActive(status: JobStatus) {
  return ACTIVE_STATUSES.has(status);
}

export function jobTitle(job: JobSummary) {
  if (!job.sourceUrl) return `Job ${job.id.slice(0, 8)}`;

  try {
    const url = new URL(job.sourceUrl);
    const last = url.pathname.split("/").filter(Boolean).pop();
    return last && last !== "watch" ? decodeURIComponent(last) : url.host + url.search;
  } catch {
    return job.sourceUrl.split(/[\\/]/).pop() ?? job.sourceUrl;
  }
}
