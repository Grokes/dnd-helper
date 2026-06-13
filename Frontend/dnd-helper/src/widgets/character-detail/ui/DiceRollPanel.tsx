type DiceRollPanelProps = {
  onRoll: (label: string, dieSides: number, modifier?: number) => void
}

const diceSides = [4, 6, 8, 10, 12, 20, 100]

export function DiceRollPanel({ onRoll }: DiceRollPanelProps) {
  return (
    <article className="surface-card">
      <div className="section-header-row">
        <h3>Броски кубов</h3>
      </div>
      <div className="skill-pick-grid">
        {diceSides.map((sides) => (
          <button key={sides} type="button" className="bonus-chip" onClick={() => onRoll(`Свободный бросок d${sides}`, sides, 0)}>
            d{sides}
          </button>
        ))}
      </div>
    </article>
  )
}
