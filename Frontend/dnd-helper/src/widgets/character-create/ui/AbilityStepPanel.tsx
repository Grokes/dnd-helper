import { translateAbility } from '../../../utils/characterPresentation'

type ComputedAbilityView = {
  key: string
  baseScore: number
  bonus: number
  total: number
  modifier: number
}

type AbilityStepPanelProps = {
  abilities: ComputedAbilityView[]
  minScore: number
  maxScore: number
  errors: string[]
  onRandomize: () => void
  onUpdateBaseAbility: (key: string, score: number) => void
}

export function AbilityStepPanel({
  abilities,
  minScore,
  maxScore,
  errors,
  onRandomize,
  onUpdateBaseAbility,
}: AbilityStepPanelProps) {
  return (
    <article className="surface-card builder-section">
      <h3>Характеристики</h3>
      <p className="muted">
        Каждую базовую характеристику можно задать вручную в пределах от {minScore} до {maxScore}. Расовые бонусы применяются автоматически и сразу видны в расчёте.
      </p>
      <button type="button" className="secondary-button button-reset ability-random-button" onClick={onRandomize}>
        Случайно распределить характеристики
      </button>
      <div className="ability-builder-grid compact">
        {abilities.map((ability) => (
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
                  onClick={() => onUpdateBaseAbility(ability.key, ability.baseScore - 1)}
                >
                  -
                </button>
                <input
                  type="number"
                  min={minScore}
                  max={maxScore}
                  value={ability.baseScore}
                  onChange={(event) => onUpdateBaseAbility(ability.key, Number(event.target.value))}
                />
                <button
                  type="button"
                  className="stepper-button"
                  onClick={() => onUpdateBaseAbility(ability.key, ability.baseScore + 1)}
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
      {errors.length > 0 ? <p className="inline-error">{errors[0]}</p> : null}
    </article>
  )
}
