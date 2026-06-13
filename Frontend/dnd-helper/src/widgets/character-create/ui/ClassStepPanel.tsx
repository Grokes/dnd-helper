import type { ClassOption, FeatureDetail } from '../../../types/character'
import { buildInfoIconLabel } from '../../../features/character-create/model/characterCreateModel'
import { translateAbility, translateSkill } from '../../../utils/characterPresentation'

type ProficiencyDisplayItem = {
  label: string
  items: string[]
  interactive: boolean
}

type SavingThrowView = {
  ability: string
  isProficient: boolean
}

type ClassStepPanelProps = {
  classes: ClassOption[]
  selectedClass: ClassOption
  selectedClassId: string
  level: number
  armorDisplayItems: ProficiencyDisplayItem[]
  weaponDisplayItems: ProficiencyDisplayItem[]
  classSkillSelections: string[]
  raceSkillSelections: string[]
  savingThrows: SavingThrowView[]
  groupedClassFeatures: Array<[number, string[]]>
  expandedClassFeatureLevel: string | null
  availableClassFeatures: FeatureDetail[]
  onLevelChange: (level: number) => void
  onSelectClass: (classId: string) => void
  onOpenClassInfo: (characterClass: ClassOption) => void
  onOpenProficiencyItems: (title: string, items: string[]) => void
  onToggleClassSkill: (skillId: string) => void
  onToggleFeatureLevel: (level: number) => void
  onOpenFeatureInfo: (title: string, description: string) => void
}

export function ClassStepPanel({
  classes,
  selectedClass,
  selectedClassId,
  level,
  armorDisplayItems,
  weaponDisplayItems,
  classSkillSelections,
  raceSkillSelections,
  savingThrows,
  groupedClassFeatures,
  expandedClassFeatureLevel,
  availableClassFeatures,
  onLevelChange,
  onSelectClass,
  onOpenClassInfo,
  onOpenProficiencyItems,
  onToggleClassSkill,
  onToggleFeatureLevel,
  onOpenFeatureInfo,
}: ClassStepPanelProps) {
  return (
    <article className="surface-card builder-section">
      <h3>Класс</h3>
      <div className="form-grid compact">
        <label className="full-span">
          Уровень персонажа
          <input
            type="number"
            min={1}
            max={20}
            value={level}
            onChange={(event) => onLevelChange(Number(event.target.value))}
          />
        </label>
      </div>
      <div className="choice-grid compact">
        {classes.map((item) => (
          <article
            key={item.id}
            className={`choice-card slim choice-card--static ${item.id === selectedClassId ? 'selected' : ''}`}
          >
            <button
              type="button"
              className="info-icon-button"
              aria-label={buildInfoIconLabel(item.name)}
              onClick={() => onOpenClassInfo(item)}
            >
              i
            </button>
            <button type="button" className="choice-card__main" onClick={() => onSelectClass(item.id)}>
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
                      onClick={() => onOpenProficiencyItems(entry.label, entry.items)}
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
                      onClick={() => onOpenProficiencyItems(entry.label, entry.items)}
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
            const isBlocked = raceSkillSelections.includes(skill)
            return (
              <button
                key={skill}
                type="button"
                className={`skill-toggle ${classSkillSelections.includes(skill) ? 'selected' : ''}`}
                onClick={() => onToggleClassSkill(skill)}
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
          {savingThrows.filter((item) => item.isProficient).map((savingThrow) => (
            <span key={savingThrow.ability} className="bonus-chip static">
              {translateAbility(savingThrow.ability)}
            </span>
          ))}
        </div>
      </div>
      <div className="builder-subsection">
        <h4>Классовые особенности по уровню</h4>
        <div className="stack">
          {groupedClassFeatures.length > 0 ? groupedClassFeatures.map(([featureLevel, features]) => (
            <article key={`level-${featureLevel}`} className="surface-card modal-detail class-feature-level-card">
              <button
                type="button"
                className="disclosure-button"
                onClick={() => onToggleFeatureLevel(featureLevel)}
              >
                <span>{featureLevel === 0 ? 'Дополнительно' : `${featureLevel} уровень`}</span>
                <strong>{expandedClassFeatureLevel === `class-level-${featureLevel}` ? 'Скрыть' : 'Показать'}</strong>
              </button>
              {expandedClassFeatureLevel === `class-level-${featureLevel}` ? (
                <div className="skill-pick-grid">
                  {features.map((feature) => {
                    const full = availableClassFeatures.find((item) =>
                      item.title.toLowerCase().includes(feature.toLowerCase()),
                    )
                    return (
                      <button
                        key={`${featureLevel}-${feature}`}
                        type="button"
                        className="bonus-chip"
                        onClick={() => onOpenFeatureInfo(feature, full?.description ?? 'Описание будет уточнено.')}
                      >
                        {feature}
                      </button>
                    )
                  })}
                </div>
              ) : null}
            </article>
          )) : <span className="muted">Для этого уровня пока нет отображаемых особенностей.</span>}
        </div>
      </div>
    </article>
  )
}
