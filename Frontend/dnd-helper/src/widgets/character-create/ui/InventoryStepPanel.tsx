import type { EquipmentCatalogItem } from '../../../types/character'
import type { EquipmentSlots } from '../../../features/character-create/model/characterCreateModel'

type InventoryItemView = {
  slug: string
  index: number
  key: string
}

type InventoryStepPanelProps = {
  filteredEquipmentCatalog: EquipmentCatalogItem[]
  equipmentMap: Map<string, EquipmentCatalogItem>
  inventoryWithIndexes: InventoryItemView[]
  inventorySearch: string
  isInventoryPickerOpen: boolean
  recentlyAddedInventorySlug: string | null
  recentlyAddedInventoryKey: string | null
  equippedSlots: EquipmentSlots
  bodyEquipOptions: string[]
  mainHandEquipOptions: string[]
  offHandEquipOptions: string[]
  isMainHandTwoHanded: boolean
  onTogglePicker: () => void
  onSearchChange: (value: string) => void
  onAddInventoryItem: (slug: string) => void
  onRemoveInventoryItem: (index: number) => void
  onEquipItem: (slot: keyof EquipmentSlots, slug: string) => void
}

export function InventoryStepPanel({
  filteredEquipmentCatalog,
  equipmentMap,
  inventoryWithIndexes,
  inventorySearch,
  isInventoryPickerOpen,
  recentlyAddedInventorySlug,
  recentlyAddedInventoryKey,
  equippedSlots,
  bodyEquipOptions,
  mainHandEquipOptions,
  offHandEquipOptions,
  isMainHandTwoHanded,
  onTogglePicker,
  onSearchChange,
  onAddInventoryItem,
  onRemoveInventoryItem,
  onEquipItem,
}: InventoryStepPanelProps) {
  return (
    <article className="surface-card builder-section">
      <h3>Инвентарь</h3>
      <div className="form-grid compact">
        <label className="full-span">
          Предметы
          <div className="skill-pick-grid">
            <button type="button" className="secondary-button button-reset" onClick={onTogglePicker}>
              + Добавить предмет
            </button>
          </div>
          {isInventoryPickerOpen ? (
            <div className="surface-card">
              <label className="full-span">
                Поиск предмета
                <input
                  className="app-search-input"
                  value={inventorySearch}
                  onChange={(event) => onSearchChange(event.target.value)}
                  placeholder="Лук, щит, фонарь..."
                />
              </label>
              <div className="skill-pick-grid">
                {filteredEquipmentCatalog.map((item) => (
                  <button
                    key={item.slug}
                    type="button"
                    className={`skill-toggle ${recentlyAddedInventorySlug === item.slug ? 'selected selection-flash' : ''}`}
                    onClick={() => onAddInventoryItem(item.slug)}
                  >
                    {item.name}
                  </button>
                ))}
              </div>
            </div>
          ) : null}
          <div className="skill-pick-grid">
            {inventoryWithIndexes.length === 0 ? <span className="muted">Предметы пока не добавлены.</span> : inventoryWithIndexes.map(({ slug, index, key }) => (
              <span key={key} className={`bonus-chip static ${recentlyAddedInventoryKey === key ? 'selection-flash' : ''}`}>
                {equipmentMap.get(slug)?.name ?? slug}
                <button
                  type="button"
                  className="button-reset icon-remove-button"
                  aria-label="Удалить предмет"
                  onClick={() => onRemoveInventoryItem(index)}
                >
                  ×
                </button>
              </span>
            ))}
          </div>
        </label>

        <label className="full-span">
          Экипировка
          <div className="form-grid compact">
            <label>
              Тело
              <select className="app-select" value={equippedSlots.body ?? ''} onChange={(event) => onEquipItem('body', event.target.value)}>
                <option value="">Не экипировано</option>
                {bodyEquipOptions.map((slug, index) => <option key={`${slug}-body-${index}`} value={slug}>{equipmentMap.get(slug)?.name}</option>)}
              </select>
            </label>
            <label>
              Правая рука
              <select className="app-select" value={equippedSlots.mainHand ?? ''} onChange={(event) => onEquipItem('mainHand', event.target.value)}>
                <option value="">Не экипировано</option>
                {mainHandEquipOptions.map((slug, index) => <option key={`${slug}-main-${index}`} value={slug}>{equipmentMap.get(slug)?.name}</option>)}
              </select>
            </label>
            <label>
              Левая рука
              <select
                className="app-select"
                value={equippedSlots.offHand ?? ''}
                onChange={(event) => onEquipItem('offHand', event.target.value)}
                disabled={isMainHandTwoHanded}
              >
                <option value="">Не экипировано</option>
                {offHandEquipOptions.map((slug, index) => <option key={`${slug}-off-${index}`} value={slug}>{equipmentMap.get(slug)?.name}</option>)}
              </select>
              {isMainHandTwoHanded ? <small className="muted">Левая рука заблокирована двуручным оружием.</small> : null}
            </label>
          </div>
        </label>
      </div>
    </article>
  )
}
