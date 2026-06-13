type CharacterNotesPanelProps = {
  notes: string
  notesDraft: string
  canEdit: boolean
  isSaving: boolean
  onNotesChange: (value: string) => void
  onSave: () => void
}

export function CharacterNotesPanel({
  notes,
  notesDraft,
  canEdit,
  isSaving,
  onNotesChange,
  onSave,
}: CharacterNotesPanelProps) {
  return (
    <article className="surface-card">
      <h3>Заметки</h3>
      {canEdit ? (
        <div className="stack">
          <textarea
            value={notesDraft}
            onChange={(event) => onNotesChange(event.target.value)}
            placeholder="Добавьте заметки по персонажу..."
            rows={6}
          />
          <button type="button" className="primary-button button-reset" onClick={onSave} disabled={isSaving}>
            {isSaving ? 'Сохранение...' : 'Сохранить заметки'}
          </button>
        </div>
      ) : <p>{notes || 'Пока без заметок.'}</p>}
    </article>
  )
}
