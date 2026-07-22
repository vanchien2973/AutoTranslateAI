export const EditTarget = { AudioText: 0, SubtitleText: 1 } as const;
export type EditTargetValue = (typeof EditTarget)[keyof typeof EditTarget];

export const ChatRole = { User: 0, Assistant: 1 } as const;
export type ChatRoleValue = (typeof ChatRole)[keyof typeof ChatRole];

export interface EditProposal {
  proposalId: string;
  segmentId: string;
  segmentIndex: number;
  target: EditTargetValue;
  currentText: string;
  proposedText: string;
  reason: string;
}

export interface ChatMessage {
  role: ChatRoleValue;
  content: string;
}

export interface ReviewChatResponse {
  assistantMessage: string | null;
  proposals: EditProposal[];
  error: string | null;
}
