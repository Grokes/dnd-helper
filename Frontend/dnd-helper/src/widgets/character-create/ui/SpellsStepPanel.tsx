import type { RuleSpellItem } from '../../../types/character'
import { buildInfoIconLabel, translateClassSlug } from '../../../features/character-create/model/characterCreateModel'

type SpellInfoView = {
  name: string
  circle: number
  minLevel: number
  classes: string[]
  summary?: string
  description?: string
}

type SpellsStepPanelProps = {
  availableSpells: RuleSpellItem[]
  selectedSpells: string[]
  spellCatalog: RuleSpellItem[]
  spellSearch: string
  isSpellPickerOpen: boolean
  recentlyAddedSpell: string | null
  onTogglePicker: () => void
  onSearchChange: (value: string) => void
  onAddSpell: (slug: string) => void
  onRemoveSpell: (slug: string) => void
  onOpenSpellInfo: (spell: SpellInfoView) => void
}

export function SpellsStepPanel({
  availableSpells,
  selectedSpells,
  spellCatalog,
  spellSearch,
  isSpellPickerOpen,
  recentlyAddedSpell,
  onTogglePicker,
  onSearchChange,
  onAddSpell,
  onRemoveSpell,
  onOpenSpellInfo,
}: SpellsStepPanelProps) {
  return (
    <article className="surface-card builder-section">
      <h3>Заклинания</h3>
      <label className="full-span">
        Заклинания
        <div className="skill-pick-grid">
          <button type="button" className="secondary-button button-reset" onClick={onTogglePicker}>
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
                onChange={(event) => onSearchChange(event.target.value)}
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
                      onOpenSpellInfo({
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
                    onClick={() => onAddSpell(spell.slug)}
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
                onClick={() => onRemoveSpell(spell)}
              >
                ×
              </button>
            </span>
          )) : <span className="muted">Заклинания пока не выбраны.</span>}
        </div>
      </label>
    </article>
  )
}
