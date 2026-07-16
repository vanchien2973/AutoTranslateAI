import type { StepType } from "@/types/job";

export interface StepMeta {
  label: string;
  description: string;
}

export const STEP_META: Record<StepType, StepMeta> = {
  Download: { label: "Download", description: "Fetching the source video." },
  ExtractAudio: { label: "Extract audio", description: "Pulling the audio track from the video." },
  SeparateBgm: {
    label: "Separate music",
    description: "Isolating vocals from the background music with Demucs.",
  },
  Transcribe: { label: "Transcribe", description: "Turning speech into timestamped text." },
  Translate: {
    label: "Translate",
    description: "Translating each segment into the target language.",
  },
  Tts: { label: "Synthesize voice", description: "Generating the dubbed voice track." },
  GenSubtitle: { label: "Subtitles", description: "Building the subtitle track." },
  Mix: { label: "Mix audio", description: "Blending the new voice back over the music." },
  Render: { label: "Render video", description: "Muxing audio, subtitles, and video." },
  Upload: { label: "Upload", description: "Storing the finished video." },
  Publish: { label: "Publish", description: "Pushing the video to connected platforms." },
};

export const PHASE1_STEPS: StepType[] = [
  "Download",
  "ExtractAudio",
  "SeparateBgm",
  "Transcribe",
  "Translate",
];

export function stepLabel(step: StepType | null | undefined) {
  return step ? STEP_META[step].label : "—";
}
