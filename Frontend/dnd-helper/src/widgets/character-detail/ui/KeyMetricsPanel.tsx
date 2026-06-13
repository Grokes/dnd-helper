import type { Character } from '../../../types/character'
import type { EquippedSlots } from '../../../features/character-detail/model/characterDetailModel'

type WeaponSlot = 'mainHand' | 'offHand'
type RestType = 'short' | 'long' | 'full-heal'

type KeyMetricsPanelProps = {
  character: Character
  canEdit: boolean
  equippedSlots: EquippedSlots
  initiativeModifier: number
  mainHandDamage: string
  offHandDamage: string
  shortRestHitDice: number
  isSaving: boolean
  saveStatus: string | null
  onInitiativeRoll: () => void
  onWeaponAttackRoll: (slot: WeaponSlot) => void
  onWeaponDamageRoll: (slot: WeaponSlot) => void
  onShortRestHitDiceChange: (value: number) => void
  onRest: (restType: RestType) => void
}

export function KeyMetricsPanel({
  character,
  canEdit,
  equippedSlots,
  initiativeModifier,
  mainHandDamage,
  offHandDamage,
  shortRestHitDice,
  isSaving,
  saveStatus,
  onInitiativeRoll,
  onWeaponAttackRoll,
  onWeaponDamageRoll,
  onShortRestHitDiceChange,
  onRest,
}: KeyMetricsPanelProps) {
  return (
    <article className="surface-card">
      <h3>Ключевые показатели</h3>
      <div className="compact-stats">
        <div className="key-metric">
          <span>Класс доспеха</span>
          <strong>{character.armorClass}</strong>
        </div>
        <div className="key-metric">
          <span>Хиты</span>
          <strong>{character.currentHitPoints}/{character.maxHitPoints}</strong>
        </div>
        <div className="compact-stats__weapon">
          <span className="compact-stats__weapon-title">Боевой блок</span>
          <div className="weapon-damage-lines">
            <button
              type="button"
              className="button-reset rollable-list-button weapon-roll-button"
              onClick={() => onWeaponAttackRoll('mainHand')}
              disabled={!equippedSlots.mainHand}
            >
              Попадание • Правая рука
            </button>
            <button
              type="button"
              className="button-reset rollable-list-button weapon-roll-button"
              onClick={() => onWeaponDamageRoll('mainHand')}
              disabled={!equippedSlots.mainHand}
            >
              Урон • Правая: {mainHandDamage}
            </button>
            <button
              type="button"
              className="button-reset rollable-list-button weapon-roll-button"
              onClick={() => onWeaponAttackRoll('offHand')}
              disabled={!equippedSlots.offHand}
            >
              Попадание • Левая рука
            </button>
            <button
              type="button"
              className="button-reset rollable-list-button weapon-roll-button"
              onClick={() => onWeaponDamageRoll('offHand')}
              disabled={!equippedSlots.offHand}
            >
              Урон • Левая: {offHandDamage}
            </button>
          </div>
        </div>
        <div className="key-metric">
          <span>Скорость</span>
          <strong>{character.speed}</strong>
        </div>
        <div className="key-metric">
          <span>Инициатива</span>
          <button
            type="button"
            className="button-reset key-metric-action"
            onClick={onInitiativeRoll}
          >
            {initiativeModifier >= 0 ? `+${initiativeModifier}` : initiativeModifier}
          </button>
        </div>
        <div className="key-metric">
          <span>Бонус мастерства</span>
          <strong>+{character.proficiencyBonus}</strong>
        </div>
        <div className="key-metric">
          <span>Пассивная внимательность</span>
          <strong>{character.passivePerception}</strong>
        </div>
      </div>
      {canEdit ? (
        <div className="stack">
          <div className="section-header-row">
            <h4>Отдых</h4>
            <span className="muted">Костей хитов доступно: {character.availableHitDice}</span>
          </div>
          <div className="form-grid compact">
            <label>
              Короткий отдых: потратить костей хитов
              <input
                className="app-search-input"
                type="number"
                min={0}
                max={Math.max(0, character.availableHitDice)}
                value={shortRestHitDice}
                onChange={(event) => onShortRestHitDiceChange(Math.max(0, Number(event.target.value) || 0))}
                disabled={isSaving}
              />
            </label>
          </div>
          <div className="skill-pick-grid">
            <button type="button" className="secondary-button button-reset" onClick={() => onRest('short')} disabled={isSaving || character.availableHitDice <= 0}>
              Короткий отдых
            </button>
            <button type="button" className="secondary-button button-reset" onClick={() => onRest('long')} disabled={isSaving}>
              Длительный отдых
            </button>
            <button type="button" className="primary-button button-reset" onClick={() => onRest('full-heal')} disabled={isSaving}>
              Полностью вылечить
            </button>
          </div>
        </div>
      ) : null}
      {saveStatus ? <p className={saveStatus.includes('Не удалось') ? 'inline-error' : 'success-text'}>{saveStatus}</p> : null}
    </article>
  )
}
