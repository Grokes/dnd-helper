import test from 'node:test'
import assert from 'node:assert/strict'
import {
  createDefaultDraft,
  extractFeatureLevel,
  getHitPoints,
  getModifier,
  getProficiencyBonus,
  mapCharacterToDraft,
  parseEquipFromInventory,
  splitMultiline,
  uniqueValues,
  type CharacterDraft,
} from './characterCreateModel'
import type { Character, CharacterOptions } from '../../../types/character'

test('createDefaultDraft creates complete level one character draft', () => {
  const draft = createDefaultDraft()

  assert.equal(draft.level, 1)
  assert.equal(draft.baseAbilities.length, 6)
  assert.deepEqual(draft.baseAbilities.map((ability) => ability.score), [10, 10, 10, 10, 10, 10])
  assert.deepEqual(draft.raceSkillSelections, [])
  assert.deepEqual(draft.classSkillSelections, [])
})

test('ability and progression helpers follow DnD 5e formulas used by builder preview', () => {
  assert.equal(getModifier(8), -1)
  assert.equal(getModifier(10), 0)
  assert.equal(getModifier(18), 4)
  assert.equal(getProficiencyBonus(1), 2)
  assert.equal(getProficiencyBonus(5), 3)
  assert.equal(getProficiencyBonus(17), 6)
  assert.equal(getHitPoints(10, 3, 2), 28)
})

test('text and inventory helpers normalize user state', () => {
  assert.deepEqual(splitMultiline(' mage-hand \n\n shield '), ['mage-hand', 'shield'])
  assert.deepEqual(uniqueValues(['a', 'b', 'a']), ['a', 'b'])
  assert.deepEqual(parseEquipFromInventory(['item:rope', 'equip:body=hide-armor;main=rapier;off=shield']), {
    body: 'hide-armor',
    mainHand: 'rapier',
    offHand: 'shield',
  })
})

test('mapCharacterToDraft restores class skill selections without fixed duplicates', () => {
  const character = createCharacter({
    skills: [
      { skillId: 'Athletics', level: 4 },
      { skillId: 'Perception', level: 3 },
      { skillId: 'Stealth', level: 5 },
    ],
    raceSkillSelections: ['Perception'],
    classSkillSelections: ['Athletics'],
  })
  const options = createOptions()

  const draft = mapCharacterToDraft(character, options)

  assert.equal(draft.raceId, 'half-elf')
  assert.equal(draft.classId, 'fighter')
  assert.equal(draft.backgroundId, 'soldier')
  assert.deepEqual(draft.raceSkillSelections, ['Perception'])
  assert.deepEqual(draft.classSkillSelections, ['Athletics'])
})

test('mapCharacterToDraft falls back to known options when saved refs are outdated', () => {
  const character = createCharacter({
    raceId: 'legacy-race',
    classId: 'legacy-class',
    backgroundId: 'legacy-background',
  })

  const draft = mapCharacterToDraft(character, createOptions())

  assert.equal(draft.raceId, 'half-elf')
  assert.equal(draft.classId, 'fighter')
  assert.equal(draft.backgroundId, 'soldier')
})

test('mapCharacterToDraft clears race selections when selected race has no skill choice', () => {
  const character = createCharacter({
    raceId: 'human',
    raceSkillSelections: ['Perception'],
    skills: [{ skillId: 'Perception', level: 2 }],
  })
  const options = createOptions()
  options.races.push({
    id: 'human',
    parentRace: 'Человек',
    name: 'Человек',
    speed: 30,
    bonuses: [],
    bonusChoiceRule: null,
    grantedSkillProficiencies: [],
    skillChoiceRule: null,
    details: [],
    summary: 'Человек',
  })

  const draft = mapCharacterToDraft(character, options)

  assert.deepEqual(draft.raceSkillSelections, [])
})

test('mapCharacterToDraft rebuilds class selections from skills when saved class selections are stale', () => {
  const character = createCharacter({
    classSkillSelections: ['Stealth'],
    skills: [
      { skillId: 'Athletics', level: 4 },
      { skillId: 'Intimidation', level: 3 },
    ],
  })

  const draft = mapCharacterToDraft(character, createOptions())

  assert.deepEqual(draft.classSkillSelections, ['Athletics'])
})

test('extractFeatureLevel reads level from localized feature titles', () => {
  assert.equal(extractFeatureLevel('3 уровень: Воинский архетип'), 3)
  assert.equal(extractFeatureLevel('Классовая особенность'), 0)
})

function createCharacter(overrides: Partial<Character> = {}): Character {
  const baseDraft: CharacterDraft = createDefaultDraft()

  return {
    id: 'character-1',
    name: 'Тестовый герой',
    race: 'Полуэльф',
    className: 'Воин',
    subclass: '',
    level: 3,
    armorClass: 16,
    weaponDamage: null,
    hitPoints: 28,
    maxHitPoints: 28,
    currentHitPoints: 20,
    spentHitDice: 0,
    availableHitDice: 3,
    passivePerception: 13,
    skills: [],
    canEdit: true,
    raceId: 'half-elf',
    classId: 'fighter',
    backgroundId: 'soldier',
    background: 'Солдат',
    alignment: '',
    speed: 30,
    proficiencyBonus: 2,
    notes: '',
    baseAbilities: baseDraft.baseAbilities,
    bonusAbilitySelections: [],
    raceSkillSelections: [],
    classSkillSelections: [],
    skillProficiencies: [],
    abilities: [],
    savingThrows: [],
    spellSlots: [],
    maxSpellSlots: [],
    knownSpells: ['shield'],
    calculationTrace: [],
    inventory: ['item:rope'],
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  }
}

function createOptions(): CharacterOptions {
  return {
    races: [
      {
        id: 'half-elf',
        parentRace: 'Полуэльф',
        name: 'Полуэльф',
        speed: 30,
        bonuses: [],
        bonusChoiceRule: null,
        grantedSkillProficiencies: [],
        skillChoiceRule: { count: 1, availableSkills: ['Perception', 'Stealth'], summary: 'Выбери навык расы.' },
        details: [],
        summary: 'Полуэльф',
      },
    ],
    classes: [
      {
        id: 'fighter',
        name: 'Воин',
        hitDie: 10,
        primaryAbilities: ['STR'],
        savingThrowProficiencies: ['STR', 'CON'],
        skillChoiceRule: { count: 1, availableSkills: ['Athletics', 'Perception'], summary: 'Выбери навык класса.' },
        details: [],
        summary: 'Воин',
      },
    ],
    backgrounds: [
      {
        id: 'soldier',
        name: 'Солдат',
        grantedSkillProficiencies: ['Intimidation'],
        details: [],
        summary: 'Солдат',
      },
    ],
  }
}
