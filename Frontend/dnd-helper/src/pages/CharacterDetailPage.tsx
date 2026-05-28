import { Link, Navigate, useParams, useSearchParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useAuth } from '../components/AuthProvider'
import { getCharacterById, getCharacterOptions, getEquipmentCatalog, getRulesSpells, updateCharacter } from '../services/charactersApi'
import type { Character, ClassOption, EquipmentCatalogItem, RuleSpellItem } from '../types/character'
import {
  availableSkills,
  getCharacterPortrait,
  translateSkill,
  translateAbility,
} from '../utils/characterPresentation'

type EquippedSlots = { body: string | null; mainHand: string | null; offHand: string | null }
type SpellModalState = {
  name: string
  circle: number
  minLevel: number
  classes: string[]
  summary?: string
  description?: string
}
type RollEntry = {
  id: string
  label: string
  expression: string
  diceResult: number
  modifier: number
  total: number
  createdAt: string
}

const skillAbilityMap: Record<string, string> = {
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

function parseEquipEntry(entry: string): EquippedSlots {
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

function humanizeSpellFallback(value: string) {
  return value.replaceAll('-', ' ')
}

function translateClassSlug(value: string) {
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

function translateDamageType(value?: string) {
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

function parseDiceExpression(value?: string) {
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

export function CharacterDetailPage() {
  const { user, isLoading: isAuthLoading } = useAuth()
  const { id = '' } = useParams()
  const [searchParams] = useSearchParams()
  const [character, setCharacter] = useState<Character | null>(null)
  const [spellCatalog, setSpellCatalog] = useState<RuleSpellItem[]>([])
  const [equipmentCatalog, setEquipmentCatalog] = useState<EquipmentCatalogItem[]>([])
  const [classOption, setClassOption] = useState<ClassOption | null>(null)
  const [notesDraft, setNotesDraft] = useState('')
  const [knownSpellsDraft, setKnownSpellsDraft] = useState<string[]>([])
  const [spellSearch, setSpellSearch] = useState('')
  const [inventorySlugs, setInventorySlugs] = useState<string[]>([])
  const [equippedSlots, setEquippedSlots] = useState<EquippedSlots>({ body: null, mainHand: null, offHand: null })
  const [inventorySearch, setInventorySearch] = useState('')
  const [isInventoryPickerOpen, setIsInventoryPickerOpen] = useState(false)
  const [isSpellPickerOpen, setIsSpellPickerOpen] = useState(false)
  const [spellModal, setSpellModal] = useState<SpellModalState | null>(null)
  const [recentlyAddedSpell, setRecentlyAddedSpell] = useState<string | null>(null)
  const [recentlyAddedInventoryKey, setRecentlyAddedInventoryKey] = useState<string | null>(null)
  const [recentlyAddedInventorySlug, setRecentlyAddedInventorySlug] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [saveStatus, setSaveStatus] = useState<string | null>(null)
  const [rollHistory, setRollHistory] = useState<RollEntry[]>([])
  const [lastRoll, setLastRoll] = useState<RollEntry | null>(null)
  const [isLastRollVisible, setIsLastRollVisible] = useState(false)
  const [isRollHistoryOpen, setIsRollHistoryOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!user) {
      setIsLoading(false)
      return
    }

    let isCancelled = false

    async function loadCharacter() {
      try {
        const [response, spells, equipment, options] = await Promise.all([
          getCharacterById(id),
          getRulesSpells(),
          getEquipmentCatalog(),
          getCharacterOptions(),
        ])
        if (!isCancelled) {
          setCharacter(response)
          setSpellCatalog(spells)
          setEquipmentCatalog(equipment)
          setClassOption(options.classes.find((item) => item.id === response.classId) ?? null)
          setNotesDraft(response.notes ?? '')
          setKnownSpellsDraft(response.knownSpells ?? [])
          setInventorySlugs(response.inventory.filter((entry) => entry.startsWith('item:')).map((entry) => entry.slice(5)))
          setEquippedSlots(parseEquipEntry(response.inventory.find((entry) => entry.startsWith('equip:')) ?? ''))
          setError(null)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить персонажа.')
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false)
        }
      }
    }

    if (id) {
      void loadCharacter()
    } else {
      setIsLoading(false)
      setError('Некорректный идентификатор персонажа.')
    }

    return () => {
      isCancelled = true
    }
  }, [id, user])

  useEffect(() => {
    if (!lastRoll) {
      return
    }

    setIsLastRollVisible(true)
    const timeoutId = window.setTimeout(() => {
      setIsLastRollVisible(false)
    }, 5500)

    return () => {
      window.clearTimeout(timeoutId)
    }
  }, [lastRoll])

  if (!isAuthLoading && !user) {
    return <Navigate to="/login" replace state={{ from: `/characters/${id}` }} />
  }

  if (isLoading) {
    return <section className="surface-card loading-state">Загрузка персонажа...</section>
  }

  if (error || !character) {
    return (
      <section className="surface-card error-state">
        <p>{error ?? 'Персонаж не найден.'}</p>
        <Link to="/characters" className="text-link">
          Вернуться к списку
        </Link>
      </section>
    )
  }

  const equipmentBySlug = new Map(equipmentCatalog.map((item) => [item.slug, item]))
  const spellBySlug = new Map(spellCatalog.map((spell) => [spell.slug, spell]))
  const knownSpellsRu = knownSpellsDraft.map((spell) => spellBySlug.get(spell)?.name ?? humanizeSpellFallback(spell))
  const allSkills = availableSkills.map(({ id }) => {
    const existingSkill = character.skills.find((skill) => skill.skillId === id)
    if (existingSkill) {
      return existingSkill
    }

    const abilityKey = skillAbilityMap[id]
    const abilityModifier = character.abilities.find((ability) => ability.key === abilityKey)?.modifier ?? 0
    return {
      skillId: id,
      level: abilityModifier,
    }
  })

  const filteredEquipmentCatalog = equipmentCatalog.filter((item) =>
    `${item.name} ${item.category ?? ''} ${item.subcategory ?? ''}`.toLowerCase().includes(inventorySearch.toLowerCase()),
  )
  const availableSpells = spellCatalog.filter((spell) =>
    (spell.classSlugs ?? []).includes(character.classId) &&
    (spell.minCharacterLevel ?? 1) <= character.level &&
    spell.name.toLowerCase().includes(spellSearch.toLowerCase()),
  )

  const inventoryWithIndexes = inventorySlugs.map((slug, index) => ({ slug, index, key: `${slug}-${index}` }))
  const isMainHandTwoHanded = Boolean(equippedSlots.mainHand && equipmentBySlug.get(equippedSlots.mainHand)?.isTwoHanded)
  const initiativeModifier = character.abilities.find((ability) => ability.key === 'DEX')?.modifier ?? 0
  const bodyEquipOptions = Array.from(new Set([
    ...(equippedSlots.body ? [equippedSlots.body] : []),
    ...inventorySlugs.filter((slug) => equipmentBySlug.get(slug)?.equipSlot === 'body'),
  ]))
  const mainHandEquipOptions = Array.from(new Set([
    ...(equippedSlots.mainHand ? [equippedSlots.mainHand] : []),
    ...inventorySlugs.filter((slug) => {
      const slot = equipmentBySlug.get(slug)?.equipSlot
      return slot === 'hand' || slot === 'off-hand'
    }),
  ]))
  const offHandEquipOptions = Array.from(new Set([
    ...(equippedSlots.offHand ? [equippedSlots.offHand] : []),
    ...inventorySlugs.filter((slug) => {
      const slot = equipmentBySlug.get(slug)?.equipSlot
      const isTwoHanded = Boolean(equipmentBySlug.get(slug)?.isTwoHanded)
      return (slot === 'hand' || slot === 'off-hand') && !isTwoHanded
    }),
  ]))

  const abilityModifierMap = new Map(character.abilities.map((ability) => [ability.key, ability.modifier]))
  const isRoomReadonlyView = searchParams.get('view') === 'room'
  const canEditPage = character.canEdit && !isRoomReadonlyView

  function resolveWeaponAbilityModifier(item: EquipmentCatalogItem) {
    const strength = abilityModifierMap.get('STR') ?? 0
    const dexterity = abilityModifierMap.get('DEX') ?? 0
    const attackAbility = (item.attackAbility ?? '').trim().toLowerCase()

    if (attackAbility === 'finesse') {
      return Math.max(strength, dexterity)
    }

    if (attackAbility === 'dexterity') {
      return dexterity
    }

    return strength
  }

  function isProficientWithWeapon(item: EquipmentCatalogItem) {
    if (!classOption?.proficiencyGroups) {
      return true
    }

    const weaponProficiencies = Object.entries(classOption.proficiencyGroups)
      .filter(([groupName]) => groupName.toLowerCase().includes('оруж'))
      .flatMap(([, values]) => values ?? [])
      .map((value) => value.trim().toLowerCase())

    if (weaponProficiencies.length === 0) {
      return true
    }

    const itemName = item.name.trim().toLowerCase()
    const category = (item.category ?? '').toLowerCase()

    if (weaponProficiencies.includes(itemName)) {
      return true
    }

    if (weaponProficiencies.includes('простое оружие') && category.includes('simple weapon')) {
      return true
    }

    if (weaponProficiencies.includes('воинское оружие') && category.includes('martial weapon')) {
      return true
    }

    return false
  }

  function getWeaponDamageLabel(slug: string | null) {
    if (!slug) {
      return '—'
    }

    const item = equipmentBySlug.get(slug)
    if (!item || !item.damageDice) {
      return '—'
    }

    const modifier = resolveWeaponAbilityModifier(item)
    const typeRu = translateDamageType(item.damageType)
    const modifierText = modifier === 0 ? '' : modifier > 0 ? ` + ${modifier}` : ` - ${Math.abs(modifier)}`
    return `${item.damageDice}${modifierText}${typeRu ? ` (${typeRu})` : ''}`
  }

  const mainHandDamage = getWeaponDamageLabel(equippedSlots.mainHand)
  const offHandDamage = getWeaponDamageLabel(equippedSlots.offHand)

  function registerRoll(label: string, dieSides: number, modifier = 0) {
    const diceResult = Math.floor(Math.random() * dieSides) + 1
    const total = diceResult + modifier
    const entry: RollEntry = {
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      label,
      expression: `1d${dieSides}`,
      diceResult,
      modifier,
      total,
      createdAt: new Date().toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
    }

    setLastRoll(entry)
    setRollHistory((current) => [entry, ...current].slice(0, 20))
  }

  function registerWeaponDamageRoll(slot: 'mainHand' | 'offHand') {
    const slug = slot === 'mainHand' ? equippedSlots.mainHand : equippedSlots.offHand
    if (!slug) {
      return
    }

    const item = equipmentBySlug.get(slug)
    const parsedDice = parseDiceExpression(item?.damageDice)
    if (!item || !parsedDice) {
      return
    }

    const baseModifier = resolveWeaponAbilityModifier(item)
    // PHB: в бонусной атаке второй рукой положительный модификатор к урону не добавляется.
    const modifier = slot === 'offHand' ? Math.min(0, baseModifier) : baseModifier
    let diceResult = 0
    let expression = ''

    if (parsedDice.isFlat) {
      diceResult = parsedDice.count
      expression = `${parsedDice.count}`
    } else {
      const rolls = Array.from({ length: parsedDice.count }, () => Math.floor(Math.random() * parsedDice.sides) + 1)
      diceResult = rolls.reduce((sum, value) => sum + value, 0)
      expression = `${parsedDice.count}d${parsedDice.sides}${parsedDice.count > 1 ? ` (${rolls.join(' + ')})` : ''}`
    }

    const total = diceResult + modifier
    const handLabel = slot === 'mainHand' ? 'Правая рука' : 'Левая рука'
    const entry: RollEntry = {
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      label: `Урон: ${item.name} (${handLabel})`,
      expression,
      diceResult,
      modifier,
      total,
      createdAt: new Date().toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
    }

    setLastRoll(entry)
    setRollHistory((current) => [entry, ...current].slice(0, 20))
  }

  function registerWeaponAttackRoll(slot: 'mainHand' | 'offHand') {
    const slug = slot === 'mainHand' ? equippedSlots.mainHand : equippedSlots.offHand
    if (!slug) {
      return
    }

    const item = equipmentBySlug.get(slug)
    if (!item) {
      return
    }

    const abilityModifier = resolveWeaponAbilityModifier(item)
    const proficiency = isProficientWithWeapon(item) ? (character?.proficiencyBonus ?? 0) : 0
    const modifier = abilityModifier + proficiency
    const dieResult = Math.floor(Math.random() * 20) + 1
    const total = dieResult + modifier
    const handLabel = slot === 'mainHand' ? 'Правая рука' : 'Левая рука'
    const entry: RollEntry = {
      id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      label: `Попадание: ${item.name} (${handLabel})`,
      expression: '1d20',
      diceResult: dieResult,
      modifier,
      total,
      createdAt: new Date().toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
    }

    setLastRoll(entry)
    setRollHistory((current) => [entry, ...current].slice(0, 20))
  }

  function registerInitiativeRoll() {
    registerRoll('Инициатива', 20, initiativeModifier)
  }

  function removeOneFromInventory(slugs: string[], itemSlug: string) {
    const index = slugs.indexOf(itemSlug)
    if (index < 0) {
      return slugs
    }
    return slugs.filter((_, currentIndex) => currentIndex !== index)
  }

  function addInventoryItem(slug: string) {
    setInventorySlugs((current) => {
      const next = [...current, slug]
      const key = `${slug}-${next.length - 1}`
      setRecentlyAddedInventoryKey(key)
      setRecentlyAddedInventorySlug(slug)
      window.setTimeout(() => setRecentlyAddedInventoryKey((active) => (active === key ? null : active)), 900)
      window.setTimeout(() => setRecentlyAddedInventorySlug((active) => (active === slug ? null : active)), 900)
      return next
    })
  }

  function removeInventoryItem(index: number) {
    setInventorySlugs((current) => current.filter((_, itemIndex) => itemIndex !== index))
    setEquippedSlots((current) => ({
      body: inventorySlugs[index] && current.body === inventorySlugs[index] ? null : current.body,
      mainHand: inventorySlugs[index] && current.mainHand === inventorySlugs[index] ? null : current.mainHand,
      offHand: inventorySlugs[index] && current.offHand === inventorySlugs[index] ? null : current.offHand,
    }))
  }

  function equipItem(slot: 'body' | 'mainHand' | 'offHand', slug: string) {
    if (slot === 'offHand' && isMainHandTwoHanded) {
      return
    }

    setInventorySlugs((currentInventory) => {
      const item = slug ? equipmentBySlug.get(slug) : null
      let nextInventory = [...currentInventory]
      let nextSlots: EquippedSlots = { ...equippedSlots }

      const clearSlot = (slotKey: keyof EquippedSlots) => {
        const equipped = nextSlots[slotKey]
        if (!equipped) {
          return
        }
        nextSlots = { ...nextSlots, [slotKey]: null }
        nextInventory = [...nextInventory, equipped]
      }

      if (slot === 'body') {
        clearSlot('body')
        if (slug && item) {
          nextInventory = removeOneFromInventory(nextInventory, slug)
          nextSlots.body = slug
        }
      } else if (slot === 'mainHand') {
        clearSlot('mainHand')
        if (isMainHandTwoHanded) {
          nextSlots.offHand = null
        }

        if (slug && item) {
          nextInventory = removeOneFromInventory(nextInventory, slug)
          nextSlots.mainHand = slug
          if (item.isTwoHanded) {
            clearSlot('offHand')
          }
        }
      } else {
        clearSlot('offHand')
        if (slug && item && !item.isTwoHanded) {
          nextInventory = removeOneFromInventory(nextInventory, slug)
          nextSlots.offHand = slug
        }
      }

      setEquippedSlots(nextSlots)
      return nextInventory
    })
  }

  function resolveSpellDetails(spellKey: string): SpellModalState {
    const bySlug = spellBySlug.get(spellKey)
    if (bySlug) {
      return {
        name: bySlug.name,
        circle: bySlug.spellLevel ?? 0,
        minLevel: bySlug.minCharacterLevel ?? 1,
        classes: (bySlug.classSlugs ?? []).map(translateClassSlug),
        summary: bySlug.summary,
        description: bySlug.description,
      }
    }

    const byName = spellCatalog.find((spell) => spell.name.toLowerCase() === spellKey.toLowerCase())
    if (byName) {
      return {
        name: byName.name,
        circle: byName.spellLevel ?? 0,
        minLevel: byName.minCharacterLevel ?? 1,
        classes: (byName.classSlugs ?? []).map(translateClassSlug),
        summary: byName.summary,
        description: byName.description,
      }
    }

    return {
      name: humanizeSpellFallback(spellKey),
      circle: 0,
      minLevel: 1,
      classes: [],
      summary: 'Описание пока недоступно.',
      description: 'Для этого заклинания пока нет карточки в справочнике правил.',
    }
  }

  function addSpell(spellSlug: string) {
    setKnownSpellsDraft((current) => {
      if (current.includes(spellSlug)) {
        return current
      }

      setRecentlyAddedSpell(spellSlug)
      window.setTimeout(() => setRecentlyAddedSpell((active) => (active === spellSlug ? null : active)), 900)
      return [...current, spellSlug]
    })
  }

  function removeSpell(spellSlug: string) {
    setKnownSpellsDraft((current) => current.filter((item) => item !== spellSlug))
  }

  async function saveCharacterChanges() {
    if (!canEditPage || !character) {
      return
    }

    setIsSaving(true)
    setSaveStatus(null)

    try {
      const editableCharacter = character
      const payload = {
        name: editableCharacter.name,
        raceId: editableCharacter.raceId,
        classId: editableCharacter.classId,
        backgroundId: editableCharacter.backgroundId,
        level: editableCharacter.level,
        alignment: editableCharacter.alignment,
        notes: notesDraft,
        baseAbilities: editableCharacter.baseAbilities,
        bonusAbilitySelections: editableCharacter.bonusAbilitySelections,
        raceSkillSelections: editableCharacter.raceSkillSelections,
        classSkillSelections: editableCharacter.classSkillSelections,
        spells: knownSpellsDraft,
        inventory: [
          ...inventorySlugs.map((slug) => `item:${slug}`),
          `equip:body=${equippedSlots.body ?? ''};main=${equippedSlots.mainHand ?? ''};off=${equippedSlots.offHand ?? ''}`,
        ],
      }

      const updated = await updateCharacter(editableCharacter.id, payload)
      setCharacter(updated)
      setNotesDraft(updated.notes ?? '')
      setKnownSpellsDraft(updated.knownSpells ?? [])
      setInventorySlugs(updated.inventory.filter((entry) => entry.startsWith('item:')).map((entry) => entry.slice(5)))
      setEquippedSlots(parseEquipEntry(updated.inventory.find((entry) => entry.startsWith('equip:')) ?? ''))
      setSaveStatus('Изменения сохранены.')
    } catch {
      setSaveStatus('Не удалось сохранить изменения.')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="stack">
      <div className="character-detail-top-actions">
        <Link to="/characters" className="secondary-button">
          Назад к персонажам
        </Link>
      </div>

      <section className="character-hero">
        <div className="character-hero__identity">
          <img
            className="character-detail__portrait"
            src={getCharacterPortrait(character.name, character.race, character.className)}
            alt={`Портрет персонажа ${character.name}`}
          />
          <div>
          <h2>{character.name}</h2>
          <p className="section-text">
            {character.race} • {character.className}
            {character.subclass ? ` • ${character.subclass}` : ''} •{' '}
            {character.background || 'Без предыстории'}
          </p>
          </div>
        </div>

        <div className="badge-cluster">
          <span className="pill">Уровень {character.level}</span>
          {character.alignment ? <span className="pill">{character.alignment}</span> : null}
          {canEditPage ? (
            <Link to={`/characters/${character.id}/edit/identity`} className="secondary-button">
              Редактировать
            </Link>
          ) : null}
        </div>
      </section>

      <article className="surface-card">
        <div className="section-header-row">
          <h3>Броски кубов</h3>
        </div>
        <div className="skill-pick-grid">
          {[4, 6, 8, 10, 12, 20, 100].map((sides) => (
            <button key={sides} type="button" className="bonus-chip" onClick={() => registerRoll(`Свободный бросок d${sides}`, sides, 0)}>
              d{sides}
            </button>
          ))}
        </div>
      </article>

      <section className="grid detail-layout">
        <article className="surface-card">
          <h3>Характеристики</h3>
          <div className="ability-grid">
            {character.abilities.map((ability) => (
              <button
                type="button"
                className="ability-card ability-card--interactive button-reset"
                key={ability.key}
                onClick={() => registerRoll(`Проверка: ${translateAbility(ability.key)}`, 20, ability.modifier)}
              >
                <span className="ability-card__name">{translateAbility(ability.key)}</span>
                <div className="ability-card__values">
                  <strong>{ability.score}</strong>
                  <small>{ability.modifier >= 0 ? `+${ability.modifier}` : ability.modifier}</small>
                </div>
              </button>
            ))}
          </div>
        </article>

        <article className="surface-card">
          <h3>Ключевые показатели</h3>
          <div className="compact-stats">
            <div className="key-metric">
              <span>Класс доспеха</span>
              <strong>{character.armorClass}</strong>
            </div>
            <div className="key-metric">
              <span>Хиты</span>
              <strong>{character.hitPoints}</strong>
            </div>
            <div className="compact-stats__weapon">
              <span className="compact-stats__weapon-title">Боевой блок</span>
              <div className="weapon-damage-lines">
                <button
                  type="button"
                  className="button-reset rollable-list-button weapon-roll-button"
                  onClick={() => registerWeaponAttackRoll('mainHand')}
                  disabled={!equippedSlots.mainHand}
                >
                  Попадание • Правая рука
                </button>
                <button
                  type="button"
                  className="button-reset rollable-list-button weapon-roll-button"
                  onClick={() => registerWeaponDamageRoll('mainHand')}
                  disabled={!equippedSlots.mainHand}
                >
                  Урон • Правая: {mainHandDamage}
                </button>
                <button
                  type="button"
                  className="button-reset rollable-list-button weapon-roll-button"
                  onClick={() => registerWeaponAttackRoll('offHand')}
                  disabled={!equippedSlots.offHand}
                >
                  Попадание • Левая рука
                </button>
                <button
                  type="button"
                  className="button-reset rollable-list-button weapon-roll-button"
                  onClick={() => registerWeaponDamageRoll('offHand')}
                  disabled={!equippedSlots.offHand}
                >
                  Урон • Левая: {offHandDamage}
                </button>
              </div>
            </div>
            <div className="key-metric">
              <span>Скорость</span>
              <strong>{character.speed}</strong>
            </div>
            <div className="key-metric">
              <span>Инициатива</span>
              <button
                type="button"
                className="button-reset key-metric-action"
                onClick={registerInitiativeRoll}
              >
                {initiativeModifier >= 0 ? `+${initiativeModifier}` : initiativeModifier}
              </button>
            </div>
            <div className="key-metric">
              <span>Бонус мастерства</span>
              <strong>+{character.proficiencyBonus}</strong>
            </div>
            <div className="key-metric">
              <span>Пассивная внимательность</span>
              <strong>{character.passivePerception}</strong>
            </div>
          </div>
        </article>

        <article className="surface-card">
          <h3>Навыки</h3>
          <ul className="plain-list sheet-list">
            {allSkills.map((skill) => (
              <li key={skill.skillId}>
                <button
                  type="button"
                  className="button-reset rollable-list-button sheet-list-button"
                  onClick={() => registerRoll(`Навык: ${translateSkill(skill.skillId)}`, 20, skill.level)}
                >
                  {translateSkill(skill.skillId)} {skill.level >= 0 ? `+${skill.level}` : skill.level}
                </button>
              </li>
            ))}
          </ul>
        </article>

        <article className="surface-card">
          <h3>Спасброски</h3>
          <ul className="plain-list sheet-list">
            {character.savingThrows.map((savingThrow) => (
              <li key={savingThrow.ability}>
                <button
                  type="button"
                  className="button-reset rollable-list-button sheet-list-button"
                  onClick={() => registerRoll(`Спасбросок: ${translateAbility(savingThrow.ability)}`, 20, savingThrow.bonus)}
                >
                  {translateAbility(savingThrow.ability)} {savingThrow.bonus >= 0 ? `+${savingThrow.bonus}` : savingThrow.bonus}
                  {savingThrow.isProficient ? ' • владение' : ''}
                </button>
              </li>
            ))}
          </ul>
        </article>

        <article className="surface-card">
          <h3>Ячейки заклинаний</h3>
          <ul className="plain-list sheet-list">
            {character.spellSlots.length > 0
              ? character.spellSlots.map((slot) => (
                <li key={slot.spellLevel}>
                  <span className="sheet-list-static">Круг {slot.spellLevel}: {slot.slots}</span>
                </li>
              ))
              : <li>Нет ячеек</li>}
          </ul>
        </article>

        <article className="surface-card">
          <div className="section-header-row">
            <h3>Известные заклинания</h3>
            {canEditPage ? (
              <button type="button" className="secondary-button button-reset compact-plus-button" onClick={() => setIsSpellPickerOpen(true)}>
                +
              </button>
            ) : null}
          </div>
          <div className="skill-pick-grid">
            {knownSpellsDraft.length > 0 ? (
              knownSpellsDraft.map((spellKey, index) => (
                <span key={`${spellKey}-${index}`} className={`bonus-chip static ${recentlyAddedSpell === spellKey ? 'selection-flash' : ''}`}>
                  <button
                    type="button"
                    className="button-reset spell-chip-trigger"
                    onClick={() => setSpellModal(resolveSpellDetails(spellKey))}
                  >
                    {knownSpellsRu[index]}
                  </button>
                  {canEditPage ? (
                    <button
                      type="button"
                      className="button-reset icon-remove-button"
                      aria-label="Удалить заклинание"
                      onClick={() => removeSpell(spellKey)}
                    >
                      ×
                    </button>
                  ) : null}
                </span>
              ))
            ) : <span className="muted">Нет данных</span>}
          </div>
          {canEditPage ? (
            <button type="button" className="primary-button button-reset" onClick={() => void saveCharacterChanges()} disabled={isSaving}>
              {isSaving ? 'Сохранение...' : 'Сохранить заклинания'}
            </button>
          ) : null}
        </article>

        <article className="surface-card">
          <div className="section-header-row">
            <h3>Инвентарь</h3>
            {canEditPage ? (
              <button type="button" className="secondary-button button-reset compact-plus-button" onClick={() => setIsInventoryPickerOpen(true)}>
                +
              </button>
            ) : null}
          </div>
          <div className="stack">
            <div className="skill-pick-grid">
              {inventoryWithIndexes.length > 0 ? inventoryWithIndexes.map(({ slug, index, key }) => (
                <span key={key} className={`bonus-chip static ${recentlyAddedInventoryKey === key ? 'selection-flash' : ''}`}>
                  {equipmentBySlug.get(slug)?.name ?? slug}
                  {canEditPage ? (
                    <button
                      type="button"
                      className="button-reset icon-remove-button"
                      aria-label="Удалить предмет"
                      onClick={() => removeInventoryItem(index)}
                    >
                      ×
                    </button>
                  ) : null}
                </span>
              )) : <span className="muted">Предметы не добавлены.</span>}
            </div>

            <div className="form-grid compact">
              <label>
                Тело
                <select
                  className="app-select"
                  value={equippedSlots.body ?? ''}
                  onChange={(event) => equipItem('body', event.target.value)}
                  disabled={!canEditPage}
                >
                  <option value="">Не экипировано</option>
                  {bodyEquipOptions.map((slug, index) => (
                    <option key={`${slug}-body-${index}`} value={slug}>{equipmentBySlug.get(slug)?.name ?? slug}</option>
                  ))}
                </select>
              </label>
              <label>
                Правая рука
                <select
                  className="app-select"
                  value={equippedSlots.mainHand ?? ''}
                  onChange={(event) => equipItem('mainHand', event.target.value)}
                  disabled={!canEditPage}
                >
                  <option value="">Не экипировано</option>
                  {mainHandEquipOptions.map((slug, index) => (
                    <option key={`${slug}-main-${index}`} value={slug}>{equipmentBySlug.get(slug)?.name ?? slug}</option>
                  ))}
                </select>
              </label>
              <label>
                Левая рука
                <select
                  className="app-select"
                  value={equippedSlots.offHand ?? ''}
                  onChange={(event) => equipItem('offHand', event.target.value)}
                  disabled={!canEditPage || isMainHandTwoHanded}
                >
                  <option value="">Не экипировано</option>
                  {offHandEquipOptions.map((slug, index) => (
                    <option key={`${slug}-off-${index}`} value={slug}>{equipmentBySlug.get(slug)?.name ?? slug}</option>
                  ))}
                </select>
                {isMainHandTwoHanded ? <small className="muted">Левая рука занята двуручным оружием.</small> : null}
              </label>
            </div>
            {canEditPage ? (
              <button type="button" className="primary-button button-reset" onClick={() => void saveCharacterChanges()} disabled={isSaving}>
                {isSaving ? 'Сохранение...' : 'Сохранить инвентарь'}
              </button>
            ) : null}
          </div>
        </article>

        <article className="surface-card">
          <h3>Заметки</h3>
          {canEditPage ? (
            <div className="stack">
              <textarea
                value={notesDraft}
                onChange={(event) => setNotesDraft(event.target.value)}
                placeholder="Добавьте заметки по персонажу..."
                rows={6}
              />
              <button type="button" className="primary-button button-reset" onClick={() => void saveCharacterChanges()} disabled={isSaving}>
                {isSaving ? 'Сохранение...' : 'Сохранить заметки'}
              </button>
            </div>
          ) : <p>{character.notes || 'Пока без заметок.'}</p>}
          {saveStatus ? <p className={saveStatus.includes('Не удалось') ? 'inline-error' : 'success-text'}>{saveStatus}</p> : null}
        </article>
      </section>

      {spellModal ? (
        <div className="modal-overlay" role="presentation" onClick={() => setSpellModal(null)}>
          <div className="modal-card spell-modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
          <div className="modal-header">
              <div>
                <h3>{spellModal.name}</h3>
                <p className="section-text">
                  Круг {spellModal.circle} • Мин. уровень {spellModal.minLevel}
                </p>
              </div>
              <button type="button" className="secondary-button button-reset" onClick={() => setSpellModal(null)}>
                Закрыть
              </button>
            </div>
            <div className="spell-meta-grid">
              <div className="status-card">
                <span>Круг</span>
                <strong>{spellModal.circle}</strong>
              </div>
              <div className="status-card">
                <span>Мин. уровень</span>
                <strong>{spellModal.minLevel}</strong>
              </div>
              <div className="status-card">
                <span>Классы</span>
                <strong>{spellModal.classes.length > 0 ? spellModal.classes.join(', ') : '—'}</strong>
              </div>
            </div>
            {spellModal.summary ? (
              <article className="surface-card spell-description-block">
                <h4>Кратко</h4>
                <p>{spellModal.summary}</p>
              </article>
            ) : null}
            {spellModal.description ? (
              <article className="surface-card spell-description-block">
                <h4>Описание</h4>
                <p>{spellModal.description}</p>
              </article>
            ) : null}
          </div>
        </div>
      ) : null}

      {canEditPage && isSpellPickerOpen ? (
        <div className="modal-overlay" role="presentation" onClick={() => setIsSpellPickerOpen(false)}>
          <div className="modal-card spell-modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3>Добавить заклинания</h3>
                <p className="section-text">Выбери доступные заклинания класса.</p>
              </div>
              <button type="button" className="secondary-button button-reset" onClick={() => setIsSpellPickerOpen(false)}>
                Закрыть
              </button>
            </div>
            <label className="full-span">
              Поиск заклинания
              <input
                className="app-search-input spell-search-input"
                value={spellSearch}
                onChange={(event) => setSpellSearch(event.target.value)}
                placeholder="Щит, Огненный шар..."
              />
            </label>
            <div className="stack">
              {availableSpells.map((spell) => (
                <article key={spell.slug} className={`choice-card slim choice-card--static ${knownSpellsDraft.includes(spell.slug) ? 'selected' : ''}`}>
                  <button
                    type="button"
                    className="info-icon-button"
                    aria-label={spell.name}
                    onClick={() =>
                      setSpellModal({
                        name: spell.name,
                        circle: spell.spellLevel ?? 0,
                        minLevel: spell.minCharacterLevel ?? 1,
                        classes: (spell.classSlugs ?? []).map(translateClassSlug),
                        summary: spell.summary,
                        description: spell.description,
                      })
                    }
                  >
                    i
                  </button>
                  <button
                    type="button"
                    className={`choice-card__main ${recentlyAddedSpell === spell.slug ? 'selection-flash' : ''}`}
                    onClick={() => addSpell(spell.slug)}
                  >
                    <strong>{spell.name}</strong>
                    <small>Круг {spell.spellLevel ?? 0} • мин. уровень {spell.minCharacterLevel ?? 1}</small>
                  </button>
                </article>
              ))}
            </div>
          </div>
        </div>
      ) : null}

      {canEditPage && isInventoryPickerOpen ? (
        <div className="modal-overlay" role="presentation" onClick={() => setIsInventoryPickerOpen(false)}>
          <div className="modal-card spell-modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3>Добавить предметы</h3>
                <p className="section-text">Выбери предметы для инвентаря.</p>
              </div>
              <button type="button" className="secondary-button button-reset" onClick={() => setIsInventoryPickerOpen(false)}>
                Закрыть
              </button>
            </div>
            <label className="full-span">
              Поиск предмета
              <input
                className="app-search-input"
                value={inventorySearch}
                onChange={(event) => setInventorySearch(event.target.value)}
                placeholder="Лук, щит, верёвка..."
              />
            </label>
            <div className="skill-pick-grid">
              {filteredEquipmentCatalog.map((item) => (
                <button
                  key={item.slug}
                  type="button"
                  className={`skill-toggle ${recentlyAddedInventorySlug === item.slug ? 'selected selection-flash' : ''}`}
                  onClick={() => addInventoryItem(item.slug)}
                >
                  {item.name}
                </button>
              ))}
            </div>
          </div>
        </div>
      ) : null}

      {rollHistory.length > 0 ? (
        <aside className="roll-toast">
          {lastRoll && isLastRollVisible ? (
            <article className="roll-toast__last">
              <strong>{lastRoll.label}</strong>
              <p>
                {lastRoll.expression}: {lastRoll.diceResult} {lastRoll.modifier >= 0 ? `+ ${lastRoll.modifier}` : `- ${Math.abs(lastRoll.modifier)}`} = {lastRoll.total}
              </p>
            </article>
          ) : null}
          <div className="roll-toast__history">
            <button
              type="button"
              className="button-reset roll-toast__history-toggle"
              onClick={() => setIsRollHistoryOpen((current) => !current)}
            >
              <strong>Недавние броски</strong>
              <span>{isRollHistoryOpen ? 'Свернуть' : 'Развернуть'}</span>
            </button>
            {isRollHistoryOpen ? (
              <ul className="plain-list">
                {rollHistory.slice(0, 6).map((roll) => (
                  <li key={roll.id}>
                    {roll.createdAt} • {roll.label}: {roll.expression} {roll.diceResult} {roll.modifier >= 0 ? `+ ${roll.modifier}` : `- ${Math.abs(roll.modifier)}`} = {roll.total}
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
        </aside>
      ) : null}
    </div>
  )
}
