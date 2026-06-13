import type { BackgroundOption, ClassOption, RaceOption } from '../../../types/character'
import { translateAbility } from '../../../utils/characterPresentation'

type ComputedAbilityView = {
  key: string
  total: number
  modifier: number
}

type SkillPreview = {
  skillId: string
  label: string
  level: number
  proficient: boolean
}

type ReviewStepPanelProps = {
  name: string
  level: number
  selectedRace: RaceOption
  selectedClass: ClassOption
  selectedBackground: BackgroundOption
  estimatedArmorClass: number
  estimatedHitPoints: number
  passivePerception: number
  abilities: ComputedAbilityView[]
  skills: SkillPreview[]
  error: string | null
}

export function ReviewStepPanel({
  name,
  level,
  selectedRace,
  selectedClass,
  selectedBackground,
  estimatedArmorClass,
  estimatedHitPoints,
  passivePerception,
  abilities,
  skills,
  error,
}: ReviewStepPanelProps) {
  return (
    <article className="surface-card builder-section">
      <h3>Проверка и завершение</h3>
      <p className="muted">Итоговый лист персонажа с применёнными бонусами и выбранными владениями.</p>
      <div className="review-sheet review-sheet--vertical">
        <div className="review-head">
          <div className="review-field"><span>Имя</span><strong>{name || '—'}</strong></div>
          <div className="review-field"><span>Предыстория</span><strong>{selectedBackground.name}</strong></div>
          <div className="review-field"><span>Раса</span><strong>{selectedRace.name}</strong></div>
          <div className="review-field"><span>Класс</span><strong>{selectedClass.name}</strong></div>
          <div className="review-field"><span>Уровень</span><strong>{level}</strong></div>
        </div>
        <div className="status-grid compact">
          <div className="status-card"><span>КД</span><strong>{estimatedArmorClass}</strong></div>
          <div className="status-card"><span>Хиты</span><strong>{estimatedHitPoints}</strong></div>
          <div className="status-card"><span>Скорость</span><strong>{selectedRace.speed}</strong></div>
          <div className="status-card"><span>Пассивная внимательность</span><strong>{passivePerception}</strong></div>
        </div>
        <div className="review-abilities">
          {abilities.map((ability) => (
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
          {skills.map((skill) => (
            <span key={skill.skillId} className={`bonus-chip ${skill.proficient ? 'selected' : 'static'}`}>
              {skill.label} {skill.level >= 0 ? `+${skill.level}` : skill.level}
            </span>
          ))}
        </div>
      </div>
      {error ? <p className="inline-error">{error}</p> : null}
    </article>
  )
}
