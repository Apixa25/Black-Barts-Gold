import type { BlackBartRuntimeInput, BlackBartPromptContext } from '@/lib/black-bart/types'

export const BLACK_BART_SYSTEM_PROMPT_VERSION = 'v1-scaffold'

export const BLACK_BART_SYSTEM_PROMPT = `You are Black Bart, the gentleman outlaw and living game master of Black Bart's Gold.

Character rules:
- You are a Wild West stagecoach robber, not a pirate.
- Speak like a polite, witty, observant outlaw from the California Gold Rush era.
- Sound warm, confident, and a little poetic.
- Use plain, vivid language that works well in a mobile game companion.

Hard rules:
- Never use pirate language or nautical imagery.
- Never invent game state, player facts, or rewards you were not given.
- Never promise actions you cannot take.
- Keep replies concise and situational.
- Prioritize helping the player feel like you are riding alongside them in a living world.
`

function describeSelectedCoin(input: BlackBartRuntimeInput): {
  selectedCoinId: string | null
  selectedCoinValue: number | null
  selectedCoinLocationName: string | null
} {
  if ('selectedCoin' in input && input.selectedCoin) {
    return {
      selectedCoinId: input.selectedCoin.id,
      selectedCoinValue: input.selectedCoin.value,
      selectedCoinLocationName: input.selectedCoin.location_name,
    }
  }

  return {
    selectedCoinId: null,
    selectedCoinValue: null,
    selectedCoinLocationName: null,
  }
}

export function buildBlackBartPromptContext(input: BlackBartRuntimeInput): BlackBartPromptContext {
  const coinDetails = describeSelectedCoin(input)
  const player = 'player' in input ? input.player : null
  const hider = 'hider' in input ? input.hider : null
  const intentType = input.action === 'submit_intent' ? input.intentType : null
  const eventType = input.action === 'report_event' ? input.eventType : null
  const distanceToCoinMeters = input.action === 'submit_intent' ? input.distanceToCoinMeters : null

  const summaryParts = [
    `action=${input.action}`,
    player?.display_name ? `player=${player.display_name}` : null,
    player?.current_zone_id ? `zone=${player.current_zone_id}` : null,
    player?.current_cell_l17 ? `cell=${player.current_cell_l17}` : null,
    coinDetails.selectedCoinLocationName ? `coin_location=${coinDetails.selectedCoinLocationName}` : null,
    coinDetails.selectedCoinValue !== null ? `coin_value=$${coinDetails.selectedCoinValue.toFixed(2)}` : null,
    hider?.display_name ? `hider=${hider.display_name}` : null,
    intentType ? `intent=${intentType}` : null,
    eventType ? `event=${eventType}` : null,
    distanceToCoinMeters !== null ? `distance_m=${Math.round(distanceToCoinMeters)}` : null,
    input.localHuntPressure
      ? `local_pressure=${input.localHuntPressure.huntPressure} (${input.localHuntPressure.activePlayerCount}p/${input.localHuntPressure.activeCoinCount}c)`
      : null,
    input.recentCompanionHistory.length > 0
      ? `recent_companion_actions=${input.recentCompanionHistory.length}`
      : null,
  ].filter((part): part is string => Boolean(part))

  return {
    playerName: player?.display_name ?? null,
    currentZoneId: player?.current_zone_id ?? null,
    currentCellL17: player?.current_cell_l17 ?? null,
    selectedCoinId: coinDetails.selectedCoinId,
    selectedCoinValue: coinDetails.selectedCoinValue,
    selectedCoinLocationName: coinDetails.selectedCoinLocationName,
    hiderName: hider?.display_name ?? null,
    distanceToCoinMeters,
    eventType,
    intentType,
    localHuntPressure: input.localHuntPressure,
    recentCompanionHistory: input.recentCompanionHistory,
    situationSummary: summaryParts.join(' | '),
  }
}

export function buildBlackBartPromptEnvelope(input: BlackBartRuntimeInput, promptContext: BlackBartPromptContext) {
  const recentHistorySummary = promptContext.recentCompanionHistory.length > 0
    ? promptContext.recentCompanionHistory
        .map((entry, index) => {
          const signal = entry.intentType ?? entry.eventType ?? entry.toolCalled
          const reply = entry.replyNow ? ` -> "${entry.replyNow}"` : ''
          return `${index + 1}. ${signal}${reply}`
        })
        .join('\n')
    : 'No recent companion history.'

  const userPrompt = [
    `Action: ${input.action}`,
    `Situation summary: ${promptContext.situationSummary || 'No summary available.'}`,
    promptContext.playerName ? `Player name: ${promptContext.playerName}` : 'Player name: unknown',
    promptContext.selectedCoinId ? `Selected coin id: ${promptContext.selectedCoinId}` : 'Selected coin id: none',
    promptContext.selectedCoinValue !== null
      ? `Selected coin value: $${promptContext.selectedCoinValue.toFixed(2)}`
      : 'Selected coin value: unknown',
    promptContext.localHuntPressure
      ? `Local hunt pressure: ${promptContext.localHuntPressure.huntPressure} with ${promptContext.localHuntPressure.activePlayerCount} active players and ${promptContext.localHuntPressure.activeCoinCount} active coins`
      : 'Local hunt pressure: unavailable',
    `Recent companion history:\n${recentHistorySummary}`,
    'Respond as Black Bart with concise, in-character guidance grounded only in this context.',
  ].join('\n')

  return {
    systemPrompt: BLACK_BART_SYSTEM_PROMPT,
    userPrompt,
  }
}
