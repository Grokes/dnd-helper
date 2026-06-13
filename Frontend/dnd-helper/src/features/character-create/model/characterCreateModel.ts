import type {
  BackgroundOption,
  BaseAbilityScore,
  Character,
  CharacterOptions,
  ClassOption,
  FeatureDetail,
  RaceOption,
} from '../../../types/character'
import { translateAbility, translateSkill } from '../../../utils/characterPresentation'

export const abilityOrder = ['STR', 'DEX', 'CON', 'INT', 'WIS', 'CHA'] as const
export const abilityScoreMin = 3
export const abilityScoreMax = 18
export const draftStorageKey = 'dnd-helper.character-draft'
export const steps = ['identity', 'race', 'class', 'background', 'abilities', 'spells', 'inventory', 'review'] as const

export type StepKey = (typeof steps)[number]

export type CharacterDraft = {
  name: string
  raceId: string
  classId: string
  backgroundId: string
  level: number
  alignment: string
  notes: string
  baseAbilities: BaseAbilityScore[]
  bonusAbilitySelections: string[]
  raceSkillSelections: string[]
  classSkillSelections: string[]
  spellsText: string
  inventoryText: string
}

export type DraftValidationErrors = Partial<Record<StepKey | 'global', string[]>>

export type InfoModalState = {
  title: string
  subtitle: string
  overview: Array<{ label: string; value: string }>
  features: FeatureDetail[]
  proficiencies: Array<{ category: string; items: string[] }>
}

export type FeatureInfoModalState = {
  title: string
  description: string
}

export type SpellInfoModalState = {
  name: string
  circle: number
  minLevel: number
  classes: string[]
  summary?: string
  description?: string
}

export type ProficiencyItemsModalState = {
  title: string
  items: string[]
}

export type EquipmentSlots = {
  body: string | null
  mainHand: string | null
  offHand: string | null
}

export function createDefaultDraft(): CharacterDraft {
  return {
    name: '',
    raceId: '',
    classId: '',
    backgroundId: '',
    level: 1,
    alignment: '',
    notes: '',
    baseAbilities: abilityOrder.map((key) => ({ key, score: 10 })),
    bonusAbilitySelections: [],
    raceSkillSelections: [],
    classSkillSelections: [],
    spellsText: '',
    inventoryText: '',
  }
}

export function loadDraft(isEditMode: boolean): CharacterDraft {
  if (typeof window === 'undefined' || isEditMode) {
    return createDefaultDraft()
  }

  const savedDraft = window.sessionStorage.getItem(draftStorageKey)
  if (!savedDraft) {
    return createDefaultDraft()
  }

  try {
    return JSON.parse(savedDraft) as CharacterDraft
  } catch {
    return createDefaultDraft()
  }
}

export function getModifier(score: number) {
  return Math.floor((score - 10) / 2)
}

export function getProficiencyBonus(level: number) {
  return 2 + Math.floor((level - 1) / 4)
}

export function getHitPoints(hitDie: number, level: number, constitutionModifier: number) {
  const firstLevel = hitDie + constitutionModifier
  if (level === 1) {
    return firstLevel
  }

  const averagePerLevel = Math.floor(hitDie / 2) + 1 + constitutionModifier
  return firstLevel + (level - 1) * averagePerLevel
}

export function splitMultiline(value: string) {
  return value
    .split('\n')
    .map((item) => item.trim())
    .filter(Boolean)
}

export function uniqueValues(values: string[]) {
  return Array.from(new Set(values))
}

export function parseEquipFromInventory(entries: string[]): EquipmentSlots {
  const source = entries.find((entry) => entry.startsWith('equip:'))
  if (!source) {
    return { body: null, mainHand: null, offHand: null }
  }

  const parsed = Object.fromEntries(
    source
      .slice(6)
      .split(';')
      .map((part) => {
        const [key, value] = part.split('=')
        return [key, value ?? '']
      }),
  ) as Record<string, string>

  return {
    body: parsed.body || null,
    mainHand: parsed.main || null,
    offHand: parsed.off || null,
  }
}

export function getStepTitle(step: StepKey) {
  const titles: Record<StepKey, string> = {
    identity: 'Основа',
    race: 'Раса',
    class: 'Класс',
    background: 'Предыстория',
    abilities: 'Характеристики',
    spells: 'Заклинания',
    inventory: 'Инвентарь',
    review: 'Проверка',
  }

  return titles[step]
}

export function mapCharacterToDraft(character: Character, options: CharacterOptions): CharacterDraft {
  const race = options.races.find((item) => item.id === character.raceId) ?? null
  const characterClass = options.classes.find((item) => item.id === character.classId) ?? null
  const background = options.backgrounds.find((item) => item.id === character.backgroundId) ?? null

  const fixedSkills = uniqueValues([
    ...(race?.grantedSkillProficiencies ?? []),
    ...(background?.grantedSkillProficiencies ?? []),
  ])

  const allCharacterSkillIds = uniqueValues(character.skills.map((item) => item.skillId))
    .filter((skill) => !fixedSkills.includes(skill))

  let raceSkillSelections = uniqueValues(character.raceSkillSelections)
  if (race?.skillChoiceRule) {
    raceSkillSelections = raceSkillSelections
      .filter((skill) => race.skillChoiceRule?.availableSkills.includes(skill))
      .filter((skill) => !fixedSkills.includes(skill))

    if (raceSkillSelections.length !== race.skillChoiceRule.count) {
      raceSkillSelections = allCharacterSkillIds
        .filter((skill) => race.skillChoiceRule?.availableSkills.includes(skill))
        .slice(0, race.skillChoiceRule.count)
    } else {
      raceSkillSelections = raceSkillSelections.slice(0, race.skillChoiceRule.count)
    }
  } else {
    raceSkillSelections = []
  }

  let classSkillSelections = uniqueValues(character.classSkillSelections)
  if (characterClass) {
    classSkillSelections = classSkillSelections
      .filter((skill) => characterClass.skillChoiceRule.availableSkills.includes(skill))
      .filter((skill) => !fixedSkills.includes(skill))
      .filter((skill) => !raceSkillSelections.includes(skill))

    if (classSkillSelections.length !== characterClass.skillChoiceRule.count) {
      classSkillSelections = allCharacterSkillIds
        .filter((skill) => !raceSkillSelections.includes(skill))
        .filter((skill) => characterClass.skillChoiceRule.availableSkills.includes(skill))
        .slice(0, characterClass.skillChoiceRule.count)
    } else {
      classSkillSelections = classSkillSelections.slice(0, characterClass.skillChoiceRule.count)
    }
  } else {
    classSkillSelections = []
  }

  return {
    name: character.name,
    raceId: options.races.some((race) => race.id === character.raceId)
      ? character.raceId
      : options.races[0]?.id ?? '',
    classId: options.classes.some((item) => item.id === character.classId)
      ? character.classId
      : options.classes[0]?.id ?? '',
    backgroundId: options.backgrounds.some((item) => item.id === character.backgroundId)
      ? character.backgroundId
      : options.backgrounds[0]?.id ?? '',
    level: character.level,
    alignment: character.alignment,
    notes: character.notes,
    baseAbilities: character.baseAbilities,
    bonusAbilitySelections: character.bonusAbilitySelections,
    raceSkillSelections,
    classSkillSelections,
    spellsText: character.knownSpells.join('\n'),
    inventoryText: character.inventory.join('\n'),
  }
}

export function buildOptionOverview(
  option: RaceOption | ClassOption | BackgroundOption,
  kind: 'race' | 'class' | 'background',
) {
  if (kind === 'race') {
    const race = option as RaceOption
    return [
      { label: 'Скорость', value: `${race.speed} футов` },
    ]
  }

  if (kind === 'background') {
    const background = option as BackgroundOption
    return [
      {
        label: 'Навыки',
        value: background.grantedSkillProficiencies.map((skill) => translateSkill(skill)).join(', '),
      },
    ]
  }

  const characterClass = option as ClassOption
  const base = [{ label: 'Кость хитов', value: `d${characterClass.hitDie}` }]
  const savingThrows = {
    label: 'Спасброски',
    value: characterClass.savingThrowProficiencies
      .map((ability) => translateAbility(ability))
      .join(', '),
  }
  return [...base, savingThrows]
}

export function buildInfoIconLabel(title: string) {
  return `Подробнее о ${title}`
}

export function formatSkillWithAbility(skillId: string, skillMap: Record<string, string>) {
  const ability = skillMap[skillId]
  if (!ability) {
    return translateSkill(skillId)
  }
  return `${translateSkill(skillId)} (${translateAbility(ability)})`
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

export function extractFeatureLevel(title: string) {
  const match = title.match(/(\d+)\s*уровень/i)
  if (!match) {
    return 0
  }

  const parsed = Number(match[1])
  return Number.isFinite(parsed) ? parsed : 0
}
