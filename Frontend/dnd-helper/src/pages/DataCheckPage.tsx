import { useEffect, useMemo, useState } from 'react'
import { getCharacterOptions } from '../features/characters/api/charactersApi'
import { getEquipmentCatalog, getMonstersCatalog, getRulesConditions, getRulesSpells } from '../features/rules/api/rulesApi'
import { catalogTabs, translateClassSlug, translateToken, type CatalogDetailModal, type CatalogTabKey } from '../features/rules/model/catalogModel'
import type { CharacterOptions, EquipmentCatalogItem, MonsterCatalogItem, RuleConditionItem, RuleSpellItem } from '../types/character'
import { translateAbility, translateSkill } from '../utils/characterPresentation'

export function DataCheckPage() {
  const [equipment, setEquipment] = useState<EquipmentCatalogItem[]>([])
  const [monsters, setMonsters] = useState<MonsterCatalogItem[]>([])
  const [spells, setSpells] = useState<RuleSpellItem[]>([])
  const [conditions, setConditions] = useState<RuleConditionItem[]>([])
  const [characterOptions, setCharacterOptions] = useState<CharacterOptions | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [query, setQuery] = useState('')
  const [activeTab, setActiveTab] = useState<CatalogTabKey>('races')
  const [error, setError] = useState<string | null>(null)
  const [detailModal, setDetailModal] = useState<CatalogDetailModal | null>(null)

  useEffect(() => {
    let isCancelled = false

    async function load() {
      try {
        const [equipmentResponse, monstersResponse, spellsResponse, conditionsResponse, optionsResponse] = await Promise.all([
          getEquipmentCatalog(),
          getMonstersCatalog(),
          getRulesSpells(),
          getRulesConditions(),
          getCharacterOptions(),
        ])

        if (!isCancelled) {
          setEquipment(equipmentResponse)
          setMonsters(monstersResponse)
          setSpells(spellsResponse)
          setConditions(conditionsResponse)
          setCharacterOptions(optionsResponse)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить справочники.')
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false)
        }
      }
    }

    void load()
    return () => {
      isCancelled = true
    }
  }, [])

  const filtered = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()
    const matches = (text: string) => text.toLowerCase().includes(normalizedQuery)

    return {
      races: (characterOptions?.races ?? []).filter((race) => matches(`${race.parentRace} ${race.name} ${race.summary} ${race.description ?? ''}`)),
      classes: (characterOptions?.classes ?? []).filter((item) => matches(`${item.name} ${item.summary} ${item.description ?? ''}`)),
      backgrounds: (characterOptions?.backgrounds ?? []).filter((item) => matches(`${item.name} ${item.summary} ${item.description ?? ''}`)),
      spells: spells.filter((item) => matches(`${item.name} ${item.summary ?? ''} ${item.description ?? ''} ${(item.classSlugs ?? []).join(' ')}`)),
      equipment: equipment.filter((item) => matches(`${item.name} ${item.category ?? ''} ${item.subcategory ?? ''} ${item.damageType ?? ''}`)),
      monsters: monsters.filter((item) => matches(`${item.name} ${item.creatureType ?? ''} ${item.alignment ?? ''}`)),
      conditions: conditions.filter((item) => matches(`${item.name} ${item.description}`)),
    }
  }, [characterOptions, spells, equipment, monsters, conditions, query])

  const activeCount = filtered[activeTab].length

  return (
    <div className="stack">
      <section className="surface-card page-hero catalog-hero">
        <div>
          <p className="eyebrow">Справочник</p>
          <h2>Справочник PHB</h2>
          <p className="section-text">Структурированный каталог рас, классов, предысторий, заклинаний, снаряжения, существ и состояний.</p>
        </div>

        <div className="catalog-search-panel">
          <label>
            Поиск по активной вкладке
            <input className="app-search-input" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Например: рапира, дроу, маг..." />
          </label>
        </div>

        <div className="catalog-tabs">
          {catalogTabs.map((tab) => (
            <button
              key={tab.key}
              type="button"
              className={`button-reset catalog-tab ${activeTab === tab.key ? 'active' : ''}`}
              onClick={() => setActiveTab(tab.key)}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </section>

      {isLoading ? <section className="surface-card loading-state">Загрузка справочников...</section> : null}
      {error ? <section className="surface-card error-state">{error}</section> : null}

      {!isLoading && !error ? (
        <section className="surface-card catalog-surface-themed">
          <div className="catalog-result-bar">
            <span>{catalogTabs.find((tab) => tab.key === activeTab)?.label}</span>
            <strong>Найдено: {activeCount}</strong>
          </div>

          {activeTab === 'races' ? (
            <div className="catalog-grid">
              {filtered.races.map((race) => (
                <article key={race.id} className="catalog-card">
                  <h4>{race.name}</h4>
                  <p className="catalog-card__meta">{race.parentRace} • Скорость {race.speed}</p>
                  <p className="catalog-card__text">{race.summary}</p>
                  <div className="catalog-card__footer">
                    <button
                      type="button"
                      className="secondary-button button-reset"
                      onClick={() =>
                        setDetailModal({
                          title: race.name,
                          subtitle: race.parentRace,
                          blocks: [
                            { title: 'Описание', text: race.description ?? race.summary },
                            { title: 'Особенности', text: race.details.map((item) => `${item.title}: ${item.description}`).join('\n\n') || 'Нет данных' },
                            { title: 'Владения', text: race.grantedSkillProficiencies.length > 0 ? race.grantedSkillProficiencies.map((item) => translateSkill(item)).join(', ') : 'Нет' },
                          ],
                        })
                      }
                    >
                      Подробнее
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}

          {activeTab === 'classes' ? (
            <div className="catalog-grid">
              {filtered.classes.map((item) => (
                <article key={item.id} className="catalog-card">
                  <h4>{item.name}</h4>
                  <p className="catalog-card__meta">Кость хитов d{item.hitDie}</p>
                  <p className="catalog-card__text">{item.summary}</p>
                  <div className="catalog-card__footer">
                    <button
                      type="button"
                      className="secondary-button button-reset"
                      onClick={() =>
                        setDetailModal({
                          title: item.name,
                          blocks: [
                            { title: 'Описание', text: item.description ?? item.summary },
                            { title: 'Спасброски', text: item.savingThrowProficiencies.map((entry) => translateAbility(entry)).join(', ') || 'Нет данных' },
                            { title: 'Классовые особенности', text: item.details.map((feature) => `${feature.title}: ${feature.description}`).join('\n\n') || 'Нет данных' },
                          ],
                        })
                      }
                    >
                      Подробнее
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}

          {activeTab === 'backgrounds' ? (
            <div className="catalog-grid">
              {filtered.backgrounds.map((item) => (
                <article key={item.id} className="catalog-card">
                  <h4>{item.name}</h4>
                  <p className="catalog-card__meta">Навыки: {item.grantedSkillProficiencies.length}</p>
                  <p className="catalog-card__text">{item.summary}</p>
                  <div className="catalog-card__footer">
                    <button
                      type="button"
                      className="secondary-button button-reset"
                      onClick={() =>
                        setDetailModal({
                          title: item.name,
                          blocks: [
                            { title: 'Описание', text: item.description ?? item.summary },
                            { title: 'Навыки', text: item.grantedSkillProficiencies.map((entry) => translateSkill(entry)).join(', ') || 'Нет' },
                            { title: 'Особенности', text: item.details.map((feature) => `${feature.title}: ${feature.description}`).join('\n\n') || 'Нет данных' },
                          ],
                        })
                      }
                    >
                      Подробнее
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}

          {activeTab === 'spells' ? (
            <div className="catalog-grid">
              {filtered.spells.map((item) => (
                <article key={item.slug} className="catalog-card">
                  <h4>{item.name}</h4>
                  <p className="catalog-card__meta">Круг {item.spellLevel ?? 0} • Мин. уровень {item.minCharacterLevel ?? 1}</p>
                  <p className="catalog-card__text">{item.summary ?? 'Без краткого описания'}</p>
                  <div className="catalog-card__footer">
                    <button
                      type="button"
                      className="secondary-button button-reset"
                      onClick={() =>
                        setDetailModal({
                          title: item.name,
                          blocks: [
                            { title: 'Кратко', text: item.summary ?? 'Нет данных' },
                            { title: 'Описание', text: item.description ?? 'Нет данных' },
                            { title: 'Классы', text: (item.classSlugs ?? []).map((entry) => translateClassSlug(entry)).join(', ') || 'Нет данных' },
                          ],
                        })
                      }
                    >
                      Подробнее
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}

          {activeTab === 'equipment' ? (
            <div className="catalog-grid">
              {filtered.equipment.map((item) => (
                <article key={item.slug} className="catalog-card">
                  <h4>{item.name}</h4>
                  <p className="catalog-card__meta">
                    {translateToken(item.category)}
                    {item.subcategory ? ` / ${translateToken(item.subcategory)}` : ''}
                  </p>
                  <p className="catalog-card__text">
                    {item.costValue ? `${item.costValue} ${translateToken(item.costUnit)} • ` : ''}
                    {item.weightLb ? `${item.weightLb} фнт.` : 'Вес не указан'}
                  </p>
                  <div className="catalog-card__footer">
                    <button
                      type="button"
                      className="secondary-button button-reset"
                      onClick={() =>
                        setDetailModal({
                          title: item.name,
                          blocks: [
                            { title: 'Категория', text: `${translateToken(item.category)}${item.subcategory ? ` / ${translateToken(item.subcategory)}` : ''}` },
                            { title: 'Стоимость и вес', text: `${item.costValue ?? '-'} ${translateToken(item.costUnit)} • ${item.weightLb ?? '-'} фнт.` },
                            { title: 'Боевые параметры', text: item.damageDice ? `Урон: ${item.damageDice} (${translateToken(item.damageType)})` : 'Нет боевых параметров' },
                          ],
                        })
                      }
                    >
                      Подробнее
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}

          {activeTab === 'monsters' ? (
            <div className="catalog-grid">
              {filtered.monsters.map((item) => (
                <article key={item.slug} className="catalog-card">
                  <h4>{item.name}</h4>
                  <p className="catalog-card__meta">{translateToken(item.size)} {translateToken(item.creatureType)} • CR {item.challengeRating ?? 0}</p>
                  <p className="catalog-card__text">КД {item.armorClass ?? '-'} • ХП {item.hitPoints ?? '-'} • Скорость {item.speed ?? '-'}</p>
                  <div className="catalog-card__footer">
                    <button
                      type="button"
                      className="secondary-button button-reset"
                      onClick={() =>
                        setDetailModal({
                          title: item.name,
                          blocks: [
                            { title: 'Тип', text: `${translateToken(item.size)} ${translateToken(item.creatureType)} • ${translateToken(item.alignment)}` },
                            { title: 'Характеристики боя', text: `КД ${item.armorClass ?? '-'} • ХП ${item.hitPoints ?? '-'} • Кости хитов ${item.hitDice ?? '-'} • Скорость ${item.speed ?? '-'}` },
                          ],
                        })
                      }
                    >
                      Подробнее
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}

          {activeTab === 'conditions' ? (
            <div className="catalog-grid">
              {filtered.conditions.map((item) => (
                <article key={item.slug} className="catalog-card">
                  <h4>{item.name}</h4>
                  <p className="catalog-card__text">{item.description}</p>
                  <div className="catalog-card__footer">
                    <button
                      type="button"
                      className="secondary-button button-reset"
                      onClick={() =>
                        setDetailModal({
                          title: item.name,
                          blocks: [{ title: 'Описание состояния', text: item.description }],
                        })
                      }
                    >
                      Подробнее
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}
        </section>
      ) : null}

      {detailModal ? (
        <div className="modal-overlay" role="presentation" onClick={() => setDetailModal(null)}>
          <div className="modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3>{detailModal.title}</h3>
                {detailModal.subtitle ? <p className="section-text">{detailModal.subtitle}</p> : null}
              </div>
              <button type="button" className="secondary-button button-reset" onClick={() => setDetailModal(null)}>
                Закрыть
              </button>
            </div>
            <div className="stack">
              {detailModal.blocks.map((block, index) => (
                <article key={`${block.title}-${index}`} className="surface-card modal-detail">
                  <h4>{block.title}</h4>
                  <p className="multiline-text">{block.text}</p>
                </article>
              ))}
            </div>
          </div>
        </div>
      ) : null}
    </div>
  )
}
