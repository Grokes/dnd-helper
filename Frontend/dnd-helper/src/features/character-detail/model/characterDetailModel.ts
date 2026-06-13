export type EquippedSlots = { body: string | null; mainHand: string | null; offHand: string | null }

export type SpellModalState = {
  slug?: string
  name: string
  circle: number
  minLevel: number
  classes: string[]
  summary?: string
  description?: string
  damageDice?: string
  damageType?: string
}

export type RollEntry = {
  id: string
  label: string
  expression: string
  diceResult: number
  modifier: number
  total: number
  createdAt: string
}

export type CharacterNotice = {
  id: string
  text: string
  kind: 'success' | 'error'
}

export const skillAbilityMap: Record<string, string> = {
  Acrobatics: 'DEX',
  AnimalHandling: 'WIS',
  Arcana: 'INT',
  Athletics: 'STR',
  Deception: 'CHA',
  History: 'INT',
  Insight: 'WIS',
  Intimidation: 'CHA',
  Investigation: 'INT',
  Medicine: 'WIS',
  Nature: 'INT',
  Perception: 'WIS',
  Performance: 'CHA',
  Persuasion: 'CHA',
  Religion: 'INT',
  SleightOfHand: 'DEX',
  Stealth: 'DEX',
  Survival: 'WIS',
}

export function parseEquipEntry(entry: string): EquippedSlots {
  const fallback = { body: null, mainHand: null, offHand: null }
  if (!entry.startsWith('equip:')) {
    return fallback
  }

  const slotsRaw = entry.slice(6).split(';')
  const slots = Object.fromEntries(
    slotsRaw.map((part) => {
      const [key, value] = part.split('=')
      return [key, value ?? '']
    }),
  ) as Record<string, string>

  return {
    body: slots.body || null,
    mainHand: slots.main || null,
    offHand: slots.off || null,
  }
}

export function humanizeSpellFallback(value: string) {
  return value.replaceAll('-', ' ')
}

export function translateClassSlug(value: string) {
  const map: Record<string, string> = {
    barbarian: 'Варвар',
    bard: 'Бард',
    cleric: 'Жрец',
    druid: 'Друид',
    fighter: 'Воин',
    monk: 'Монах',
    paladin: 'Паладин',
    ranger: 'Следопыт',
    rogue: 'Плут',
    sorcerer: 'Чародей',
    warlock: 'Колдун',
    wizard: 'Волшебник',
  }
  return map[value] ?? value
}

export function translateDamageType(value?: string) {
  const normalized = (value ?? '').trim().toLowerCase()
  const map: Record<string, string> = {
    slashing: 'рубящий',
    piercing: 'колющий',
    bludgeoning: 'дробящий',
    fire: 'огненный',
    cold: 'холод',
    poison: 'яд',
    thunder: 'гром',
    lightning: 'молния',
    radiant: 'сияние',
    necrotic: 'некротический',
    force: 'силовой',
    psychic: 'психический',
    acid: 'кислота',
  }
  return map[normalized] ?? value ?? ''
}

export function parseDiceExpression(value?: string) {
  const normalized = (value ?? '').trim().toLowerCase()
  const match = normalized.match(/^(\d+)d(\d+)$/)
  if (match) {
    const count = Number(match[1])
    const sides = Number(match[2])
    if (count > 0 && sides > 0) {
      return { count, sides, isFlat: false as const }
    }
  }

  const flat = Number(normalized)
  if (Number.isFinite(flat) && flat >= 0) {
    return { count: flat, sides: 0, isFlat: true as const }
  }

  return null
}
