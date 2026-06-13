type SpellSlot = {
  spellLevel: number
  slots: number
}

type SpellSlotsPanelProps = {
  currentSlots: SpellSlot[]
  maxSlots: SpellSlot[]
}

export function SpellSlotsPanel({ currentSlots, maxSlots }: SpellSlotsPanelProps) {
  return (
    <article className="surface-card">
      <h3>Ячейки заклинаний</h3>
      <ul className="plain-list sheet-list">
        {maxSlots.length > 0
          ? maxSlots.map((slot) => {
              const current = currentSlots.find((item) => item.spellLevel === slot.spellLevel)?.slots ?? 0
              return (
                <li key={slot.spellLevel}>
                  <span className="sheet-list-static">Круг {slot.spellLevel}: {current}/{slot.slots}</span>
                </li>
              )
            })
          : <li>Нет ячеек</li>}
      </ul>
    </article>
  )
}
