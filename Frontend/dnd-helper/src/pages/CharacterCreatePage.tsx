import { Fragment, useEffect, useMemo, useState } from 'react'
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
  buildInfoIconLabel,
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
  translateClassSlug,
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
            <article className="surface-card builder-section">
              <h3>Базовая информация</h3>
              <div className="form-grid compact">
                <label className="full-span">
                  Имя персонажа
                  <input value={draft.name} onChange={(event) => updateDraft('name', event.target.value)} />
                </label>
              </div>
              {getStepErrors('identity').length > 0 ? <p className="inline-error">{getStepErrors('identity')[0]}</p> : null}
            </article>
          ) : null}

          {currentStep === 'race' ? (
            <article className="surface-card builder-section">
              <h3>Раса</h3>
              <div className="race-group-stack">
                {groupedRaces.map(([group, races]) => (
                  <section key={group} className="race-group">
                    <div className="race-group__title">
                      <h4>{group}</h4>
                    </div>
                    <div className="choice-grid compact">
                      {races.map((race) => (
                        <Fragment key={race.id}>
                          <article
                            className={`choice-card slim choice-card--static ${race.id === draft.raceId ? 'selected' : ''}`}
                          >
                            <button
                              type="button"
                              className="info-icon-button"
                              aria-label={buildInfoIconLabel(race.name)}
                              onClick={() => openRaceInfo(race)}
                            >
                              i
                            </button>
                            <button type="button" className="choice-card__main" onClick={() => updateDraft('raceId', race.id)}>
                              <strong>{race.name}</strong>
                              {race.summary && race.summary !== race.name ? <small>{race.summary}</small> : null}
                              <small className="multiline-text">
                                Бонусы:{'\n'}
                                {(race.bonuses.map((bonus) => `${translateAbility(bonus.ability)} +${bonus.value}`).join('\n')) || 'нет'}
                              </small>
                              {race.skillChoiceRule ? (
                                <small>Выбор навыков: {race.skillChoiceRule.count}</small>
                              ) : null}
                            </button>
                          </article>
                          {race.id === draft.raceId && race.bonusChoiceRule ? (
                            <div className="bonus-choice-panel full-width race-skill-selection-panel">
                              <p>{race.bonusChoiceRule.summary} (выбери: {race.bonusChoiceRule.count})</p>
                              <div className="bonus-choice-grid">
                                {race.bonusChoiceRule.allowedAbilities.map((ability) => (
                                  <button
                                    key={ability}
                                    type="button"
                                    className={`bonus-chip ${draft.bonusAbilitySelections.includes(ability) ? 'selected' : ''}`}
                                    onClick={() => toggleBonusSelection(ability)}
                                  >
                                    {translateAbility(ability)}
                                  </button>
                                ))}
                              </div>
                            </div>
                          ) : null}
                          {race.id === draft.raceId && race.skillChoiceRule ? (
                            <div className="bonus-choice-panel full-width race-skill-selection-panel">
                              <p>{race.skillChoiceRule.summary} (выбери: {race.skillChoiceRule.count})</p>
                              <div className="skill-pick-grid">
                                {race.skillChoiceRule.availableSkills.map((skill) => (
                                  <button
                                    key={skill}
                                    type="button"
                                    className={`skill-toggle ${draft.raceSkillSelections.includes(skill) ? 'selected' : ''}`}
                                    onClick={() => toggleRaceSkill(skill)}
                                  >
                                    {translateSkill(skill)}
                                  </button>
                                ))}
                              </div>
                            </div>
                          ) : null}
                        </Fragment>
                      ))}
                    </div>
                  </section>
                ))}
              </div>
            </article>
          ) : null}

          {currentStep === 'class' ? (
            <article className="surface-card builder-section">
              <h3>Класс</h3>
              <div className="form-grid compact">
                <label className="full-span">
                  Уровень персонажа
                  <input
                    type="number"
                    min={1}
                    max={20}
                    value={draft.level}
                    onChange={(event) => updateDraft('level', Number(event.target.value))}
                  />
                </label>
              </div>
              <div className="choice-grid compact">
                {options.classes.map((item) => (
                  <article
                    key={item.id}
                    className={`choice-card slim choice-card--static ${item.id === draft.classId ? 'selected' : ''}`}
                  >
                    <button
                      type="button"
                      className="info-icon-button"
                      aria-label={buildInfoIconLabel(item.name)}
                      onClick={() => openClassInfo(item)}
                    >
                      i
                    </button>
                    <button type="button" className="choice-card__main" onClick={() => updateDraft('classId', item.id)}>
                      <strong>{item.name}</strong>
                      <small>{item.summary}</small>
                      <small>Кость хитов d{item.hitDie}</small>
                      <small>Спасброски: {item.savingThrowProficiencies.map((ability) => translateAbility(ability)).join(', ')}</small>
                    </button>
                  </article>
                ))}
              </div>
              <div className="builder-subsection">
                <h4>Владения класса</h4>
                <div className="stack">
                  <article className="surface-card modal-detail">
                    <h4>Доспехи</h4>
                    {armorDisplayItems.length > 0 ? (
                      <div className="skill-pick-grid">
                        {armorDisplayItems.map((entry, index) => (
                          entry.interactive ? (
                            <button
                              key={`armor-${entry.label}-${index}`}
                              type="button"
                              className="bonus-chip"
                              onClick={() => setProficiencyItemsModal({ title: entry.label, items: entry.items })}
                            >
                              {entry.label}
                            </button>
                          ) : <span key={`armor-${entry.label}-${index}`} className="bonus-chip static">{entry.label}</span>
                        ))}
                      </div>
                    ) : <span className="muted">Нет владений доспехами.</span>}
                  </article>
                  <article className="surface-card modal-detail">
                    <h4>Оружие</h4>
                    {weaponDisplayItems.length > 0 ? (
                      <div className="skill-pick-grid">
                        {weaponDisplayItems.map((entry, index) => (
                          entry.interactive ? (
                            <button
                              key={`weapon-${entry.label}-${index}`}
                              type="button"
                              className="bonus-chip"
                              onClick={() => setProficiencyItemsModal({ title: entry.label, items: entry.items })}
                            >
                              {entry.label}
                            </button>
                          ) : <span key={`weapon-${entry.label}-${index}`} className="bonus-chip static">{entry.label}</span>
                        ))}
                      </div>
                    ) : <span className="muted">Нет владений оружием.</span>}
                  </article>
                </div>
              </div>
              <div className="builder-subsection">
                <h4>Навыки класса</h4>
                <p className="muted">
                  {selectedClass.skillChoiceRule.summary} (выбери: {selectedClass.skillChoiceRule.count})
                </p>
                <div className="skill-pick-grid">
                  {selectedClass.skillChoiceRule.availableSkills.map((skill) => {
                    const isBlocked = draft.raceSkillSelections.includes(skill)
                    return (
                      <button
                        key={skill}
                        type="button"
                        className={`skill-toggle ${draft.classSkillSelections.includes(skill) ? 'selected' : ''}`}
                        onClick={() => toggleClassSkill(skill)}
                        disabled={isBlocked}
                      >
                        {translateSkill(skill)}
                      </button>
                    )
                  })}
                </div>
              </div>
              <div className="builder-subsection">
                <h4>Владения спасбросками</h4>
                <div className="skill-pick-grid">
                  {computedSavingThrows.filter((item) => item.isProficient).map((savingThrow) => (
                    <span key={savingThrow.ability} className="bonus-chip static">
                      {translateAbility(savingThrow.ability)}
                    </span>
                  ))}
                </div>
              </div>
              <div className="builder-subsection">
                <h4>Классовые особенности по уровню</h4>
                <div className="stack">
                  {groupedClassFeatures.length > 0 ? groupedClassFeatures.map(([level, features]) => (
                    <article key={`level-${level}`} className="surface-card modal-detail class-feature-level-card">
                      <button
                        type="button"
                        className="disclosure-button"
                        onClick={() =>
                          setExpandedClassFeatureLevel((current) =>
                            current === `class-level-${level}` ? null : `class-level-${level}`,
                          )
                        }
                      >
                        <span>{level === 0 ? 'Дополнительно' : `${level} уровень`}</span>
                        <strong>{expandedClassFeatureLevel === `class-level-${level}` ? 'Скрыть' : 'Показать'}</strong>
                      </button>
                      {expandedClassFeatureLevel === `class-level-${level}` ? (
                        <div className="skill-pick-grid">
                          {features.map((feature) => (
                            <button
                              key={`${level}-${feature}`}
                              type="button"
                              className="bonus-chip"
                              onClick={() => {
                                const full = availableClassFeatures.find((item) =>
                                  item.title.toLowerCase().includes(feature.toLowerCase()),
                                )
                                setFeatureInfoModal({
                                  title: feature,
                                  description: full?.description ?? 'Описание будет уточнено.',
                                })
                              }}
                            >
                              {feature}
                            </button>
                          ))}
                        </div>
                      ) : null}
                    </article>
                  )) : <span className="muted">Для этого уровня пока нет отображаемых особенностей.</span>}
                </div>
              </div>
            </article>
          ) : null}

          {currentStep === 'background' ? (
            <article className="surface-card builder-section">
              <h3>Предыстория</h3>
              <div className="choice-grid compact">
                {options.backgrounds.map((item) => (
                  <article
                    key={item.id}
                    className={`choice-card slim choice-card--static ${item.id === draft.backgroundId ? 'selected' : ''}`}
                  >
                    <button
                      type="button"
                      className="info-icon-button"
                      aria-label={buildInfoIconLabel(item.name)}
                      onClick={() => openBackgroundInfo(item)}
                    >
                      i
                    </button>
                    <button
                      type="button"
                      className="choice-card__main"
                      onClick={() => updateDraft('backgroundId', item.id)}
                    >
                      <strong>{item.name}</strong>
                      <small>{item.summary}</small>
                      <small>{item.description ?? item.summary}</small>
                      <small>Навыки: {item.grantedSkillProficiencies.map((skill) => translateSkill(skill)).join(', ')}</small>
                    </button>
                  </article>
                ))}
              </div>
              {getStepErrors('background').length > 0 ? <p className="inline-error">{getStepErrors('background')[0]}</p> : null}
            </article>
          ) : null}

          {currentStep === 'abilities' ? (
            <article className="surface-card builder-section">
              <h3>Характеристики</h3>
              <p className="muted">Каждую базовую характеристику можно задать вручную в пределах от 3 до 18. Расовые бонусы применяются автоматически и сразу видны в расчёте.</p>
              <button type="button" className="secondary-button button-reset ability-random-button" onClick={randomizeBaseAbilities}>
                Случайно распределить характеристики
              </button>
              <div className="ability-builder-grid compact">
                {computedAbilities.map((ability) => (
                  <article className="ability-builder-card compact" key={ability.key}>
                    <div>
                      <p className="ability-key">{translateAbility(ability.key)}</p>
                      <h4>{translateAbility(ability.key)}</h4>
                    </div>
                    <label>
                      Базовое значение
                      <div className="ability-stepper">
                        <button
                          type="button"
                          className="stepper-button"
                          onClick={() => updateBaseAbility(ability.key, ability.baseScore - 1)}
                        >
                          -
                        </button>
                        <input
                          type="number"
                          min={abilityScoreMin}
                          max={abilityScoreMax}
                          value={ability.baseScore}
                          onChange={(event) => updateBaseAbility(ability.key, Number(event.target.value))}
                        />
                        <button
                          type="button"
                          className="stepper-button"
                          onClick={() => updateBaseAbility(ability.key, ability.baseScore + 1)}
                        >
                          +
                        </button>
                      </div>
                    </label>
                    <div className="ability-breakdown compact">
                      <div>
                        <span>Расовый бонус</span>
                        <strong>{ability.bonus >= 0 ? `+${ability.bonus}` : ability.bonus}</strong>
                      </div>
                      <div>
                        <span>Итог</span>
                        <strong>{ability.total}</strong>
                      </div>
                      <div>
                        <span>Модификатор</span>
                        <strong>{ability.modifier >= 0 ? `+${ability.modifier}` : ability.modifier}</strong>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
              {getStepErrors('abilities').length > 0 ? <p className="inline-error">{getStepErrors('abilities')[0]}</p> : null}
            </article>
          ) : null}

          

          {currentStep === 'spells' ? (
            <article className="surface-card builder-section">
              <h3>Заклинания</h3>
              <label className="full-span">
                Заклинания
                <div className="skill-pick-grid">
                  <button type="button" className="secondary-button button-reset" onClick={() => setIsSpellPickerOpen((current) => !current)}>
                    + Добавить заклинание
                  </button>
                </div>
                {isSpellPickerOpen ? (
                  <div className="surface-card">
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
                        <article
                          key={spell.slug}
                          className={`choice-card slim choice-card--static ${selectedSpells.includes(spell.slug) ? 'selected' : ''}`}
                        >
                          <button
                            type="button"
                            className="info-icon-button"
                            aria-label={buildInfoIconLabel(spell.name)}
                            onClick={() =>
                              setSpellInfoModal({
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
                            {spell.summary ? <small>{spell.summary}</small> : null}
                          </button>
                        </article>
                      ))}
                    </div>
                  </div>
                ) : null}
                <div className="skill-pick-grid">
                  {selectedSpells.length > 0 ? selectedSpells.map((spell) => (
                    <span key={spell} className="bonus-chip static">
                      {spellCatalog.find((item) => item.slug === spell)?.name ?? spell}
                      <button
                        type="button"
                        className="button-reset icon-remove-button"
                        aria-label="Удалить заклинание"
                        onClick={() => removeSpell(spell)}
                      >
                        ×
                      </button>
                    </span>
                  )) : <span className="muted">Заклинания пока не выбраны.</span>}
                </div>
              </label>
            </article>
          ) : null}

          {currentStep === 'inventory' ? (
            <article className="surface-card builder-section">
              <h3>Инвентарь</h3>
              <div className="form-grid compact">
                <label className="full-span">
                  Предметы
                  <div className="skill-pick-grid">
                    <button type="button" className="secondary-button button-reset" onClick={() => setIsInventoryPickerOpen((current) => !current)}>
                      + Добавить предмет
                    </button>
                  </div>
                  {isInventoryPickerOpen ? (
                    <div className="surface-card">
                      <label className="full-span">
                        Поиск предмета
                        <input
                          className="app-search-input"
                          value={inventorySearch}
                          onChange={(event) => setInventorySearch(event.target.value)}
                          placeholder="Лук, щит, фонарь..."
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
                  ) : null}
                  <div className="skill-pick-grid">
                    {inventoryWithIndexes.length === 0 ? <span className="muted">Предметы пока не добавлены.</span> : inventoryWithIndexes.map(({ slug, index, key }) => (
                      <span key={key} className={`bonus-chip static ${recentlyAddedInventoryKey === key ? 'selection-flash' : ''}`}>
                        {equipmentMap.get(slug)?.name ?? slug}
                        <button
                          type="button"
                          className="button-reset icon-remove-button"
                          aria-label="Удалить предмет"
                          onClick={() => removeInventoryItem(index)}
                        >
                          ×
                        </button>
                      </span>
                    ))}
                  </div>
                </label>

                <label className="full-span">
                  Экипировка
                  <div className="form-grid compact">
                    <label>
                      Тело
                      <select className="app-select" value={equippedSlots.body ?? ''} onChange={(event) => equipItem('body', event.target.value)}>
                        <option value="">Не экипировано</option>
                        {bodyEquipOptions.map((slug, index) => <option key={`${slug}-body-${index}`} value={slug}>{equipmentMap.get(slug)?.name}</option>)}
                      </select>
                    </label>
                    <label>
                      Правая рука
                      <select className="app-select" value={equippedSlots.mainHand ?? ''} onChange={(event) => equipItem('mainHand', event.target.value)}>
                        <option value="">Не экипировано</option>
                        {mainHandEquipOptions.map((slug, index) => <option key={`${slug}-main-${index}`} value={slug}>{equipmentMap.get(slug)?.name}</option>)}
                      </select>
                    </label>
                    <label>
                      Левая рука
                      <select
                        className="app-select"
                        value={equippedSlots.offHand ?? ''}
                        onChange={(event) => equipItem('offHand', event.target.value)}
                        disabled={isMainHandTwoHanded}
                      >
                        <option value="">Не экипировано</option>
                        {offHandEquipOptions.map((slug, index) => <option key={`${slug}-off-${index}`} value={slug}>{equipmentMap.get(slug)?.name}</option>)}
                      </select>
                      {isMainHandTwoHanded ? <small className="muted">Левая рука заблокирована двуручным оружием.</small> : null}
                    </label>
                  </div>
                </label>
              </div>
            </article>
          ) : null}

          {currentStep === 'review' ? (
            <article className="surface-card builder-section">
              <h3>Проверка и завершение</h3>
              <p className="muted">Итоговый лист персонажа с применёнными бонусами и выбранными владениями.</p>
              <div className="review-sheet review-sheet--vertical">
                <div className="review-head">
                  <div className="review-field"><span>Имя</span><strong>{draft.name || '—'}</strong></div>
                  <div className="review-field"><span>Предыстория</span><strong>{selectedBackground.name}</strong></div>
                  <div className="review-field"><span>Раса</span><strong>{selectedRace.name}</strong></div>
                  <div className="review-field"><span>Класс</span><strong>{selectedClass.name}</strong></div>
                  <div className="review-field"><span>Уровень</span><strong>{draft.level}</strong></div>
                </div>
                <div className="status-grid compact">
                  <div className="status-card"><span>КД</span><strong>{estimatedArmorClass}</strong></div>
                  <div className="status-card"><span>Хиты</span><strong>{estimatedHitPoints}</strong></div>
                  <div className="status-card"><span>Скорость</span><strong>{selectedRace.speed}</strong></div>
                  <div className="status-card"><span>Пассивная внимательность</span><strong>{passivePerception}</strong></div>
                </div>
                <div className="review-abilities">
                  {computedAbilities.map((ability) => (
                    <div key={ability.key} className="review-ability">
                      <span>{translateAbility(ability.key)}</span>
                      <strong>{ability.total}</strong>
                      <small>{ability.modifier >= 0 ? `+${ability.modifier}` : ability.modifier}</small>
                    </div>
                  ))}
                </div>
              </div>
              <div className="builder-subsection">
                <h4>Все навыки</h4>
                <div className="skill-pick-grid">
                  {allSkillsPreview.map((skill) => (
                    <span key={skill.skillId} className={`bonus-chip ${skill.proficient ? 'selected' : 'static'}`}>
                      {skill.label} {skill.level >= 0 ? `+${skill.level}` : skill.level}
                    </span>
                  ))}
                </div>
              </div>
              {error ? <p className="inline-error">{error}</p> : null}
            </article>
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

      {infoModal ? (
        <div className="modal-overlay" role="presentation" onClick={() => setInfoModal(null)}>
          <div className="modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3>{infoModal.title}</h3>
                <p className="section-text">{infoModal.subtitle}</p>
              </div>
              <button type="button" className="secondary-button button-reset" onClick={() => setInfoModal(null)}>
                Закрыть
              </button>
            </div>

            <div className="wizard-steps" aria-label="Разделы описания">
              <button type="button" className={`wizard-step button-reset ${modalTab === 'overview' ? 'active' : ''}`} onClick={() => setModalTab('overview')}>
                Обзор
              </button>
              <button type="button" className={`wizard-step button-reset ${modalTab === 'features' ? 'active' : ''}`} onClick={() => setModalTab('features')}>
                Особенности
              </button>
              <button type="button" className={`wizard-step button-reset ${modalTab === 'proficiencies' ? 'active' : ''}`} onClick={() => setModalTab('proficiencies')}>
                Владения
              </button>
            </div>

            {modalTab === 'overview' ? (
              <div className="modal-facts">
                {infoModal.overview.map((fact) => (
                  <div key={fact.label} className="status-card">
                    <span>{fact.label}</span>
                    <strong>{fact.value}</strong>
                  </div>
                ))}
              </div>
            ) : null}

            {modalTab === 'features' ? (
              (() => {
                const classFeatureGroups = infoModal.features.reduce((acc, detail) => {
                  const match = detail.title.match(/(\d+)\s*уровень\s*:\s*(.+)/i)
                  if (!match) {
                    return acc
                  }
                  const level = Number(match[1])
                  const featureName = match[2].trim()
                  const list = acc.get(level) ?? []
                  if (!list.some((item) => item.title === featureName)) {
                    list.push({ title: featureName, description: detail.description })
                  }
                  acc.set(level, list)
                  return acc
                }, new Map<number, Array<{ title: string; description: string }>>())

                if (classFeatureGroups.size > 0) {
                  const levels = Array.from(classFeatureGroups.keys()).sort((a, b) => a - b)
                  const activeLevel = selectedModalFeatureLevel ?? levels[0]
                  const features = classFeatureGroups.get(activeLevel) ?? []
                  return (
                    <div className="stack">
                      <div className="skill-pick-grid">
                        {levels.map((level) => (
                          <button
                            key={level}
                            type="button"
                            className={`bonus-chip ${activeLevel === level ? 'selected' : ''}`}
                            onClick={() => setSelectedModalFeatureLevel(level)}
                          >
                            {level} уровень
                          </button>
                        ))}
                      </div>
                      <div className="stack">
                        {features.map((detail) => (
                          <div key={`${activeLevel}-${detail.title}`} className="surface-card modal-detail">
                            <button
                              type="button"
                              className="disclosure-button"
                              onClick={() =>
                                setExpandedModalFeature((current) => (current === `${activeLevel}-${detail.title}` ? null : `${activeLevel}-${detail.title}`))
                              }
                            >
                              <span>{detail.title}</span>
                              <strong>i</strong>
                            </button>
                            {expandedModalFeature === `${activeLevel}-${detail.title}` ? <p>{detail.description}</p> : null}
                          </div>
                        ))}
                      </div>
                    </div>
                  )
                }

                return (
                  <div className="stack">
                    {infoModal.features.map((detail) => (
                      <div key={detail.title} className="surface-card modal-detail">
                        <button
                          type="button"
                          className="disclosure-button"
                          onClick={() =>
                            setExpandedModalFeature((current) => (current === detail.title ? null : detail.title))
                          }
                        >
                          <span>{detail.title}</span>
                          <strong>i</strong>
                        </button>
                        {expandedModalFeature === detail.title ? <p>{detail.description}</p> : null}
                      </div>
                    ))}
                  </div>
                )
              })()
            ) : null}

            {modalTab === 'proficiencies' ? (
              <div className="stack">
                {infoModal.proficiencies.length > 0 ? infoModal.proficiencies.map((entry) => (
                  <div key={entry.category} className="surface-card modal-detail">
                    <h4>{entry.category}</h4>
                    <div className="skill-pick-grid">
                      {entry.items.length > 0 ? entry.items.map((item) => (
                        <span key={item} className="bonus-chip static">{item}</span>
                      )) : <span className="muted">Нет обязательных навыков для этого класса.</span>}
                    </div>
                  </div>
                )) : (
                  <p className="muted">Нет отдельных владений в этом разделе.</p>
                )}
              </div>
            ) : null}
          </div>
        </div>
      ) : null}

      {proficiencyItemsModal ? (
        <div className="modal-overlay" role="presentation" onClick={() => setProficiencyItemsModal(null)}>
          <div className="modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3>{proficiencyItemsModal.title}</h3>
                <p className="section-text">Список предметов по выбранному типу владения</p>
              </div>
              <button type="button" className="secondary-button button-reset" onClick={() => setProficiencyItemsModal(null)}>
                Закрыть
              </button>
            </div>
            <div className="skill-pick-grid">
              {proficiencyItemsModal.items.length > 0
                ? proficiencyItemsModal.items.map((item) => <span key={item} className="bonus-chip static">{item}</span>)
                : <p className="muted">Список предметов пока не заполнен.</p>}
            </div>
          </div>
        </div>
      ) : null}

      {spellInfoModal ? (
        <div className="modal-overlay" role="presentation" onClick={() => setSpellInfoModal(null)}>
          <div className="modal-card spell-modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3>{spellInfoModal.name}</h3>
                <p className="section-text">{spellInfoModal.summary ?? 'Описание из Книги игрока'}</p>
              </div>
              <button type="button" className="secondary-button button-reset" onClick={() => setSpellInfoModal(null)}>
                Закрыть
              </button>
            </div>
            <div className="spell-meta-grid">
              <div className="status-card"><span>Круг</span><strong>{spellInfoModal.circle}</strong></div>
              <div className="status-card"><span>Мин. уровень</span><strong>{spellInfoModal.minLevel}</strong></div>
              <div className="status-card"><span>Классы</span><strong>{spellInfoModal.classes.join(', ') || '-'}</strong></div>
            </div>
            <article className="surface-card spell-description-block">
              <h4>Эффект заклинания</h4>
              <p>{spellInfoModal.description ?? 'Подробное описание пока не заполнено в базе правил.'}</p>
            </article>
          </div>
        </div>
      ) : null}

      {featureInfoModal ? (
        <div className="modal-overlay" role="presentation" onClick={() => setFeatureInfoModal(null)}>
          <div className="modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3>{featureInfoModal.title}</h3>
                <p className="section-text">{featureInfoModal.description}</p>
              </div>
              <button type="button" className="secondary-button button-reset" onClick={() => setFeatureInfoModal(null)}>
                Закрыть
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  )
}
