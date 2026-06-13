import type { EquipmentCatalogItem } from '../../../types/character'
import type { EquippedSlots } from '../../../features/character-detail/model/characterDetailModel'

type InventoryItemView = {
  slug: string
  index: number
  key: string
}

type InventoryPanelProps = {
  items: InventoryItemView[]
  equipmentBySlug: Map<string, EquipmentCatalogItem>
  equippedSlots: EquippedSlots
  bodyEquipOptions: string[]
  mainHandEquipOptions: string[]
  offHandEquipOptions: string[]
  isMainHandTwoHanded: boolean
  recentlyAddedInventoryKey: string | null
  canEdit: boolean
  isSaving: boolean
  onOpenPicker: () => void
  onRemoveItem: (index: number) => void
  onEquipItem: (slot: keyof EquippedSlots, value: string) => void
  onSave: () => void
}

export function InventoryPanel({
  items,
  equipmentBySlug,
  equippedSlots,
  bodyEquipOptions,
  mainHandEquipOptions,
  offHandEquipOptions,
  isMainHandTwoHanded,
  recentlyAddedInventoryKey,
  canEdit,
  isSaving,
  onOpenPicker,
  onRemoveItem,
  onEquipItem,
  onSave,
}: InventoryPanelProps) {
  return (
    <article className="surface-card">
      <div className="section-header-row">
        <h3>Инвентарь</h3>
        {canEdit ? (
          <button type="button" className="secondary-button button-reset compact-plus-button" onClick={onOpenPicker}>
            +
          </button>
        ) : null}
      </div>
      <div className="stack">
        <div className="skill-pick-grid">
          {items.length > 0 ? items.map(({ slug, index, key }) => (
            <span key={key} className={`bonus-chip static ${recentlyAddedInventoryKey === key ? 'selection-flash' : ''}`}>
              {equipmentBySlug.get(slug)?.name ?? slug}
              {canEdit ? (
                <button
                  type="button"
                  className="button-reset icon-remove-button"
                  aria-label="Удалить предмет"
                  onClick={() => onRemoveItem(index)}
                >
                  ×
                </button>
              ) : null}
            </span>
          )) : <span className="muted">Предметы не добавлены.</span>}
        </div>

        <div className="form-grid compact">
          <label>
            Тело
            <select
              className="app-select"
              value={equippedSlots.body ?? ''}
              onChange={(event) => onEquipItem('body', event.target.value)}
              disabled={!canEdit}
            >
              <option value="">Не экипировано</option>
              {bodyEquipOptions.map((slug, index) => (
                <option key={`${slug}-body-${index}`} value={slug}>{equipmentBySlug.get(slug)?.name ?? slug}</option>
              ))}
            </select>
          </label>
          <label>
            Правая рука
            <select
              className="app-select"
              value={equippedSlots.mainHand ?? ''}
              onChange={(event) => onEquipItem('mainHand', event.target.value)}
              disabled={!canEdit}
            >
              <option value="">Не экипировано</option>
              {mainHandEquipOptions.map((slug, index) => (
                <option key={`${slug}-main-${index}`} value={slug}>{equipmentBySlug.get(slug)?.name ?? slug}</option>
              ))}
            </select>
          </label>
          <label>
            Левая рука
            <select
              className="app-select"
              value={equippedSlots.offHand ?? ''}
              onChange={(event) => onEquipItem('offHand', event.target.value)}
              disabled={!canEdit || isMainHandTwoHanded}
            >
              <option value="">Не экипировано</option>
              {offHandEquipOptions.map((slug, index) => (
                <option key={`${slug}-off-${index}`} value={slug}>{equipmentBySlug.get(slug)?.name ?? slug}</option>
              ))}
            </select>
            {isMainHandTwoHanded ? <small className="muted">Левая рука занята двуручным оружием.</small> : null}
          </label>
        </div>
        {canEdit ? (
          <button type="button" className="primary-button button-reset" onClick={onSave} disabled={isSaving}>
            {isSaving ? 'Сохранение...' : 'Сохранить инвентарь'}
          </button>
        ) : null}
      </div>
    </article>
  )
}
