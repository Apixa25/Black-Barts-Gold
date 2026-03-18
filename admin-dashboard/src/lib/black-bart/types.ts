import type {
  CompanionCoinContext,
  CompanionHiderContext,
  CompanionPlayerContext,
} from '@/lib/companion/companion-engine'
import type { CompanionIntentType, CompanionResponsePack } from '@/lib/companion/quick-prompts'

export interface AuthenticatedPlayer {
  id: string
  displayName: string | null
}

export interface BlackBartLocalHuntPressureSummary {
  cellId: string
  activeWindowMinutes: number
  activePlayerCount: number
  activeCoinCount: number
  huntPressure: number
}

export interface BlackBartRecentCompanionAction {
  id: string
  toolCalled: string
  createdAt: string
  intentType: string | null
  eventType: string | null
  replyNow: string | null
}

export interface BlackBartStartSessionInput {
  action: 'start_session'
  player: CompanionPlayerContext
  selectedCoin: CompanionCoinContext | null
  localHuntPressure: BlackBartLocalHuntPressureSummary | null
  recentCompanionHistory: BlackBartRecentCompanionAction[]
}

export interface BlackBartSubmitIntentInput {
  action: 'submit_intent'
  player: CompanionPlayerContext
  selectedCoin: CompanionCoinContext | null
  hider: CompanionHiderContext | null
  intentType: CompanionIntentType
  distanceToCoinMeters: number | null
  localHuntPressure: BlackBartLocalHuntPressureSummary | null
  recentCompanionHistory: BlackBartRecentCompanionAction[]
}

export interface BlackBartReportEventInput {
  action: 'report_event'
  selectedCoin: CompanionCoinContext | null
  eventType: string
  localHuntPressure: BlackBartLocalHuntPressureSummary | null
  recentCompanionHistory: BlackBartRecentCompanionAction[]
}

export type BlackBartRuntimeInput =
  | BlackBartStartSessionInput
  | BlackBartSubmitIntentInput
  | BlackBartReportEventInput

export interface BlackBartPromptContext {
  playerName: string | null
  currentZoneId: string | null
  currentCellL17: string | null
  selectedCoinId: string | null
  selectedCoinValue: number | null
  selectedCoinLocationName: string | null
  hiderName: string | null
  distanceToCoinMeters: number | null
  eventType: string | null
  intentType: CompanionIntentType | null
  localHuntPressure: BlackBartLocalHuntPressureSummary | null
  recentCompanionHistory: BlackBartRecentCompanionAction[]
  situationSummary: string
}

export interface BlackBartRuntimeMeta {
  source: 'scripted_fallback'
  systemPromptVersion: string
  promptContext: BlackBartPromptContext
}

export interface BlackBartRuntimeResult {
  responsePack: CompanionResponsePack | null
  runtimeMeta: BlackBartRuntimeMeta
}
