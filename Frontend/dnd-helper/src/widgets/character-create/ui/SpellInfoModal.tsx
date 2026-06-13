import type { SpellInfoModalState } from '../../../features/character-create/model/characterCreateModel'

type SpellInfoModalProps = {
  modal: SpellInfoModalState | null
  onClose: () => void
}

export function SpellInfoModal({ modal, onClose }: SpellInfoModalProps) {
  if (!modal) {
    return null
  }

  return (
    <div className="modal-overlay" role="presentation" onClick={onClose}>
      <div className="modal-card spell-modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h3>{modal.name}</h3>
            <p className="section-text">{modal.summary ?? 'Описание из Книги игрока'}</p>
          </div>
          <button type="button" className="secondary-button button-reset" onClick={onClose}>
            Закрыть
          </button>
        </div>
        <div className="spell-meta-grid">
          <div className="status-card"><span>Круг</span><strong>{modal.circle}</strong></div>
          <div className="status-card"><span>Мин. уровень</span><strong>{modal.minLevel}</strong></div>
          <div className="status-card"><span>Классы</span><strong>{modal.classes.join(', ') || '-'}</strong></div>
        </div>
        <article className="surface-card spell-description-block">
          <h4>Эффект заклинания</h4>
          <p>{modal.description ?? 'Подробное описание пока не заполнено в базе правил.'}</p>
        </article>
      </div>
    </div>
  )
}
