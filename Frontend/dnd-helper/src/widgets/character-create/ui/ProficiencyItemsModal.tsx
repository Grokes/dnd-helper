import type { ProficiencyItemsModalState } from '../../../features/character-create/model/characterCreateModel'

type ProficiencyItemsModalProps = {
  modal: ProficiencyItemsModalState | null
  onClose: () => void
}

export function ProficiencyItemsModal({ modal, onClose }: ProficiencyItemsModalProps) {
  if (!modal) {
    return null
  }

  return (
    <div className="modal-overlay" role="presentation" onClick={onClose}>
      <div className="modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3>{modal.title}</h3>
            <p className="section-text">Список предметов по выбранному типу владения</p>
          </div>
          <button type="button" className="secondary-button button-reset" onClick={onClose}>
            Закрыть
          </button>
        </div>
        <div className="skill-pick-grid">
          {modal.items.length > 0
            ? modal.items.map((item) => <span key={item} className="bonus-chip static">{item}</span>)
            : <p className="muted">Список предметов пока не заполнен.</p>}
        </div>
      </div>
    </div>
  )
}
