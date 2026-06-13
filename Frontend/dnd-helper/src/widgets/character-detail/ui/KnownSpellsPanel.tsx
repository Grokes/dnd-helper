type KnownSpellsPanelProps = {
  spellKeys: string[]
  spellNames: string[]
  recentlyAddedSpell: string | null
  canEdit: boolean
  isSaving: boolean
  onOpenPicker: () => void
  onOpenSpell: (spellKey: string) => void
  onRemoveSpell: (spellKey: string) => void
  onSave: () => void
}

export function KnownSpellsPanel({
  spellKeys,
  spellNames,
  recentlyAddedSpell,
  canEdit,
  isSaving,
  onOpenPicker,
  onOpenSpell,
  onRemoveSpell,
  onSave,
}: KnownSpellsPanelProps) {
  return (
    <article className="surface-card">
      <div className="section-header-row">
        <h3>Известные заклинания</h3>
        {canEdit ? (
          <button type="button" className="secondary-button button-reset compact-plus-button" onClick={onOpenPicker}>
            +
          </button>
        ) : null}
      </div>
      <div className="skill-pick-grid">
        {spellKeys.length > 0 ? (
          spellKeys.map((spellKey, index) => (
            <span key={`${spellKey}-${index}`} className={`bonus-chip static ${recentlyAddedSpell === spellKey ? 'selection-flash' : ''}`}>
              <button
                type="button"
                className="button-reset spell-chip-trigger"
                onClick={() => onOpenSpell(spellKey)}
              >
                {spellNames[index]}
              </button>
              {canEdit ? (
                <button
                  type="button"
                  className="button-reset icon-remove-button"
                  aria-label="Удалить заклинание"
                  onClick={() => onRemoveSpell(spellKey)}
                >
                  ×
                </button>
              ) : null}
            </span>
          ))
        ) : <span className="muted">Нет данных</span>}
      </div>
      {canEdit ? (
        <button type="button" className="primary-button button-reset" onClick={onSave} disabled={isSaving}>
          {isSaving ? 'Сохранение...' : 'Сохранить заклинания'}
        </button>
      ) : null}
    </article>
  )
}
