import { useEffect, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { useAuth } from '../components/AuthProvider'
import {
  addRoomMonster,
  attackRoomCharacterByMonster,
  applyRoomMonsterDamage,
  getMonstersCatalog,
  getMyCharacters,
  getRoomById,
  getRoomMonsters,
  removeRoomMonster,
  rollRoomMonsterDamage,
  selectRoomCharacter,
  updateRoomMemberRole,
} from '../services/charactersApi'
import type { ApiValidationError, CharacterSummary, MonsterCatalogItem, Room, RoomMonster } from '../types/character'
import { getCharacterPortrait } from '../utils/characterPresentation'

type RoomNoticeKind = 'success' | 'error' | 'info'

type RoomNotice = {
  id: number
  kind: RoomNoticeKind
  text: string
}

function getRoomRoleLabel(role: string) {
  return role === 'GameMaster' ? 'Ведущий' : 'Игрок'
}

function translateDamageType(value?: string) {
  const normalized = (value ?? '').trim().toLowerCase()
  const map: Record<string, string> = {
    slashing: 'рубящий',
    piercing: 'колющий',
    bludgeoning: 'дробящий',
    fire: 'огненный',
    cold: 'холод',
    poison: 'яд',
  }
  return map[normalized] ?? value ?? ''
}

export function RoomDetailPage() {
  const { id = '' } = useParams()
  const { user, isLoading: isAuthLoading } = useAuth()
  const [room, setRoom] = useState<Room | null>(null)
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [roomMonsters, setRoomMonsters] = useState<RoomMonster[]>([])
  const [monsterCatalog, setMonsterCatalog] = useState<MonsterCatalogItem[]>([])
  const [isMonsterPickerOpen, setIsMonsterPickerOpen] = useState(false)
  const [monsterSearch, setMonsterSearch] = useState('')
  const [monsterDamageInputs, setMonsterDamageInputs] = useState<Record<string, string>>({})
  const [monsterTargets, setMonsterTargets] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)
  const [notifications, setNotifications] = useState<RoomNotice[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    if (!user) {
      setIsLoading(false)
      return
    }

    let isCancelled = false
    async function loadRoom() {
      try {
        const [roomResponse, charactersResponse, monstersResponse, monsterCatalogResponse] = await Promise.all([
          getRoomById(id),
          getMyCharacters(),
          getRoomMonsters(id),
          getMonstersCatalog(),
        ])
        if (!isCancelled) {
          setRoom(roomResponse)
          setCharacters(charactersResponse)
          setRoomMonsters(monstersResponse)
          setMonsterCatalog(monsterCatalogResponse)
          setError(null)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить комнату.')
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false)
        }
      }
    }
    void loadRoom()
    return () => {
      isCancelled = true
    }
  }, [id, user])

  if (!isAuthLoading && !user) {
    return <Navigate to="/login" replace state={{ from: `/rooms/${id}` }} />
  }

  function pushNotice(text: string, kind: RoomNoticeKind = 'info') {
    const id = Date.now() + Math.floor(Math.random() * 100_000)
    setNotifications((current) => [...current, { id, kind, text }])
    window.setTimeout(() => {
      setNotifications((current) => current.filter((item) => item.id !== id))
    }, 4800)
  }

  function getApiErrorMessage(caughtError: unknown, fallback: string) {
    const apiError = caughtError as ApiValidationError
    const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
    return firstFieldError ?? apiError.message ?? fallback
  }

  async function handleCharacterToggle(characterId: string, isSelected: boolean) {
    if (!room || !user) return
    setIsSaving(true)
    try {
      if (!isSelected) {
        const updated = await selectRoomCharacter(id, { characterId })
        setRoom(updated)
      }
    } catch (caughtError) {
      pushNotice(getApiErrorMessage(caughtError, 'Не удалось обновить персонажей в комнате.'), 'error')
    } finally {
      setIsSaving(false)
    }
  }

  async function clearCharacterSelection() {
    setIsSaving(true)
    try {
      const updated = await selectRoomCharacter(id, { characterId: null })
      setRoom(updated)
    } catch (caughtError) {
      pushNotice(getApiErrorMessage(caughtError, 'Не удалось очистить выбор персонажей.'), 'error')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleRoleChange(memberUserId: string, role: string) {
    setIsSaving(true)
    try {
      const updatedRoom = await updateRoomMemberRole(id, memberUserId, { role })
      setRoom(updatedRoom)
    } catch (caughtError) {
      pushNotice(getApiErrorMessage(caughtError, 'Не удалось обновить роль участника.'), 'error')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleAddMonster(monsterSlug: string) {
    if (!room || !monsterSlug) return
    setIsSaving(true)
    try {
      const addedMonster = await addRoomMonster(room.id, monsterSlug)
      setRoomMonsters((current) => [...current, addedMonster])
      pushNotice(`Добавлено чудовище: ${addedMonster.name}.`, 'success')
      setIsMonsterPickerOpen(false)
      setMonsterSearch('')
    } catch (caughtError) {
      pushNotice(getApiErrorMessage(caughtError, 'Не удалось добавить чудовище.'), 'error')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleApplyMonsterDamage(monsterId: string) {
    const raw = monsterDamageInputs[monsterId] ?? ''
    const value = Number(raw)
    if (!Number.isFinite(value) || value <= 0) {
      pushNotice('Укажи корректное количество полученного урона (больше 0).', 'error')
      return
    }

    setIsSaving(true)
    try {
      const result = await applyRoomMonsterDamage(id, monsterId, value)
      if (result.removed) {
        setRoomMonsters((current) => current.filter((monster) => monster.id !== result.monsterId))
        setMonsterTargets((current) => {
          const { [monsterId]: _, ...next } = current
          return next
        })
        pushNotice(`${result.monsterName} получает ${value} урона и выбывает из комнаты.`, 'success')
      } else if (result.monster) {
        setRoomMonsters((current) =>
          current.map((monster) => (monster.id === result.monster!.id ? result.monster! : monster)),
        )
        pushNotice(
          `${result.monster.name} получает ${value} урона. ХП: ${result.monster.currentHitPoints}/${result.monster.maxHitPoints}.`,
          'success',
        )
      }
      setMonsterDamageInputs((current) => ({ ...current, [monsterId]: '' }))
    } catch (caughtError) {
      pushNotice(getApiErrorMessage(caughtError, 'Не удалось применить урон по чудовищу.'), 'error')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleRollMonsterDamage(monsterId: string) {
    setIsSaving(true)
    try {
      const roll = await rollRoomMonsterDamage(id, monsterId)
      pushNotice(`${roll.monsterName}: ${roll.damageExpression} + ${roll.damageBonus} = ${roll.totalDamage} урона.`, 'success')
    } catch (caughtError) {
      pushNotice(getApiErrorMessage(caughtError, 'Не удалось выполнить бросок урона чудовища.'), 'error')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleRemoveMonster(monsterId: string) {
    setIsSaving(true)
    try {
      const target = roomMonsters.find((monster) => monster.id === monsterId)
      await removeRoomMonster(id, monsterId)
      setRoomMonsters((current) => current.filter((monster) => monster.id !== monsterId))
      setMonsterTargets((current) => {
        const { [monsterId]: _, ...next } = current
        return next
      })
      pushNotice(`Чудовище ${target?.name ?? ''} удалено из комнаты.`, 'success')
    } catch (caughtError) {
      pushNotice(getApiErrorMessage(caughtError, 'Не удалось удалить чудовище.'), 'error')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleMonsterAttack(monsterId: string) {
    const targetCharacterId = monsterTargets[monsterId]
    if (!targetCharacterId) {
      pushNotice('Выбери цель атаки для чудовища.', 'error')
      return
    }

    setIsSaving(true)
    try {
      const result = await attackRoomCharacterByMonster(id, monsterId, targetCharacterId)
      setRoom((current) => {
        if (!current) {
          return current
        }

        return {
          ...current,
          members: current.members.map((member) => ({
            ...member,
            characters: member.characters.map((character) =>
              character.id === result.targetCharacterId
                ? {
                  ...character,
                  currentHitPoints: result.targetCurrentHitPoints,
                  maxHitPoints: result.targetMaxHitPoints,
                }
                : character),
          })),
        }
      })

      const attackLine = `Атака: d20(${result.attackRoll}) + ${result.attackBonus} = ${result.attackTotal} против КД ${result.targetArmorClass}.`
      const damageLine = result.isHit
        ? `Урон: ${result.damageExpression} + ${result.damageBonus} = ${result.damageTotal}.`
        : 'Попадания нет, урон не проходит.'
      pushNotice(`${result.message} ${attackLine} ${damageLine}`, result.isHit ? 'success' : 'info')
    } catch (caughtError) {
      pushNotice(getApiErrorMessage(caughtError, 'Не удалось выполнить атаку чудовищем.'), 'error')
    } finally {
      setIsSaving(false)
    }
  }

  const currentMember = room?.members.find((member) => member.userId === user?.id) ?? null
  const selectedCharacterIds = new Set(currentMember?.characters.map((item) => item.id) ?? [])
  const filteredMonstersCatalog = monsterCatalog
    .filter((monster) => `${monster.name} ${monster.creatureType ?? ''} ${monster.alignment ?? ''}`.toLowerCase().includes(monsterSearch.toLowerCase()))
    .sort((left, right) => left.name.localeCompare(right.name, 'ru-RU'))
  const roomCharacterTargets = room
    ? room.members.flatMap((member) => member.characters).filter((character, index, list) => list.findIndex((item) => item.id === character.id) === index)
    : []

  return (
    <div className="stack">
      {isLoading ? <section className="surface-card loading-state">Загрузка комнаты...</section> : null}
      {error ? <section className="surface-card error-state">{error}</section> : null}

      {!isLoading && room ? (
        <>
          <section className="surface-card room-hero">
            <div className="stack room-hero__content">
              <h2>{room.name}</h2>
              <p className="section-text">Код подключения: {room.joinCode}</p>
              <p className="muted">Ведущий: {room.ownerDisplayName} • Твоя роль: {getRoomRoleLabel(room.currentUserRole)}</p>
              <div className="badge-cluster">
                <span className="pill">Участников: {room.members.length}</span>
                <span className="pill">В сети: {room.connectedMembers}</span>
              </div>
            </div>
            <div className="room-hero__actions">
              <Link to="/rooms" className="text-link">Ко всем комнатам</Link>
            </div>
          </section>

          <section className="surface-card">
            <h3>Мои персонажи в комнате</h3>
            <div className="skill-pick-grid">
              {characters.map((character) => {
                const isSelected = selectedCharacterIds.has(character.id)
                return (
                  <button
                    key={character.id}
                    type="button"
                    className={`skill-toggle ${isSelected ? 'selected' : ''}`}
                    onClick={() => void handleCharacterToggle(character.id, isSelected)}
                    disabled={isSaving}
                  >
                    {character.name} • {character.race} • {character.className}
                  </button>
                )
              })}
              <button type="button" className="secondary-button button-reset" onClick={() => void clearCharacterSelection()} disabled={isSaving}>
                Очистить выбор
              </button>
            </div>
          </section>

          <section className="surface-card">
            <h3>Участники комнаты</h3>
            <div className="stack">
              {room.members.map((member) => (
                <div key={member.userId} className="room-member-row room-member-row--detailed">
                  <div className="room-member-row__main">
                    <strong>{member.displayName}</strong>
                    <div className="badge-cluster">
                      <span className="pill">{member.isOwner ? 'Владелец' : getRoomRoleLabel(member.role)}</span>
                    </div>
                    {member.characters.length > 0 ? (
                      <div className="room-character-grid">
                        {member.characters.map((character) => (
                          <article key={character.id} className="character-card room-character-card">
                            <img
                              className="character-card__portrait"
                              src={getCharacterPortrait(character.name, character.race, character.className)}
                              alt={`Портрет персонажа ${character.name}`}
                            />
                            <div className="character-card__body">
                              <div className="character-card__header">
                                <div>
                                  <h3>{character.name}</h3>
                                  <p>{character.race} • {character.className}</p>
                                </div>
                                <span className="pill">Ур. {character.level}</span>
                              </div>
                              <p className="muted">
                                {member.userId === user?.id
                                  ? 'Твой персонаж: доступно редактирование.'
                                  : 'Персонаж другого игрока: только просмотр.'}
                              </p>
                              <Link
                                to={member.userId === user?.id ? `/characters/${character.id}` : `/characters/${character.id}?view=room`}
                                className="text-link"
                              >
                                Открыть лист
                              </Link>
                            </div>
                          </article>
                        ))}
                      </div>
                    ) : (
                      <p className="muted">Персонажи не выбраны.</p>
                    )}
                  </div>
                  {room.canManageMembers && !member.isOwner ? (
                    <label className="room-role-editor">
                      Роль
                      <select className="app-select" value={member.role} onChange={(event) => void handleRoleChange(member.userId, event.target.value)} disabled={isSaving}>
                        <option value="Player">Игрок</option>
                        <option value="GameMaster">Ведущий</option>
                      </select>
                    </label>
                  ) : null}
                </div>
              ))}
            </div>
          </section>

          <section className="surface-card">
            <div className="section-header-row">
              <div>
                <h3>Чудовища комнаты</h3>
              </div>
            </div>

            {room.canManageMembers ? (
              <div className="room-monster-add">
                <button
                  type="button"
                  className="primary-button button-reset"
                  onClick={() => setIsMonsterPickerOpen(true)}
                  disabled={isSaving || monsterCatalog.length === 0}
                >
                  Добавить чудовище
                </button>
              </div>
            ) : null}

            <div className="room-monster-grid">
              {roomMonsters.length > 0 ? (
                roomMonsters.map((monster) => (
                  <article key={monster.id} className="surface-card room-monster-card">
                    <div className="section-header-row">
                      <h4>{monster.name}</h4>
                      <span className="pill">CR {monster.challengeRating}</span>
                    </div>
                    <p className="muted">КД {monster.armorClass} • ХП {monster.currentHitPoints}/{monster.maxHitPoints}</p>
                    <p className="muted">
                      {monster.attackName}: +{monster.attackBonus} к попаданию • урон {monster.damageDice}
                      {monster.damageBonus >= 0 ? ` + ${monster.damageBonus}` : ` - ${Math.abs(monster.damageBonus)}`} ({translateDamageType(monster.damageType)})
                    </p>
                    {room.canManageMembers ? (
                      <div className="room-monster-actions">
                        <button type="button" className="secondary-button button-reset" onClick={() => void handleRollMonsterDamage(monster.id)} disabled={isSaving}>
                          Бросок урона
                        </button>
                        <label>
                          Цель атаки
                          <select
                            className="app-select"
                            value={monsterTargets[monster.id] ?? ''}
                            onChange={(event) => setMonsterTargets((current) => ({ ...current, [monster.id]: event.target.value }))}
                            disabled={isSaving || roomCharacterTargets.length === 0}
                          >
                            <option value="">Выбери персонажа</option>
                            {roomCharacterTargets.map((target) => (
                              <option key={target.id} value={target.id}>
                                {target.name} • КД {target.armorClass} • ХП {target.currentHitPoints}/{target.maxHitPoints}
                              </option>
                            ))}
                          </select>
                        </label>
                        <button type="button" className="secondary-button button-reset" onClick={() => void handleMonsterAttack(monster.id)} disabled={isSaving}>
                          Атаковать персонажа
                        </button>
                        <label>
                          Полученный урон
                          <input
                            type="number"
                            min={1}
                            value={monsterDamageInputs[monster.id] ?? ''}
                            onChange={(event) => setMonsterDamageInputs((current) => ({ ...current, [monster.id]: event.target.value }))}
                            className="app-search-input"
                            placeholder="Например: 7"
                          />
                        </label>
                        <button type="button" className="primary-button button-reset" onClick={() => void handleApplyMonsterDamage(monster.id)} disabled={isSaving}>
                          Применить урон
                        </button>
                        <button type="button" className="secondary-button button-reset" onClick={() => void handleRemoveMonster(monster.id)} disabled={isSaving}>
                          Удалить чудовище
                        </button>
                      </div>
                    ) : (
                      <p className="muted">Только ведущий может управлять бросками и уроном чудовищ.</p>
                    )}
                  </article>
                ))
              ) : (
                <p className="muted">Пока нет добавленных чудовищ.</p>
              )}
            </div>
          </section>
        </>
      ) : null}

      {isMonsterPickerOpen ? (
        <div className="modal-overlay" role="presentation" onClick={() => setIsMonsterPickerOpen(false)}>
          <div className="modal-card room-monster-picker-modal" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <div>
                <h3>Добавить чудовище в комнату</h3>
                <p className="muted">Выбери существо из справочника PHB.</p>
              </div>
              <button type="button" className="info-icon-button button-reset" onClick={() => setIsMonsterPickerOpen(false)} aria-label="Закрыть окно">
                ×
              </button>
            </div>
            <input
              className="app-search-input"
              type="search"
              value={monsterSearch}
              onChange={(event) => setMonsterSearch(event.target.value)}
              placeholder="Поиск по названию, типу или мировоззрению..."
            />
            <div className="room-monster-picker-grid">
              {filteredMonstersCatalog.map((monster) => (
                <button
                  key={monster.slug}
                  type="button"
                  className="room-monster-picker-card button-reset"
                  onClick={() => void handleAddMonster(monster.slug)}
                  disabled={isSaving}
                >
                  <div className="section-header-row">
                    <strong>{monster.name}</strong>
                    <span className="pill">CR {monster.challengeRating ?? 0}</span>
                  </div>
                  <p className="muted">{monster.size} • {monster.creatureType} • {monster.alignment}</p>
                  <p className="muted">КД {monster.armorClass} • ХП {monster.hitPoints}</p>
                  <p className="muted">
                    {monster.attackName}: +{monster.attackBonus} • {monster.damageDice}
                    {(monster.damageBonus ?? 0) >= 0
                      ? ` + ${monster.damageBonus ?? 0}`
                      : ` - ${Math.abs(monster.damageBonus ?? 0)}`} ({translateDamageType(monster.damageType)})
                  </p>
                </button>
              ))}
            </div>
          </div>
        </div>
      ) : null}

      {notifications.length > 0 ? (
        <aside className="room-toast-stack">
          {notifications.map((notice) => (
            <article key={notice.id} className={`room-toast room-toast--${notice.kind}`}>
              {notice.text}
            </article>
          ))}
        </aside>
      ) : null}
    </div>
  )
}
