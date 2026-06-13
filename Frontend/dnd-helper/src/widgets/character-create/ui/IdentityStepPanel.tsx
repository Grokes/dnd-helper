type IdentityStepPanelProps = {
  name: string
  errors: string[]
  onNameChange: (value: string) => void
}

export function IdentityStepPanel({ name, errors, onNameChange }: IdentityStepPanelProps) {
  return (
    <article className="surface-card builder-section">
      <h3>Базовая информация</h3>
      <div className="form-grid compact">
        <label className="full-span">
          Имя персонажа
          <input value={name} onChange={(event) => onNameChange(event.target.value)} />
        </label>
      </div>
      {errors.length > 0 ? <p className="inline-error">{errors[0]}</p> : null}
    </article>
  )
}
