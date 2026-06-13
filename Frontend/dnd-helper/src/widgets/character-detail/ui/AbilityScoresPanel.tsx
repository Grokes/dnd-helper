import type { AbilityScore } from '../../../types/character'
import { translateAbility } from '../../../utils/characterPresentation'

type AbilityScoresPanelProps = {
  abilities: AbilityScore[]
  onRoll: (label: string, dieSides: number, modifier?: number) => void
}

export function AbilityScoresPanel({ abilities, onRoll }: AbilityScoresPanelProps) {
  return (
    <article className="surface-card">
      <h3>Характеристики</h3>
      <div className="ability-grid">
        {abilities.map((ability) => (
          <button
            type="button"
            className="ability-card ability-card--interactive button-reset"
            key={ability.key}
            onClick={() => onRoll(`Проверка: ${translateAbility(ability.key)}`, 20, ability.modifier)}
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
  )
}
