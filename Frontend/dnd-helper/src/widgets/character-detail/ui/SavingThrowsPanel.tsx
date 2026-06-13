import type { SavingThrowBonus } from '../../../types/character'
import { translateAbility } from '../../../utils/characterPresentation'

type SavingThrowsPanelProps = {
  savingThrows: SavingThrowBonus[]
  onRoll: (label: string, dieSides: number, modifier?: number) => void
}

export function SavingThrowsPanel({ savingThrows, onRoll }: SavingThrowsPanelProps) {
  return (
    <article className="surface-card">
      <h3>Спасброски</h3>
      <ul className="plain-list sheet-list">
        {savingThrows.map((savingThrow) => (
          <li key={savingThrow.ability}>
            <button
              type="button"
              className="button-reset rollable-list-button sheet-list-button"
              onClick={() => onRoll(`Спасбросок: ${translateAbility(savingThrow.ability)}`, 20, savingThrow.bonus)}
            >
              {translateAbility(savingThrow.ability)} {savingThrow.bonus >= 0 ? `+${savingThrow.bonus}` : savingThrow.bonus}
              {savingThrow.isProficient ? ' • владение' : ''}
            </button>
          </li>
        ))}
      </ul>
    </article>
  )
}
