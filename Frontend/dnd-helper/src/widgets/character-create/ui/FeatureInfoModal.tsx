import type { FeatureInfoModalState } from '../../../features/character-create/model/characterCreateModel'

type FeatureInfoModalProps = {
  modal: FeatureInfoModalState | null
  onClose: () => void
}

export function FeatureInfoModal({ modal, onClose }: FeatureInfoModalProps) {
  if (!modal) {
    return null
  }

  return (
    <div className="modal-overlay" role="presentation" onClick={onClose}>
      <div className="modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3>{modal.title}</h3>
            <p className="section-text">{modal.description}</p>
          </div>
          <button type="button" className="secondary-button button-reset" onClick={onClose}>
            Закрыть
          </button>
        </div>
      </div>
    </div>
  )
}
