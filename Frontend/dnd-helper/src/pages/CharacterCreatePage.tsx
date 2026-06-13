import { useEffect, useMemo, useState } from 'react'
import { Navigate, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../features/auth/model/AuthProvider'
import {
  createCharacter,
  getCharacterById,
  getCharacterOptions,
  updateCharacter,
} from '../features/characters/api/charactersApi'
import { getEquipmentCatalog, getRulesSpells } from '../features/rules/api/rulesApi'
import type {
  ApiValidationError,
  BackgroundOption,
  CharacterOptions,
  CharacterPayload,
  ClassOption,
  EquipmentCatalogItem,
  RaceOption,
  RuleSpellItem,
} from '../types/character'
import {
  abilityOrder,
  abilityScoreMax,
  abilityScoreMin,
  buildOptionOverview,
  draftStorageKey,
  extractFeatureLevel,
  formatSkillWithAbility,
  getHitPoints,
  getModifier,
  getProficiencyBonus,
  loadDraft,
  mapCharacterToDraft,
  parseEquipFromInventory,
  splitMultiline,
  steps,
  uniqueValues,
  type CharacterDraft,
  type DraftValidationErrors,
  type EquipmentSlots,
  type FeatureInfoModalState,
  type InfoModalState,
  type ProficiencyItemsModalState,
  type SpellInfoModalState,
  type StepKey,
} from '../features/character-create/model/characterCreateModel'
import { CharacterBuilderHeader } from '../widgets/character-create/ui/CharacterBuilderHeader'
import { AbilityStepPanel } from '../widgets/character-create/ui/AbilityStepPanel'
import { IdentityStepPanel } from '../widgets/character-create/ui/IdentityStepPanel'
import { RaceStepPanel } from '../widgets/character-create/ui/RaceStepPanel'
import { BackgroundStepPanel } from '../widgets/character-create/ui/BackgroundStepPanel'
import { ClassStepPanel } from '../widgets/character-create/ui/ClassStepPanel'
import { ReviewStepPanel } from '../widgets/character-create/ui/ReviewStepPanel'
import { SpellsStepPanel } from '../widgets/character-create/ui/SpellsStepPanel'
import { InventoryStepPanel } from '../widgets/character-create/ui/InventoryStepPanel'
import { ProficiencyItemsModal } from '../widgets/character-create/ui/ProficiencyItemsModal'
import { SpellInfoModal } from '../widgets/character-create/ui/SpellInfoModal'
import { FeatureInfoModal } from '../widgets/character-create/ui/FeatureInfoModal'
import { CharacterOptionInfoModal } from '../widgets/character-create/ui/CharacterOptionInfoModal'
import {
  translateAbility,
  translateSkill,
} from '../utils/characterPresentation'

export function CharacterCreatePage() {
  const { user, isLoading: isAuthLoading } = useAuth()
  const { step = 'identity', id } = useParams()
  const navigate = useNavigate()
  const isEditMode = Boolean(id)
  const [options, setOptions] = useState<CharacterOptions | null>(null)
  const [draft, setDraft] = useState<CharacterDraft>(() => loadDraft(isEditMode))
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [validationErrors, setValidationErrors] = useState<DraftValidationErrors>({})
  const [infoModal, setInfoModal] = useState<InfoModalState | null>(null)
  const [equipmentCatalog, setEquipmentCatalog] = useState<EquipmentCatalogItem[]>([])
  const [inventorySearch, setInventorySearch] = useState('')
  const [isInventoryPickerOpen, setIsInventoryPickerOpen] = useState(false)
  const [inventorySlugs, setInventorySlugs] = useState<string[]>([])
  const [spellCatalog, setSpellCatalog] = useState<RuleSpellItem[]>([])
  const [spellSearch, setSpellSearch] = useState('')
  const [isSpellPickerOpen, setIsSpellPickerOpen] = useState(false)
  const [equippedSlots, setEquippedSlots] = useState<{ body: string | null; mainHand: string | null; offHand: string | null }>({
    body: null,
    mainHand: null,
    offHand: null,
  })
  const [recentlyAddedSpell, setRecentlyAddedSpell] = useState<string | null>(null)
  const [recentlyAddedInventoryKey, setRecentlyAddedInventoryKey] = useState<string | null>(null)
  const [recentlyAddedInventorySlug, setRecentlyAddedInventorySlug] = useState<string | null>(null)
  const [modalTab, setModalTab] = useState<'overview' | 'features' | 'proficiencies'>('overview')
  const [expandedModalFeature, setExpandedModalFeature] = useState<string | null>(null)
  const [selectedModalFeatureLevel, setSelectedModalFeatureLevel] = useState<number | null>(null)
  const [expandedClassFeatureLevel, setExpandedClassFeatureLevel] = useState<string | null>(null)
  const [featureInfoModal, setFeatureInfoModal] = useState<FeatureInfoModalState | null>(null)
  const [spellInfoModal, setSpellInfoModal] = useState<SpellInfoModalState | null>(null)
  const [proficiencyItemsModal, setProficiencyItemsModal] = useState<ProficiencyItemsModalState | null>(null)

  const currentStep = steps.includes(step as StepKey) ? (step as StepKey) : null

  useEffect(() => {
    if (!user) {
      setIsLoading(false)
      return
    }

    let isCancelled = false

    async function loadInitialData() {
      try {
        const [response, equipment, spells] = await Promise.all([getCharacterOptions(), getEquipmentCatalog(), getRulesSpells()])
        if (isCancelled) {
          return
        }

        setOptions(response)
        setEquipmentCatalog(equipment)
        setSpellCatalog(spells)

        if (isEditMode && id) {
          const character = await getCharacterById(id)
          if (isCancelled) {
            return
          }

          setDraft(mapCharacterToDraft(character, response))
          setInventorySlugs(character.inventory.filter((entry) => entry.startsWith('item:')).map((entry) => entry.slice(5)))
          setEquippedSlots(parseEquipFromInventory(character.inventory))
        } else {
          setDraft((current) => ({
            ...current,
            raceId: current.raceId || response.races[0]?.id || '',
            classId: current.classId || response.classes[0]?.id || '',
            backgroundId: current.backgroundId || response.backgrounds[0]?.id || '',
          }))
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить данные конструктора.')
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadInitialData()

    return () => {
      isCancelled = true
    }
  }, [id, isEditMode, user])

  const equipmentMap = useMemo(
    () => new Map(equipmentCatalog.map((item) => [item.slug, item])),
    [equipmentCatalog],
  )

  const filteredEquipmentCatalog = useMemo(
    () =>
      equipmentCatalog.filter((item) =>
        `${item.name} ${item.category ?? ''} ${item.subcategory ?? ''}`.toLowerCase().includes(inventorySearch.toLowerCase()),
      ),
    [equipmentCatalog, inventorySearch],
  )

  const selectedSpells = useMemo(() => splitMultiline(draft.spellsText), [draft.spellsText])
  const availableSpells = useMemo(
    () =>
      spellCatalog.filter((spell) =>
        (spell.classSlugs ?? []).includes(draft.classId) &&
        (spell.minCharacterLevel ?? 1) <= draft.level &&
        spell.name.toLowerCase().includes(spellSearch.toLowerCase()),
      ),
    [draft.classId, draft.level, spellCatalog, spellSearch],
  )

  useEffect(() => {
    if (typeof window !== 'undefined' && !isEditMode) {
      window.sessionStorage.setItem(draftStorageKey, JSON.stringify(draft))
    }
  }, [draft, isEditMode])

  const selectedRace = useMemo<RaceOption | null>(
    () => options?.races.find((item) => item.id === draft.raceId) ?? null,
    [draft.raceId, options],
  )
  const selectedClass = useMemo<ClassOption | null>(
    () => options?.classes.find((item) => item.id === draft.classId) ?? null,
    [draft.classId, options],
  )
  const selectedBackground = useMemo(
    () => options?.backgrounds.find((item) => item.id === draft.backgroundId) ?? null,
    [draft.backgroundId, options],
  )
  const skillAbilityLookup = useMemo(() => options?.skillAbilityMap ?? {}, [options])

  const groupedRaces = useMemo(() => {
    const groups = new Map<string, RaceOption[]>()

    options?.races.forEach((race) => {
      const items = groups.get(race.parentRace) ?? []
      items.push(race)
      groups.set(race.parentRace, items)
    })

    return Array.from(groups.entries())
  }, [options])


  const fixedSkillProficiencies = useMemo(
    () =>
      uniqueValues([
        ...(selectedRace?.grantedSkillProficiencies ?? []),
        ...(selectedBackground?.grantedSkillProficiencies ?? []),
      ]),
    [selectedBackground, selectedRace],
  )

  useEffect(() => {
    if (!selectedRace || !selectedClass) {
      return
    }

    setDraft((current) => {
      const nextBonusSelections = selectedRace.bonusChoiceRule
        ? current.bonusAbilitySelections
            .filter((ability) => selectedRace.bonusChoiceRule?.allowedAbilities.includes(ability))
            .slice(0, selectedRace.bonusChoiceRule.count)
        : []

      const nextRaceSelections = selectedRace.skillChoiceRule
        ? current.raceSkillSelections
            .filter((skill) => selectedRace.skillChoiceRule?.availableSkills.includes(skill))
            .slice(0, selectedRace.skillChoiceRule.count)
        : []

      const nextClassSelections = current.classSkillSelections
        .filter((skill) => selectedClass.skillChoiceRule.availableSkills.includes(skill))
        .filter((skill) => !nextRaceSelections.includes(skill))
        .slice(0, selectedClass.skillChoiceRule.count)

      return {
        ...current,
        bonusAbilitySelections: nextBonusSelections,
        raceSkillSelections: nextRaceSelections,
        classSkillSelections: nextClassSelections,
      }
    })
  }, [selectedClass, selectedRace])


  const bonusMap = useMemo(() => {
    const map = new Map<string, number>()
    abilityOrder.forEach((ability) => map.set(ability, 0))

    selectedRace?.bonuses.forEach((bonus) => {
      map.set(bonus.ability, (map.get(bonus.ability) ?? 0) + bonus.value)
    })

    if (selectedRace?.bonusChoiceRule) {
      draft.bonusAbilitySelections.forEach((ability) => {
        map.set(
          ability,
          (map.get(ability) ?? 0) + selectedRace.bonusChoiceRule!.bonusValue,
        )
      })
    }

    return map
  }, [draft.bonusAbilitySelections, selectedRace])

  const computedAbilities = useMemo(
    () =>
      draft.baseAbilities.map((ability) => {
        const bonus = bonusMap.get(ability.key) ?? 0
        const total = ability.score + bonus

        return {
          key: ability.key,
          baseScore: ability.score,
          bonus,
          total,
          modifier: getModifier(total),
        }
      }),
    [bonusMap, draft.baseAbilities],
  )

  const proficiencyBonus = getProficiencyBonus(draft.level)
  const dexterityModifier = computedAbilities.find((item) => item.key === 'DEX')?.modifier ?? 0
  const constitutionModifier = computedAbilities.find((item) => item.key === 'CON')?.modifier ?? 0
  const wisdomModifier = computedAbilities.find((item) => item.key === 'WIS')?.modifier ?? 0
  const passivePerception = 10 + wisdomModifier + (fixedSkillProficiencies.includes('Perception') || draft.raceSkillSelections.includes('Perception') || draft.classSkillSelections.includes('Perception') ? proficiencyBonus : 0)
  const estimatedArmorClass = 10 + dexterityModifier
  const estimatedHitPoints = selectedClass
    ? getHitPoints(selectedClass.hitDie, draft.level, constitutionModifier)
    : 0

  const combinedSkillProficiencies = useMemo(
    () =>
      uniqueValues([
        ...fixedSkillProficiencies,
        ...draft.raceSkillSelections,
        ...draft.classSkillSelections,
      ]),
    [draft.classSkillSelections, draft.raceSkillSelections, fixedSkillProficiencies],
  )

  const allSkillsPreview = useMemo(() => {
    return Object.entries(skillAbilityLookup).map(([skillId, abilityKey]) => {
      const abilityModifier = computedAbilities.find((item) => item.key === abilityKey)?.modifier ?? 0
      const proficient = combinedSkillProficiencies.includes(skillId)
      return {
        skillId,
        label: translateSkill(skillId),
        level: abilityModifier + (proficient ? proficiencyBonus : 0),
        proficient,
      }
    })
  }, [combinedSkillProficiencies, computedAbilities, proficiencyBonus, skillAbilityLookup])

  const armorProficiencyEntries = useMemo(
    () => selectedClass?.proficiencyGroups?.['Доспехи'] ?? [],
    [selectedClass?.proficiencyGroups],
  )

  const weaponProficiencyEntries = useMemo(
    () => selectedClass?.proficiencyGroups?.['Оружие'] ?? [],
    [selectedClass?.proficiencyGroups],
  )

  const proficiencyTypeLabels = useMemo(
    () =>
      new Set([
        'Лёгкие доспехи',
        'Средние доспехи',
        'Тяжёлые доспехи',
        'Щиты',
        'Простое оружие',
        'Воинское оружие',
      ]),
    [],
  )

  function resolveProficiencyItems(label: string) {
    const normalized = label.trim().toLowerCase()

    const byName = (name: string) => equipmentCatalog
      .filter((item) => item.name.toLowerCase() === name.toLowerCase())
      .map((item) => item.name)

    const allArmor = () => equipmentCatalog
      .filter((item) => item.category === 'Armor')
      .map((item) => item.name)
    const allSimpleWeapons = () => equipmentCatalog
      .filter((item) => item.category === 'Simple Weapon')
      .map((item) => item.name)
    const allMartialWeapons = () => equipmentCatalog
      .filter((item) => item.category === 'Martial Weapon')
      .map((item) => item.name)

    const fromCatalog =
      normalized === 'лёгкие доспехи'
        ? equipmentCatalog.filter((item) => item.category === 'Armor' && item.subcategory === 'Light').map((item) => item.name)
        : normalized === 'средние доспехи'
          ? equipmentCatalog.filter((item) => item.category === 'Armor' && item.subcategory === 'Medium').map((item) => item.name)
          : normalized === 'тяжёлые доспехи'
            ? equipmentCatalog.filter((item) => item.category === 'Armor' && item.subcategory === 'Heavy').map((item) => item.name)
            : normalized === 'щиты'
              ? equipmentCatalog.filter((item) => item.isShield).map((item) => item.name)
              : normalized === 'простое оружие'
                ? allSimpleWeapons()
                : normalized === 'воинское оружие'
                  ? allMartialWeapons()
                  : normalized === 'все доспехи'
                    ? allArmor()
                    : normalized === 'оружие' || normalized === 'всё оружие' || normalized === 'все оружие'
                      ? [...allSimpleWeapons(), ...allMartialWeapons()]
                      : normalized === 'кинжалы'
                        ? byName('Кинжал')
                        : normalized === 'дротики'
                          ? byName('Дротик')
                          : normalized === 'пращи'
                            ? byName('Праща')
                            : normalized === 'посохи'
                              ? byName('Посох')
                              : normalized === 'лёгкие арбалеты'
                                ? byName('Лёгкий арбалет')
                                : normalized === 'ручные арбалеты'
                                  ? byName('Ручной арбалет')
                                  : normalized === 'длинные мечи'
                                    ? byName('Длинный меч')
                                    : normalized === 'короткие мечи'
                                      ? byName('Короткий меч')
                                      : byName(label)

    return uniqueValues(fromCatalog).sort((left, right) => left.localeCompare(right, 'ru'))
  }

  function buildProficiencyDisplayItems(entries: readonly string[]) {
    return entries.flatMap((entry) => {
      const isType = proficiencyTypeLabels.has(entry)
      const items = resolveProficiencyItems(entry)

      if (isType) {
        return [{ label: entry, interactive: true, items }]
      }

      if (items.length > 0) {
        return items.map((item) => ({ label: item, interactive: false, items: [item] }))
      }

      return [{ label: entry, interactive: false, items: [entry] }]
    })
  }

  const armorDisplayItems = useMemo(
    () => buildProficiencyDisplayItems(armorProficiencyEntries),
    [armorProficiencyEntries],
  )

  const weaponDisplayItems = useMemo(
    () => buildProficiencyDisplayItems(weaponProficiencyEntries),
    [weaponProficiencyEntries],
  )

  const isMainHandTwoHanded = useMemo(
    () => Boolean(equippedSlots.mainHand && equipmentMap.get(equippedSlots.mainHand)?.isTwoHanded),
    [equipmentMap, equippedSlots.mainHand],
  )

  const inventoryWithIndexes = useMemo(
    () => inventorySlugs.map((slug, index) => ({ slug, index, key: `${slug}-${index}` })),
    [inventorySlugs],
  )

  const availableClassFeatures = useMemo(
    () =>
      (selectedClass?.details ?? []).filter((feature) => {
        const featureLevel = extractFeatureLevel(feature.title)
        return featureLevel <= draft.level
      }),
    [draft.level, selectedClass?.details],
  )

  const groupedClassFeatures = useMemo(() => {
    const groups = new Map<number, string[]>()
    availableClassFeatures.forEach((feature) => {
      const featureNameMatch = feature.title.match(/^\s*\d+\s*уровень\s*:\s*(.+)$/i)
      const level = extractFeatureLevel(feature.title)
      const featureName = (featureNameMatch?.[1] ?? feature.title).trim()
      const list = groups.get(level) ?? []
      if (!list.includes(featureName)) {
        list.push(featureName)
      }
      groups.set(level, list)
    })
    return Array.from(groups.entries())
      .filter(([, features]) => features.length > 0)
      .sort((a, b) => a[0] - b[0])
  }, [availableClassFeatures])

  const computedSavingThrows = useMemo(
    () =>
      abilityOrder.map((ability) => {
        const modifier = computedAbilities.find((item) => item.key === ability)?.modifier ?? 0
        const isProficient = selectedClass?.savingThrowProficiencies.includes(ability) ?? false
        return {
          ability,
          bonus: modifier + (isProficient ? proficiencyBonus : 0),
          isProficient,
        }
      }),
    [computedAbilities, proficiencyBonus, selectedClass],
  )

  const validateStep = useMemo(() => {
    const result: DraftValidationErrors = {}

    if (!draft.name.trim()) {
      result.identity = ['Укажи имя персонажа.']
    } else if (draft.name.trim().length < 2) {
      result.identity = ['Имя должно быть не короче двух символов.']
    }

    if (draft.level < 1 || draft.level > 20) {
      result.identity = ['Уровень должен быть от 1 до 20.']
    }

    if (!selectedRace) {
      result.race = ['Выбери расу из книги игрока.']
    } else if (selectedRace.bonusChoiceRule) {
      const uniqueSelections = uniqueValues(draft.bonusAbilitySelections)
      if (
        uniqueSelections.length !== selectedRace.bonusChoiceRule.count ||
        uniqueSelections.some((ability) => !selectedRace.bonusChoiceRule?.allowedAbilities.includes(ability))
      ) {
        result.race = ['Проверь выбор характеристик для расового бонуса.']
      }
    }

    if (!selectedClass) {
      result.class = ['Выбери класс из книги игрока.']
    }

    if (!selectedBackground) {
      result.background = ['Выбери предысторию из книги игрока.']
    }

    if (
      draft.baseAbilities.length !== abilityOrder.length ||
      draft.baseAbilities.some((ability) => !abilityOrder.includes(ability.key as (typeof abilityOrder)[number])) ||
      draft.baseAbilities.some((ability) => ability.score < abilityScoreMin || ability.score > abilityScoreMax)
    ) {
      result.abilities = [`Базовые характеристики должны быть в диапазоне от ${abilityScoreMin} до ${abilityScoreMax}.`]
    }

    if (selectedRace?.skillChoiceRule) {
      const uniqueRaceSelections = uniqueValues(draft.raceSkillSelections)
      if (
        uniqueRaceSelections.length !== selectedRace.skillChoiceRule.count ||
        uniqueRaceSelections.some((skill) => !selectedRace.skillChoiceRule?.availableSkills.includes(skill))
      ) {
        result.race = ['Проверь выбор навыков расы.']
      }
    }

    const uniqueClassSelections = uniqueValues(draft.classSkillSelections)
    if (
      selectedClass &&
      (uniqueClassSelections.length !== selectedClass.skillChoiceRule.count ||
        uniqueClassSelections.some((skill) => !selectedClass.skillChoiceRule.availableSkills.includes(skill)))
    ) {
      result.class = ['Проверь выбор навыков класса.']
    }

    return result
  }, [
    draft.baseAbilities,
    draft.bonusAbilitySelections,
    draft.classSkillSelections,
    draft.level,
    draft.name,
    draft.raceSkillSelections,
    selectedBackground,
    selectedClass,
    selectedRace,
  ])

  function updateDraft<K extends keyof CharacterDraft>(key: K, value: CharacterDraft[K]) {
    setDraft((current) => ({ ...current, [key]: value }))
  }

  function getStepErrors(stepKey: StepKey) {
    return validationErrors[stepKey] ?? []
  }

  const hasBlockingErrors = useMemo(
    () => steps.some((stepKey) => (validateStep[stepKey]?.length ?? 0) > 0),
    [validateStep],
  )

  const allBlockingMessages = useMemo(
    () => steps.flatMap((stepKey) => validateStep[stepKey] ?? []),
    [validateStep],
  )

  function navigateToStep(stepKey: StepKey) {
    const basePath = isEditMode && id ? `/characters/${id}/edit` : '/characters/new'
    navigate(`${basePath}/${stepKey}`)
  }

  function applyValidationErrors(errors: DraftValidationErrors) {
    setValidationErrors(errors)
    const currentErrors = currentStep ? errors[currentStep] ?? [] : []
    const globalErrors = errors.global ?? []
    setError([...currentErrors, ...globalErrors][0] ?? null)
  }

  useEffect(() => {
    applyValidationErrors(validateStep)
  }, [validateStep])

  function getFieldStep(errorKey: string): StepKey | 'global' {
    const mapping: Record<string, StepKey> = {
      name: 'identity',
      level: 'identity',
      raceId: 'race',
      bonusAbilitySelections: 'race',
      classId: 'class',
      backgroundId: 'background',
      baseAbilities: 'abilities',
      raceSkillSelections: 'race',
      classSkillSelections: 'class',
    }

    return mapping[errorKey] ?? 'global'
  }

  function updateBaseAbility(key: string, score: number) {
    const clampedValue = Math.min(abilityScoreMax, Math.max(abilityScoreMin, Number.isNaN(score) ? 10 : score))
    setDraft((current) => ({
      ...current,
      baseAbilities: current.baseAbilities.map((ability) =>
        ability.key === key ? { ...ability, score: clampedValue } : ability,
      ),
    }))
  }

  function rollAbilityScore() {
    const rolls = Array.from({ length: 4 }, () => Math.floor(Math.random() * 6) + 1)
    return rolls
      .sort((left, right) => right - left)
      .slice(0, 3)
      .reduce((sum, value) => sum + value, 0)
  }

  function randomizeBaseAbilities() {
    setDraft((current) => ({
      ...current,
      baseAbilities: abilityOrder.map((key) => ({ key, score: rollAbilityScore() })),
    }))
  }

  function toggleBonusSelection(ability: string) {
    const bonusChoiceRule = selectedRace?.bonusChoiceRule
    if (!bonusChoiceRule) {
      return
    }

    setDraft((current) => {
      if (current.bonusAbilitySelections.includes(ability)) {
        return {
          ...current,
          bonusAbilitySelections: current.bonusAbilitySelections.filter((item) => item !== ability),
        }
      }

      if (current.bonusAbilitySelections.length >= bonusChoiceRule.count) {
        return {
          ...current,
          bonusAbilitySelections: [...current.bonusAbilitySelections.slice(1), ability],
        }
      }

      return {
        ...current,
        bonusAbilitySelections: [...current.bonusAbilitySelections, ability],
      }
    })
  }

  function toggleClassSkill(skillId: string) {
    if (!selectedClass || draft.raceSkillSelections.includes(skillId)) {
      return
    }

    setDraft((current) => {
      if (current.classSkillSelections.includes(skillId)) {
        return {
          ...current,
          classSkillSelections: current.classSkillSelections.filter((item) => item !== skillId),
        }
      }

      if (current.classSkillSelections.length >= selectedClass.skillChoiceRule.count) {
        return {
          ...current,
          classSkillSelections: [...current.classSkillSelections.slice(1), skillId],
        }
      }

      return {
        ...current,
        classSkillSelections: [...current.classSkillSelections, skillId],
      }
    })
  }

  function toggleRaceSkill(skillId: string) {
    const skillChoiceRule = selectedRace?.skillChoiceRule
    if (!skillChoiceRule) {
      return
    }

    setDraft((current) => {
      if (current.raceSkillSelections.includes(skillId)) {
        return {
          ...current,
          raceSkillSelections: current.raceSkillSelections.filter((item) => item !== skillId),
        }
      }

      if (current.raceSkillSelections.length >= skillChoiceRule.count) {
        return {
          ...current,
          raceSkillSelections: [...current.raceSkillSelections.slice(1), skillId],
        }
      }

      return {
        ...current,
        raceSkillSelections: [...current.raceSkillSelections, skillId],
      }
    })
  }

  function goToStep(direction: 1 | -1) {
    if (!currentStep) {
      return
    }

    if (direction > 0) {
      const currentErrors = getStepErrors(currentStep)
      if (currentErrors.length > 0) {
        setError(currentErrors[0])
        return
      }
    }

    const nextIndex = steps.indexOf(currentStep) + direction
    const nextStep = steps[nextIndex]
    if (nextStep) {
      navigateToStep(nextStep)
    }
  }

  async function handleSave() {
    const firstInvalidStep = steps.find((stepKey) => (validateStep[stepKey]?.length ?? 0) > 0)
    if (firstInvalidStep) {
      applyValidationErrors(validateStep)
      navigateToStep(firstInvalidStep)
      return
    }

    setIsSaving(true)
    setError(null)

    const payload: CharacterPayload = {
      name: draft.name,
      raceId: draft.raceId,
      classId: draft.classId,
      backgroundId: draft.backgroundId,
      level: draft.level,
      alignment: draft.alignment,
      notes: draft.notes,
      baseAbilities: draft.baseAbilities,
      bonusAbilitySelections: draft.bonusAbilitySelections,
      raceSkillSelections: draft.raceSkillSelections,
      classSkillSelections: draft.classSkillSelections,
      spells: splitMultiline(draft.spellsText),
      inventory: [
        ...inventorySlugs.map((slug) => `item:${slug}`),
        `equip:body=${equippedSlots.body ?? ''};main=${equippedSlots.mainHand ?? ''};off=${equippedSlots.offHand ?? ''}`,
      ],
    }

    try {
      const character = isEditMode && id
        ? await updateCharacter(id, payload)
        : await createCharacter(payload)

      setValidationErrors({})

      if (typeof window !== 'undefined' && !isEditMode) {
        window.sessionStorage.removeItem(draftStorageKey)
      }

      navigate(`/characters/${character.id}`)
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError

      if (apiError.errors) {
        const nextValidationErrors: DraftValidationErrors = {}

        Object.entries(apiError.errors).forEach(([key, messages]) => {
          const stepKey = getFieldStep(key)
          nextValidationErrors[stepKey] = [
            ...(nextValidationErrors[stepKey] ?? []),
            ...messages,
          ]
        })

        applyValidationErrors(nextValidationErrors)
      } else {
        setError(
          apiError.message ||
            (isEditMode ? 'Не удалось сохранить изменения.' : 'Не удалось создать персонажа.'),
        )
      }
    } finally {
      setIsSaving(false)
    }
  }

  function openRaceInfo(race: RaceOption) {
    const raceFeatures = race.details.filter((detail) => !detail.title.toLowerCase().includes('увеличение характеристик'))
    setInfoModal({
      title: race.name,
      subtitle: race.summary,
      overview: [
        { label: 'Описание', value: race.description ?? race.summary },
        ...buildOptionOverview(race, 'race'),
      ],
      features: raceFeatures,
      proficiencies: [
        {
          category: 'Бонусы характеристик',
          items: race.bonuses.map((bonus) => `${translateAbility(bonus.ability)} +${bonus.value}`),
        },
        {
          category: 'Языки',
          items: race.grantedLanguages ?? [],
        },
        {
          category: 'Навыки',
          items: race.grantedSkillProficiencies.map((skill) => formatSkillWithAbility(skill, skillAbilityLookup)),
        },
      ].filter((entry) => entry.items.length > 0),
    })
    setModalTab('overview')
    setExpandedModalFeature(null)
    setSelectedModalFeatureLevel(null)
  }

  function openClassInfo(characterClass: ClassOption) {
    setInfoModal({
      title: characterClass.name,
      subtitle: characterClass.summary,
      overview: [
        { label: 'Описание', value: characterClass.description ?? characterClass.summary },
        ...buildOptionOverview(characterClass, 'class'),
      ],
      features: characterClass.details,
      proficiencies: Object.entries(characterClass.proficiencyGroups ?? {}).map(([category, items]) => ({
        category,
        items,
      })).concat([
        {
          category: `Навыки на выбор (${characterClass.skillChoiceRule.count})`,
          items: characterClass.skillChoiceRule.availableSkills.map((skill) => formatSkillWithAbility(skill, skillAbilityLookup)),
        },
        {
          category: 'Обязательные навыки',
          items: [],
        },
      ]),
    })
    setModalTab('overview')
    setExpandedModalFeature(null)
    setSelectedModalFeatureLevel(null)
  }

  function openBackgroundInfo(background: BackgroundOption) {
    setInfoModal({
      title: background.name,
      subtitle: background.summary,
      overview: [{ label: 'Описание', value: background.description ?? background.summary }],
      features: background.details,
      proficiencies: [
        {
          category: 'Навыки',
          items: background.grantedSkillProficiencies.map((skill) => formatSkillWithAbility(skill, skillAbilityLookup)),
        },
      ],
    })
    setModalTab('overview')
    setExpandedModalFeature(null)
    setSelectedModalFeatureLevel(null)
  }

  function addToInventory(slugs: string[], itemSlug: string) {
    return [...slugs, itemSlug]
  }

  function removeOneFromInventory(slugs: string[], itemSlug: string) {
    const index = slugs.indexOf(itemSlug)
    if (index < 0) {
      return slugs
    }

    return slugs.filter((_, currentIndex) => currentIndex !== index)
  }

  function clearSlot(slots: EquipmentSlots, inventory: string[], slot: keyof EquipmentSlots) {
    const equipped = slots[slot]
    if (!equipped) {
      return { slots, inventory }
    }

    return {
      slots: { ...slots, [slot]: null },
      inventory: addToInventory(inventory, equipped),
    }
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
      const item = slug ? equipmentMap.get(slug) : null
      let nextInventory = [...currentInventory]
      let nextSlots: EquipmentSlots = { ...equippedSlots }

      if (slot === 'body') {
        const cleared = clearSlot(nextSlots, nextInventory, 'body')
        nextSlots = cleared.slots
        nextInventory = cleared.inventory

        if (slug && item) {
          nextInventory = removeOneFromInventory(nextInventory, slug)
          nextSlots.body = slug
        }
      } else if (slot === 'mainHand') {
        const clearedMain = clearSlot(nextSlots, nextInventory, 'mainHand')
        nextSlots = clearedMain.slots
        nextInventory = clearedMain.inventory

        if (isMainHandTwoHanded) {
          nextSlots.offHand = null
        }

        if (slug && item) {
          nextInventory = removeOneFromInventory(nextInventory, slug)
          nextSlots.mainHand = slug

          if (item.isTwoHanded) {
            const clearedOff = clearSlot(nextSlots, nextInventory, 'offHand')
            nextSlots = clearedOff.slots
            nextInventory = clearedOff.inventory
          }
        }
      } else {
        const cleared = clearSlot(nextSlots, nextInventory, 'offHand')
        nextSlots = cleared.slots
        nextInventory = cleared.inventory

        if (slug && item && !item.isTwoHanded) {
          nextInventory = removeOneFromInventory(nextInventory, slug)
          nextSlots.offHand = slug
        }
      }

      setEquippedSlots(nextSlots)
      return nextInventory
    })
  }

  function addSpell(slug: string) {
    if (selectedSpells.includes(slug)) {
      return
    }
    setRecentlyAddedSpell(slug)
    window.setTimeout(() => setRecentlyAddedSpell((active) => (active === slug ? null : active)), 900)
    updateDraft('spellsText', [...selectedSpells, slug].join('\n'))
  }

  function removeSpell(slug: string) {
    updateDraft('spellsText', selectedSpells.filter((item) => item !== slug).join('\n'))
  }

  const bodyEquipOptions = useMemo(() => {
    const fromInventory = inventorySlugs.filter((slug) => equipmentMap.get(slug)?.equipSlot === 'body')
    const equipped = equippedSlots.body ? [equippedSlots.body] : []
    return uniqueValues([...equipped, ...fromInventory])
  }, [equipmentMap, equippedSlots.body, inventorySlugs])

  const mainHandEquipOptions = useMemo(() => {
    const fromInventory = inventorySlugs.filter((slug) => {
      const slot = equipmentMap.get(slug)?.equipSlot
      return slot === 'hand' || slot === 'off-hand'
    })
    const equipped = equippedSlots.mainHand ? [equippedSlots.mainHand] : []
    return uniqueValues([...equipped, ...fromInventory])
  }, [equipmentMap, equippedSlots.mainHand, inventorySlugs])

  const offHandEquipOptions = useMemo(() => {
    const fromInventory = inventorySlugs.filter((slug) => {
      const slot = equipmentMap.get(slug)?.equipSlot
      const isTwoHanded = Boolean(equipmentMap.get(slug)?.isTwoHanded)
      return (slot === 'hand' || slot === 'off-hand') && !isTwoHanded
    })
    const equipped = equippedSlots.offHand ? [equippedSlots.offHand] : []
    return uniqueValues([...equipped, ...fromInventory])
  }, [equipmentMap, equippedSlots.offHand, inventorySlugs])

  if (!currentStep) {
    return <Navigate to={isEditMode && id ? `/characters/${id}/edit/identity` : '/characters/new/identity'} replace />
  }

  if (!isAuthLoading && !user) {
    return <Navigate to="/login" replace state={{ from: isEditMode && id ? `/characters/${id}/edit/identity` : '/characters/new/identity' }} />
  }

  if (isLoading) {
    return <section className="surface-card loading-state">Загрузка конструктора...</section>
  }

  if (!options || !selectedRace || !selectedClass || !selectedBackground) {
    return <section className="surface-card error-state">{error ?? 'Конструктор недоступен.'}</section>
  }

  return (
    <div className="stack">
      <CharacterBuilderHeader isEditMode={isEditMode} characterId={id} currentStep={currentStep} />

      {allBlockingMessages.length > 0 ? (
        <article className="surface-card blocking-errors-top">
          {allBlockingMessages.map((message, index) => (
            <p key={`${message}-${index}`} className="inline-error inline-error--prominent">{message}</p>
          ))}
        </article>
      ) : null}

      <section className="builder-layout compact">
        <div className="builder-main">
          {currentStep === 'identity' ? (
            <IdentityStepPanel
              name={draft.name}
              errors={getStepErrors('identity')}
              onNameChange={(value) => updateDraft('name', value)}
            />
          ) : null}

          {currentStep === 'race' ? (
            <RaceStepPanel
              raceGroups={groupedRaces}
              selectedRaceId={draft.raceId}
              bonusAbilitySelections={draft.bonusAbilitySelections}
              raceSkillSelections={draft.raceSkillSelections}
              onSelectRace={(raceId) => updateDraft('raceId', raceId)}
              onOpenRaceInfo={openRaceInfo}
              onToggleBonusSelection={toggleBonusSelection}
              onToggleRaceSkill={toggleRaceSkill}
            />
          ) : null}

          {currentStep === 'class' ? (
            <ClassStepPanel
              classes={options.classes}
              selectedClass={selectedClass}
              selectedClassId={draft.classId}
              level={draft.level}
              armorDisplayItems={armorDisplayItems}
              weaponDisplayItems={weaponDisplayItems}
              classSkillSelections={draft.classSkillSelections}
              raceSkillSelections={draft.raceSkillSelections}
              savingThrows={computedSavingThrows}
              groupedClassFeatures={groupedClassFeatures}
              expandedClassFeatureLevel={expandedClassFeatureLevel}
              availableClassFeatures={availableClassFeatures}
              onLevelChange={(level) => updateDraft('level', level)}
              onSelectClass={(classId) => updateDraft('classId', classId)}
              onOpenClassInfo={openClassInfo}
              onOpenProficiencyItems={(title, items) => setProficiencyItemsModal({ title, items })}
              onToggleClassSkill={toggleClassSkill}
              onToggleFeatureLevel={(featureLevel) =>
                setExpandedClassFeatureLevel((current) =>
                  current === `class-level-${featureLevel}` ? null : `class-level-${featureLevel}`,
                )
              }
              onOpenFeatureInfo={(title, description) => setFeatureInfoModal({ title, description })}
            />
          ) : null}

          {currentStep === 'background' ? (
            <BackgroundStepPanel
              backgrounds={options.backgrounds}
              selectedBackgroundId={draft.backgroundId}
              errors={getStepErrors('background')}
              onSelectBackground={(backgroundId) => updateDraft('backgroundId', backgroundId)}
              onOpenBackgroundInfo={openBackgroundInfo}
            />
          ) : null}

          {currentStep === 'abilities' ? (
            <AbilityStepPanel
              abilities={computedAbilities}
              minScore={abilityScoreMin}
              maxScore={abilityScoreMax}
              errors={getStepErrors('abilities')}
              onRandomize={randomizeBaseAbilities}
              onUpdateBaseAbility={updateBaseAbility}
            />
          ) : null}

          

          {currentStep === 'spells' ? (
            <SpellsStepPanel
              availableSpells={availableSpells}
              selectedSpells={selectedSpells}
              spellCatalog={spellCatalog}
              spellSearch={spellSearch}
              isSpellPickerOpen={isSpellPickerOpen}
              recentlyAddedSpell={recentlyAddedSpell}
              onTogglePicker={() => setIsSpellPickerOpen((current) => !current)}
              onSearchChange={setSpellSearch}
              onAddSpell={addSpell}
              onRemoveSpell={removeSpell}
              onOpenSpellInfo={setSpellInfoModal}
            />
          ) : null}

          {currentStep === 'inventory' ? (
            <InventoryStepPanel
              filteredEquipmentCatalog={filteredEquipmentCatalog}
              equipmentMap={equipmentMap}
              inventoryWithIndexes={inventoryWithIndexes}
              inventorySearch={inventorySearch}
              isInventoryPickerOpen={isInventoryPickerOpen}
              recentlyAddedInventorySlug={recentlyAddedInventorySlug}
              recentlyAddedInventoryKey={recentlyAddedInventoryKey}
              equippedSlots={equippedSlots}
              bodyEquipOptions={bodyEquipOptions}
              mainHandEquipOptions={mainHandEquipOptions}
              offHandEquipOptions={offHandEquipOptions}
              isMainHandTwoHanded={isMainHandTwoHanded}
              onTogglePicker={() => setIsInventoryPickerOpen((current) => !current)}
              onSearchChange={setInventorySearch}
              onAddInventoryItem={addInventoryItem}
              onRemoveInventoryItem={removeInventoryItem}
              onEquipItem={equipItem}
            />
          ) : null}

          {currentStep === 'review' ? (
            <ReviewStepPanel
              name={draft.name}
              level={draft.level}
              selectedRace={selectedRace}
              selectedClass={selectedClass}
              selectedBackground={selectedBackground}
              estimatedArmorClass={estimatedArmorClass}
              estimatedHitPoints={estimatedHitPoints}
              passivePerception={passivePerception}
              abilities={computedAbilities}
              skills={allSkillsPreview}
              error={error}
            />
          ) : null}

          <div className="wizard-actions">
            <button
              type="button"
              className="secondary-button button-reset"
              onClick={() => goToStep(-1)}
              disabled={currentStep === 'identity'}
            >
              Назад
            </button>
            {currentStep !== 'review' ? (
              <>
                <button type="button" className="primary-button button-reset" onClick={() => goToStep(1)}>
                  Далее
                </button>
                {!hasBlockingErrors ? (
                  <button
                    type="button"
                    className="secondary-button button-reset"
                    onClick={() => void handleSave()}
                    disabled={isSaving}
                  >
                    {isSaving ? 'Сохранение...' : isEditMode ? 'Сохранить изменения' : 'Создать персонажа'}
                  </button>
                ) : null}
              </>
            ) : (
              <button
                type="button"
                className="primary-button button-reset"
                onClick={() => void handleSave()}
                disabled={isSaving}
              >
                {isSaving ? 'Сохранение...' : isEditMode ? 'Сохранить изменения' : 'Создать персонажа'}
              </button>
            )}
          </div>
        </div>

        
      </section>

      <CharacterOptionInfoModal
        modal={infoModal}
        activeTab={modalTab}
        expandedFeature={expandedModalFeature}
        selectedFeatureLevel={selectedModalFeatureLevel}
        onClose={() => setInfoModal(null)}
        onTabChange={setModalTab}
        onExpandedFeatureChange={setExpandedModalFeature}
        onSelectedFeatureLevelChange={setSelectedModalFeatureLevel}
      />
      <ProficiencyItemsModal modal={proficiencyItemsModal} onClose={() => setProficiencyItemsModal(null)} />
      <SpellInfoModal modal={spellInfoModal} onClose={() => setSpellInfoModal(null)} />
      <FeatureInfoModal modal={featureInfoModal} onClose={() => setFeatureInfoModal(null)} />
    </div>
  )
}
