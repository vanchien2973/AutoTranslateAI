import { create } from "zustand";
import { persist } from "zustand/middleware";

export interface JobDefaults {
  audioLanguage: string;
  subtitleLanguage: string;
  enableDubbing: boolean;
}

interface UiState {
  sidebarOpen: boolean;
  selectedSegmentId: string | null;
  forceReducedMotion: boolean;
  jobDefaults: JobDefaults;
  toggleSidebar: () => void;
  selectSegment: (segmentId: string | null) => void;
  setForceReducedMotion: (value: boolean) => void;
  setJobDefaults: (defaults: Partial<JobDefaults>) => void;
}

export const DEFAULT_JOB_DEFAULTS: JobDefaults = {
  audioLanguage: "vi",
  subtitleLanguage: "vi",
  enableDubbing: true,
};

export const useUiStore = create<UiState>()(
  persist(
    (set) => ({
      sidebarOpen: true,
      selectedSegmentId: null,
      forceReducedMotion: false,
      jobDefaults: DEFAULT_JOB_DEFAULTS,
      toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
      selectSegment: (segmentId) => set({ selectedSegmentId: segmentId }),
      setForceReducedMotion: (value) => set({ forceReducedMotion: value }),
      setJobDefaults: (defaults) =>
        set((state) => ({ jobDefaults: { ...state.jobDefaults, ...defaults } })),
    }),
    {
      name: "ata.ui",
      partialize: (state) => ({
        sidebarOpen: state.sidebarOpen,
        forceReducedMotion: state.forceReducedMotion,
        jobDefaults: state.jobDefaults,
      }),
    },
  ),
);
