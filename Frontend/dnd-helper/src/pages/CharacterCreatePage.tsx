import { useEffect, useMemo, useState } from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../components/AuthProvider'
import {
  createCharacter,
  getCharacterById,
  getCharacterOptions,
  updateCharacter,
} from '../services/charactersApi'
import type {
  ApiValidationError,
  BackgroundOption,
  BaseAbilityScore,
  Character,
  CharacterOptions,
  CharacterPayload,
  ClassOption,
  FeatureDetail,
  RaceOption,
} from '../types/character'
import {
  formatSkillLevel,
  getCharacterPortrait,
  translateAbility,
  translateSkill,
} from '../utils/characterPresentation'

const abilityOrder = ['STR', 'DEX', 'CON', 'INT', 'WIS', 'CHA'] as const
const draftStorageKey = 'dnd-helper.character-draft'
const steps = ['identity', 'race', 'class', 'background', 'abilities', 'skills', 'review'] as const

type StepKey = (typeof steps)[number]

type CharacterDraft = {
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

type DraftValidationErrors = Partial<Record<StepKey | 'global', string[]>>

type InfoModalState = {
  title: string
  subtitle: string
  details: FeatureDetail[]
  facts: Array<{ label: string; value: string }>
}

function createDefaultDraft(): CharacterDraft {
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

function loadDraft(isEditMode: boolean): CharacterDraft {
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

function getModifier(score: number) {
  return Math.floor((score - 10) / 2)
}

function getProficiencyBonus(level: number) {
  return 2 + Math.floor((level - 1) / 4)
}

function getHitPoints(hitDie: number, level: number, constitutionModifier: number) {
  const firstLevel = hitDie + constitutionModifier
  if (level === 1) {
    return firstLevel
  }

  const averagePerLevel = Math.floor(hitDie / 2) + 1 + constitutionModifier
  return firstLevel + (level - 1) * averagePerLevel
}

function splitMultiline(value: string) {
  return value
    .split('\n')
    .map((item) => item.trim())
    .filter(Boolean)
}

function uniqueValues(values: string[]) {
  return Array.from(new Set(values))
}

function getStepTitle(step: StepKey) {
  const titles: Record<StepKey, string> = {
    identity: 'Основа',
    race: 'Раса',
    class: 'Класс',
    background: 'Предыстория',
    abilities: 'Характеристики',
    skills: 'Владения',
    review: 'Проверка',
  }

  return titles[step]
}

function mapCharacterToDraft(character: Character, options: CharacterOptions): CharacterDraft {
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
    raceSkillSelections: character.raceSkillSelections,
    classSkillSelections: character.classSkillSelections,
    spellsText: character.spells.join('\n'),
    inventoryText: character.inventory.join('\n'),
  }
}

function buildOptionFacts(
  option: RaceOption | ClassOption | BackgroundOption,
  kind: 'race' | 'class' | 'background',
) {
  if (kind === 'race') {
    const race = option as RaceOption
    return [
      {
        label: 'Бонусы характеристик',
        value:
          race.bonuses.length > 0
            ? race.bonuses.map((bonus) => `${translateAbility(bonus.ability)} +${bonus.value}`).join(', ')
            : 'Нет фиксированных бонусов',
      },
      { label: 'Скорость', value: `${race.speed} футов` },
      {
        label: 'Автоматические навыки',
        value:
          race.grantedSkillProficiencies.length > 0
            ? race.grantedSkillProficiencies.map((skill) => translateSkill(skill)).join(', ')
            : 'Нет',
      },
      {
        label: 'Выбор навыков',
        value: race.skillChoiceRule
          ? `${race.skillChoiceRule.count}: ${race.skillChoiceRule.availableSkills
              .map((skill) => translateSkill(skill))
              .join(', ')}`
          : 'Нет',
      },
    ]
  }

  if (kind === 'background') {
    const background = option as BackgroundOption
    return [
      {
        label: 'Навыки предыстории',
        value: background.grantedSkillProficiencies.map((skill) => translateSkill(skill)).join(', '),
      },
      {
        label: 'Описание',
        value: background.summary,
      },
    ]
  }

  const characterClass = option as ClassOption
  return [
    { label: 'Кость хитов', value: `d${characterClass.hitDie}` },
    {
      label: 'Основные характеристики',
      value: characterClass.primaryAbilities.map((ability) => translateAbility(ability)).join(', '),
    },
    {
      label: 'Спасброски',
      value: characterClass.savingThrowProficiencies
        .map((ability) => translateAbility(ability))
        .join(', '),
    },
    {
      label: 'Навыки класса',
      value: `${characterClass.skillChoiceRule.count}: ${characterClass.skillChoiceRule.availableSkills
        .map((skill) => translateSkill(skill))
        .join(', ')}`,
    },
  ]
}

function buildInfoIconLabel(title: string) {
  return `Подробнее о ${title}`
}

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

  const currentStep = steps.includes(step as StepKey) ? (step as StepKey) : null

  useEffect(() => {
    if (!user) {
      setIsLoading(false)
      return
    }

    let isCancelled = false

    async function loadInitialData() {
      try {
        const response = await getCharacterOptions()
        if (isCancelled) {
          return
        }

        setOptions(response)

        if (isEditMode && id) {
          const character = await getCharacterById(id)
          if (isCancelled) {
            return
          }

          setDraft(mapCharacterToDraft(character, response))
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
        .filter((skill) => !fixedSkillProficiencies.includes(skill))
        .filter((skill) => !nextRaceSelections.includes(skill))
        .slice(0, selectedClass.skillChoiceRule.count)

      return {
        ...current,
        bonusAbilitySelections: nextBonusSelections,
        raceSkillSelections: nextRaceSelections,
        classSkillSelections: nextClassSelections,
      }
    })
  }, [fixedSkillProficiencies, selectedClass, selectedRace])

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

  const computedSkills = useMemo(
    () =>
      combinedSkillProficiencies
        .map((skillId) => {
          const abilityKey = {
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
          }[skillId]
          const modifier = computedAbilities.find((item) => item.key === abilityKey)?.modifier ?? 0
          return {
            skillId,
            level: modifier + proficiencyBonus,
          }
        })
        .sort((left, right) => translateSkill(left.skillId).localeCompare(translateSkill(right.skillId), 'ru')),
    [combinedSkillProficiencies, computedAbilities, proficiencyBonus],
  )

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
        result.race = [`Для расы ${selectedRace.name} нужно выбрать ${selectedRace.bonusChoiceRule.count} допустимые характеристики.`]
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
      draft.baseAbilities.some((ability) => ability.score < 8 || ability.score > 15)
    ) {
      result.abilities = ['Базовые характеристики задаются вручную, но каждая должна быть в диапазоне от 8 до 15.']
    }

    if (selectedRace?.skillChoiceRule) {
      const uniqueSelections = uniqueValues(draft.raceSkillSelections)
      if (
        uniqueSelections.length !== selectedRace.skillChoiceRule.count ||
        uniqueSelections.some((skill) => !selectedRace.skillChoiceRule?.availableSkills.includes(skill))
      ) {
        result.skills = [`Для расы ${selectedRace.name} нужно выбрать ${selectedRace.skillChoiceRule.count} допустимых навыка.`]
      }
    } else if (draft.raceSkillSelections.length > 0) {
      result.skills = ['У выбранной расы нет дополнительных навыков на выбор.']
    }

    const uniqueClassSelections = uniqueValues(draft.classSkillSelections)
    if (
      selectedClass &&
      (uniqueClassSelections.length !== selectedClass.skillChoiceRule.count ||
        uniqueClassSelections.some((skill) => !selectedClass.skillChoiceRule.availableSkills.includes(skill)) ||
        uniqueClassSelections.some((skill) => fixedSkillProficiencies.includes(skill)) ||
        uniqueClassSelections.some((skill) => draft.raceSkillSelections.includes(skill)))
    ) {
      result.skills = [`Для класса ${selectedClass.name} нужно выбрать ${selectedClass.skillChoiceRule.count} разных навыка без повторов.`]
    }

    return result
  }, [
    draft.baseAbilities,
    draft.bonusAbilitySelections,
    draft.classSkillSelections,
    draft.level,
    draft.name,
    draft.raceSkillSelections,
    fixedSkillProficiencies,
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
      raceSkillSelections: 'skills',
      classSkillSelections: 'skills',
    }

    return mapping[errorKey] ?? 'global'
  }

  function updateBaseAbility(key: string, score: number) {
    const clampedValue = Math.min(15, Math.max(8, Number.isNaN(score) ? 8 : score))
    setDraft((current) => ({
      ...current,
      baseAbilities: current.baseAbilities.map((ability) =>
        ability.key === key ? { ...ability, score: clampedValue } : ability,
      ),
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

  function toggleRaceSkill(skillId: string) {
    const raceSkillChoiceRule = selectedRace?.skillChoiceRule
    if (!raceSkillChoiceRule) {
      return
    }

    setDraft((current) => {
      if (current.raceSkillSelections.includes(skillId)) {
        return {
          ...current,
          raceSkillSelections: current.raceSkillSelections.filter((item) => item !== skillId),
        }
      }

      if (current.raceSkillSelections.length >= raceSkillChoiceRule.count) {
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

  function toggleClassSkill(skillId: string) {
    if (!selectedClass || fixedSkillProficiencies.includes(skillId) || draft.raceSkillSelections.includes(skillId)) {
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
      inventory: splitMultiline(draft.inventoryText),
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
    setInfoModal({
      title: race.name,
      subtitle: race.summary,
      details: race.details,
      facts: buildOptionFacts(race, 'race'),
    })
  }

  function openClassInfo(characterClass: ClassOption) {
    setInfoModal({
      title: characterClass.name,
      subtitle: characterClass.summary,
      details: characterClass.details,
      facts: buildOptionFacts(characterClass, 'class'),
    })
  }

  function openBackgroundInfo(background: BackgroundOption) {
    setInfoModal({
      title: background.name,
      subtitle: background.summary,
      details: background.details,
      facts: buildOptionFacts(background, 'background'),
    })
  }

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
      <section className="builder-header">
        <div>
          <h2>{isEditMode ? 'Редактирование персонажа' : 'Создание персонажа'}</h2>
        </div>
        <div className="wizard-steps" aria-label="Шаги конструктора">
          {steps.map((stepKey, index) => {
            const basePath = isEditMode && id ? `/characters/${id}/edit` : '/characters/new'
            return (
              <Link
                key={stepKey}
                to={`${basePath}/${stepKey}`}
                className={`wizard-step ${stepKey === currentStep ? 'active' : ''}`}
              >
                <span>{index + 1}</span>
                {getStepTitle(stepKey)}
              </Link>
            )
          })}
        </div>
      </section>

      <section className="builder-layout compact">
        <div className="builder-main">
          {currentStep === 'identity' ? (
            <article className="surface-card builder-section">
              <h3>Базовая информация</h3>
              <div className="form-grid compact">
                <label>
                  Имя персонажа
                  <input value={draft.name} onChange={(event) => updateDraft('name', event.target.value)} />
                </label>
                <label>
                  Уровень
                  <input
                    type="number"
                    min={1}
                    max={20}
                    value={draft.level}
                    onChange={(event) => updateDraft('level', Number(event.target.value))}
                  />
                </label>
                <label className="full-span">
                  Мировоззрение
                  <input
                    value={draft.alignment}
                    onChange={(event) => updateDraft('alignment', event.target.value)}
                  />
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
                        <article
                          key={race.id}
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
                            <small>{race.summary}</small>
                            <small>
                              Бонусы: {race.bonuses.map((bonus) => `${translateAbility(bonus.ability)} +${bonus.value}`).join(', ') || 'нет'}
                            </small>
                          </button>
                        </article>
                      ))}
                    </div>
                  </section>
                ))}
              </div>

              {selectedRace.bonusChoiceRule ? (
                <div className="bonus-choice-panel">
                  <p>{selectedRace.bonusChoiceRule.summary}</p>
                  <div className="bonus-choice-grid">
                    {selectedRace.bonusChoiceRule.allowedAbilities.map((ability) => (
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
              {getStepErrors('race').length > 0 ? <p className="inline-error">{getStepErrors('race')[0]}</p> : null}
            </article>
          ) : null}

          {currentStep === 'class' ? (
            <article className="surface-card builder-section">
              <h3>Класс</h3>
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
                      <small>Кость хитов d{item.hitDie}</small>
                      <small>Спасброски: {item.savingThrowProficiencies.map((ability) => translateAbility(ability)).join(', ')}</small>
                    </button>
                  </article>
                ))}
              </div>
              {getStepErrors('class').length > 0 ? <p className="inline-error">{getStepErrors('class')[0]}</p> : null}
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
              <p className="muted">Каждую базовую характеристику можно задать вручную в пределах от 8 до 15. Расовые бонусы применяются автоматически и сразу видны в расчёте.</p>
              <div className="ability-builder-grid compact">
                {computedAbilities.map((ability) => (
                  <article className="ability-builder-card compact" key={ability.key}>
                    <div>
                      <p className="ability-key">{translateAbility(ability.key)}</p>
                      <h4>{translateAbility(ability.key)}</h4>
                    </div>
                    <label>
                      Базовое значение
                      <input
                        type="number"
                        min={8}
                        max={15}
                        value={ability.baseScore}
                        onChange={(event) => updateBaseAbility(ability.key, Number(event.target.value))}
                      />
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

          {currentStep === 'skills' ? (
            <article className="surface-card builder-section">
              <h3>Владения навыками и спасбросками</h3>
              <div className="builder-subsection">
                <h4>Автоматически получаемые навыки</h4>
                <div className="skill-pick-grid">
                  {fixedSkillProficiencies.length > 0 ? fixedSkillProficiencies.map((skill) => (
                    <span key={skill} className="bonus-chip static">{translateSkill(skill)}</span>
                  )) : <span className="muted">Автоматических навыков нет.</span>}
                </div>
              </div>

              {selectedRace.skillChoiceRule ? (
                <div className="builder-subsection">
                  <h4>Навыки от расы</h4>
                  <p className="muted">{selectedRace.skillChoiceRule.summary}</p>
                  <div className="skill-pick-grid">
                    {selectedRace.skillChoiceRule.availableSkills.map((skill) => (
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

              <div className="builder-subsection">
                <h4>Навыки от класса</h4>
                <p className="muted">{selectedClass.skillChoiceRule.summary}</p>
                <div className="skill-pick-grid">
                  {selectedClass.skillChoiceRule.availableSkills.map((skill) => {
                    const isBlocked = fixedSkillProficiencies.includes(skill) || draft.raceSkillSelections.includes(skill)
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
                <h4>Спасброски класса</h4>
                <div className="skill-pick-grid">
                  {computedSavingThrows.filter((item) => item.isProficient).map((savingThrow) => (
                    <span key={savingThrow.ability} className="bonus-chip static">
                      {translateAbility(savingThrow.ability)} {savingThrow.bonus >= 0 ? `+${savingThrow.bonus}` : savingThrow.bonus}
                    </span>
                  ))}
                </div>
              </div>

              {getStepErrors('skills').length > 0 ? <p className="inline-error">{getStepErrors('skills')[0]}</p> : null}
            </article>
          ) : null}

          {currentStep === 'review' ? (
            <article className="surface-card builder-section">
              <h3>Проверка и завершение</h3>
              <p className="muted">На этом шаге можно добавить дополнительные заметки. Итоговые бонусы уже рассчитаны по выбранным расе, классу и владениям.</p>
              <div className="form-grid compact">
                <label className="full-span">
                  Заклинания
                  <textarea
                    rows={4}
                    value={draft.spellsText}
                    onChange={(event) => updateDraft('spellsText', event.target.value)}
                    placeholder="По одному заклинанию на строку"
                  />
                </label>
                <label className="full-span">
                  Инвентарь
                  <textarea
                    rows={4}
                    value={draft.inventoryText}
                    onChange={(event) => updateDraft('inventoryText', event.target.value)}
                    placeholder="По одному предмету на строку"
                  />
                </label>
                <label className="full-span">
                  Заметки
                  <textarea
                    rows={5}
                    value={draft.notes}
                    onChange={(event) => updateDraft('notes', event.target.value)}
                    placeholder="Краткая концепция персонажа"
                  />
                </label>
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
              <button type="button" className="primary-button button-reset" onClick={() => goToStep(1)}>
                Далее
              </button>
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

        <aside className="builder-sidebar">
          <article className="preview-card sticky-preview">
            <div className="preview-card__identity">
              <img
                className="preview-card__portrait"
                src={getCharacterPortrait(draft.name || 'Новый герой', selectedRace.name, selectedClass.name)}
                alt="Предпросмотр портрета персонажа"
              />
              <div>
                <h3>{draft.name || 'Новый персонаж'}</h3>
                <p>{selectedRace.name} • {selectedClass.name}</p>
              </div>
            </div>

            <div className="status-grid compact">
              <div className="status-card">
                <span>Класс доспеха</span>
                <strong>{estimatedArmorClass}</strong>
              </div>
              <div className="status-card">
                <span>Хиты</span>
                <strong>{estimatedHitPoints}</strong>
              </div>
              <div className="status-card">
                <span>Скорость</span>
                <strong>{selectedRace.speed}</strong>
              </div>
              <div className="status-card">
                <span>Бонус мастерства</span>
                <strong>+{proficiencyBonus}</strong>
              </div>
              <div className="status-card">
                <span>Пассивная внимательность</span>
                <strong>{passivePerception}</strong>
              </div>
              <div className="status-card">
                <span>Кость хитов</span>
                <strong>d{selectedClass.hitDie}</strong>
              </div>
            </div>

            <div className="sidebar-summary">
              <div className="skill-tags">
                {computedSkills.length > 0 ? computedSkills.slice(0, 6).map((skill) => (
                  <span key={skill.skillId} className="skill-tag">
                    {formatSkillLevel(skill)}
                  </span>
                )) : <span className="muted">Владения ещё не выбраны</span>}
              </div>
            </div>
          </article>
        </aside>
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

            <div className="modal-facts">
              {infoModal.facts.map((fact) => (
                <div key={fact.label} className="status-card">
                  <span>{fact.label}</span>
                  <strong>{fact.value}</strong>
                </div>
              ))}
            </div>

            <div className="stack">
              {infoModal.details.map((detail) => (
                <div key={detail.title} className="surface-card modal-detail">
                  <h4>{detail.title}</h4>
                  <p>{detail.description}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      ) : null}
    </div>
  )
}
