import { create } from "zustand";

interface UiState {
  sidebarOpen: boolean;
  selectedSegmentId: string | null;
  toggleSidebar: () => void;
  selectSegment: (segmentId: string | null) => void;
}

export const useUiStore = create<UiState>((set) => ({
  sidebarOpen: true,
  selectedSegmentId: null,
  toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),
  selectSegment: (segmentId) => set({ selectedSegmentId: segmentId }),
}));
