import { useEffect, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { useAuth } from '../components/AuthProvider'
import {
  addRoomMonster,
  applyRoomMonsterDamage,
  getMonstersCatalog,
  getMyCharacters,
  getRoomById,
  getRoomMonsters,
  rollRoomMonsterDamage,
  selectRoomCharacter,
  updateRoomMemberRole,
} from '../services/charactersApi'
import type { ApiValidationError, CharacterSummary, MonsterCatalogItem, Room, RoomMonster } from '../types/character'
import { getCharacterPortrait } from '../utils/characterPresentation'

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
  const [selectedMonsterSlug, setSelectedMonsterSlug] = useState('')
  const [monsterDamageInputs, setMonsterDamageInputs] = useState<Record<string, string>>({})
  const [monsterRollLog, setMonsterRollLog] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
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
          setSelectedMonsterSlug(monsterCatalogResponse[0]?.slug ?? '')
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

  async function handleCharacterToggle(characterId: string, isSelected: boolean) {
    if (!room || !user) return
    setIsSaving(true)
    setError(null)
    try {
      if (!isSelected) {
        const updated = await selectRoomCharacter(id, { characterId })
        setRoom(updated)
      }
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось обновить персонажей в комнате.')
    } finally {
      setIsSaving(false)
    }
  }

  async function clearCharacterSelection() {
    setIsSaving(true)
    setError(null)
    try {
      const updated = await selectRoomCharacter(id, { characterId: null })
      setRoom(updated)
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось очистить выбор персонажей.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleRoleChange(memberUserId: string, role: string) {
    setIsSaving(true)
    setError(null)
    try {
      const updatedRoom = await updateRoomMemberRole(id, memberUserId, { role })
      setRoom(updatedRoom)
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось обновить роль участника.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleAddMonster() {
    if (!room || !selectedMonsterSlug) return
    setIsSaving(true)
    setError(null)
    try {
      const addedMonster = await addRoomMonster(room.id, selectedMonsterSlug)
      setRoomMonsters((current) => [...current, addedMonster])
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось добавить чудовище.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleApplyMonsterDamage(monsterId: string) {
    const raw = monsterDamageInputs[monsterId] ?? ''
    const value = Number(raw)
    if (!Number.isFinite(value) || value <= 0) {
      setError('Укажи корректное количество полученного урона (больше 0).')
      return
    }

    setIsSaving(true)
    setError(null)
    try {
      const updatedMonster = await applyRoomMonsterDamage(id, monsterId, value)
      setRoomMonsters((current) =>
        current.map((monster) => (monster.id === updatedMonster.id ? updatedMonster : monster)),
      )
      setMonsterDamageInputs((current) => ({ ...current, [monsterId]: '' }))
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось применить урон по чудовищу.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleRollMonsterDamage(monsterId: string) {
    setIsSaving(true)
    setError(null)
    try {
      const roll = await rollRoomMonsterDamage(id, monsterId)
      const modifierText = roll.damageBonus > 0 ? `+ ${roll.damageBonus}` : roll.damageBonus < 0 ? `- ${Math.abs(roll.damageBonus)}` : '+ 0'
      setMonsterRollLog(`${roll.monsterName}: ${roll.damageExpression} ${roll.diceResult} ${modifierText} = ${roll.totalDamage}`)
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось выполнить бросок урона чудовища.')
    } finally {
      setIsSaving(false)
    }
  }

  const currentMember = room?.members.find((member) => member.userId === user?.id) ?? null
  const selectedCharacterIds = new Set(currentMember?.characters.map((item) => item.id) ?? [])

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
                              <p className="muted">Просмотр доступен только в режиме чтения.</p>
                              <Link to={`/characters/${character.id}?view=room`} className="text-link">
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
                <p className="muted">Справочник чудовищ взят из PHB-сида проекта. Ведущий управляет уроном и бросками.</p>
              </div>
            </div>

            {room.canManageMembers ? (
              <div className="room-monster-add">
                <label className="full-span">
                  Добавить чудовище
                  <select
                    className="app-select"
                    value={selectedMonsterSlug}
                    onChange={(event) => setSelectedMonsterSlug(event.target.value)}
                    disabled={isSaving || monsterCatalog.length === 0}
                  >
                    {monsterCatalog
                      .slice()
                      .sort((left, right) => left.name.localeCompare(right.name, 'ru-RU'))
                      .map((monster) => (
                        <option key={monster.slug} value={monster.slug}>
                          {monster.name} • CR {monster.challengeRating ?? 0}
                        </option>
                      ))}
                  </select>
                </label>
                <button type="button" className="primary-button button-reset" onClick={() => void handleAddMonster()} disabled={isSaving || !selectedMonsterSlug}>
                  Добавить в комнату
                </button>
              </div>
            ) : null}

            {monsterRollLog ? <p className="success-text">{monsterRollLog}</p> : null}

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
    </div>
  )
}
