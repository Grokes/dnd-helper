import type { BackgroundOption } from '../../../types/character'
import { buildInfoIconLabel } from '../../../features/character-create/model/characterCreateModel'
import { translateSkill } from '../../../utils/characterPresentation'

type BackgroundStepPanelProps = {
  backgrounds: BackgroundOption[]
  selectedBackgroundId: string
  errors: string[]
  onSelectBackground: (backgroundId: string) => void
  onOpenBackgroundInfo: (background: BackgroundOption) => void
}

export function BackgroundStepPanel({
  backgrounds,
  selectedBackgroundId,
  errors,
  onSelectBackground,
  onOpenBackgroundInfo,
}: BackgroundStepPanelProps) {
  return (
    <article className="surface-card builder-section">
      <h3>Предыстория</h3>
      <div className="choice-grid compact">
        {backgrounds.map((item) => (
          <article
            key={item.id}
            className={`choice-card slim choice-card--static ${item.id === selectedBackgroundId ? 'selected' : ''}`}
          >
            <button
              type="button"
              className="info-icon-button"
              aria-label={buildInfoIconLabel(item.name)}
              onClick={() => onOpenBackgroundInfo(item)}
            >
              i
            </button>
            <button
              type="button"
              className="choice-card__main"
              onClick={() => onSelectBackground(item.id)}
            >
              <strong>{item.name}</strong>
              <small>{item.summary}</small>
              <small>{item.description ?? item.summary}</small>
              <small>Навыки: {item.grantedSkillProficiencies.map((skill) => translateSkill(skill)).join(', ')}</small>
            </button>
          </article>
        ))}
      </div>
      {errors.length > 0 ? <p className="inline-error">{errors[0]}</p> : null}
    </article>
  )
}
