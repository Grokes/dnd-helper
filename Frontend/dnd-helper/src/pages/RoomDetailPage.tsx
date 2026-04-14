import { useEffect, useMemo, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { useAuth } from '../components/AuthProvider'
import {
  getMyCharacters,
  getRoomById,
  selectRoomCharacter,
  updateRoomMemberRole,
  updateRoomPresence,
  updateRoomSession,
} from '../services/charactersApi'
import type { ApiValidationError, CharacterSummary, Room } from '../types/character'

function getRoomRoleLabel(role: string) {
  return role === 'GameMaster' ? 'Ведущий' : 'Игрок'
}

export function RoomDetailPage() {
  const { id = '' } = useParams()
  const { user, isLoading: isAuthLoading } = useAuth()
  const [room, setRoom] = useState<Room | null>(null)
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [error, setError] = useState<string | null>(null)
  const [copiedMessage, setCopiedMessage] = useState<string | null>(null)
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
        const [roomResponse, charactersResponse] = await Promise.all([getRoomById(id), getMyCharacters()])

        if (!isCancelled) {
          setRoom(roomResponse)
          setCharacters(charactersResponse)
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

  useEffect(() => {
    if (!user || !id) {
      return
    }

    let isCancelled = false

    async function sendPresence() {
      try {
        await updateRoomPresence(id)
      } catch {
        if (!isCancelled) {
          setError((current) => current ?? 'Не удалось обновить присутствие в комнате.')
        }
      }
    }

    void sendPresence()
    const intervalId = window.setInterval(() => {
      void sendPresence()
    }, 30000)

    return () => {
      isCancelled = true
      window.clearInterval(intervalId)
    }
  }, [id, user])

  useEffect(() => {
    if (!copiedMessage) {
      return
    }

    const timeoutId = window.setTimeout(() => setCopiedMessage(null), 2500)
    return () => window.clearTimeout(timeoutId)
  }, [copiedMessage])

  if (!isAuthLoading && !user) {
    return <Navigate to="/login" replace state={{ from: `/rooms/${id}` }} />
  }

  async function handleCharacterSelect(characterId: string | null) {
    setIsSaving(true)
    setError(null)

    try {
      const updatedRoom = await selectRoomCharacter(id, { characterId })
      setRoom(updatedRoom)
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось обновить персонажа в комнате.')
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

  async function handleActiveMemberChange(activeMemberUserId: string | null) {
    setIsSaving(true)
    setError(null)

    try {
      const updatedRoom = await updateRoomSession(id, { activeMemberUserId })
      setRoom(updatedRoom)
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось обновить состояние сессии.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleCopyInviteLink() {
    if (!room) {
      return
    }

    const inviteLink = `${window.location.origin}/rooms/invite/${room.inviteToken}`
    try {
      await navigator.clipboard.writeText(inviteLink)
      setCopiedMessage('Ссылка приглашения скопирована.')
    } catch {
      setCopiedMessage(inviteLink)
    }
  }

  const currentMember = room?.members.find((member) => member.userId === user?.id) ?? null
  const activeMembers = useMemo(() => room?.members ?? [], [room])

  return (
    <div className="stack">
      {isLoading ? <section className="surface-card loading-state">Загрузка комнаты...</section> : null}
      {error ? <section className="surface-card error-state">{error}</section> : null}

      {!isLoading && room ? (
        <>
          <section className="surface-card room-hero">
            <div className="stack room-hero__content">
              <div>
                <h2>{room.name}</h2>
                <p className="section-text">Код подключения: {room.joinCode}</p>
                <p className="muted">
                  Ведущий: {room.ownerDisplayName} • Твоя роль: {getRoomRoleLabel(room.currentUserRole)}
                </p>
              </div>

              <div className="badge-cluster">
                <span className="pill">Участников: {room.members.length}</span>
                <span className="pill">В сети: {room.session.connectedMembers}</span>
                <span className="pill">
                  Активен: {room.session.activeMemberDisplayName ?? 'не выбран'}
                  {room.session.activeCharacterName ? ` • ${room.session.activeCharacterName}` : ''}
                </span>
              </div>
            </div>

            <div className="room-hero__actions">
              <button type="button" className="secondary-button button-reset" onClick={() => void handleCopyInviteLink()}>
                Скопировать ссылку-приглашение
              </button>
              <Link to="/rooms" className="text-link">
                Ко всем комнатам
              </Link>
            </div>
          </section>

          {copiedMessage ? <section className="surface-card success-text">{copiedMessage}</section> : null}

          <section className="grid two-columns">
            <article className="surface-card">
              <h3>Состояние сессии</h3>
              <div className="stack">
                <p className="muted">
                  Сейчас активен:{' '}
                  {room.session.activeMemberDisplayName
                    ? `${room.session.activeMemberDisplayName}${room.session.activeCharacterName ? ` • ${room.session.activeCharacterName}` : ''}`
                    : 'никто не выбран'}
                </p>
                <p className="muted">
                  Последнее обновление:{' '}
                  {room.session.updatedAtUtc ? new Date(room.session.updatedAtUtc).toLocaleString('ru-RU') : 'ещё не было'}
                </p>

                {room.canManageSession ? (
                  <label className="full-span">
                    Активный участник
                    <select
                      value={room.session.activeMemberUserId ?? ''}
                      onChange={(event) => void handleActiveMemberChange(event.target.value || null)}
                      disabled={isSaving}
                    >
                      <option value="">Не выбран</option>
                      {activeMembers.map((member) => (
                        <option key={member.userId} value={member.userId}>
                          {member.displayName}
                          {member.character ? ` • ${member.character.name}` : ''}
                        </option>
                      ))}
                    </select>
                  </label>
                ) : (
                  <p className="muted">Назначать активного участника может только ведущий комнаты.</p>
                )}
              </div>
            </article>

            <article className="surface-card">
              <h3>Мой персонаж в комнате</h3>
              <p className="muted">Можно выбрать только одного из своих персонажей. Чужие листы остаются недоступны.</p>
              <div className="form-grid compact">
                <label className="full-span">
                  Выбери персонажа
                  <select
                    value={currentMember?.character?.id ?? ''}
                    onChange={(event) => void handleCharacterSelect(event.target.value || null)}
                    disabled={isSaving}
                  >
                    <option value="">Без персонажа</option>
                    {characters.map((character) => (
                      <option key={character.id} value={character.id}>
                        {character.name} • {character.race} • {character.className}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            </article>
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
                      <span className={`presence-pill ${member.isOnline ? 'presence-pill--online' : ''}`}>
                        {member.isOnline ? 'В сети' : 'Не в сети'}
                      </span>
                    </div>
                    <p className="muted">В комнате с {new Date(member.joinedAtUtc).toLocaleString('ru-RU')}</p>
                    <p className="muted">
                      {member.character ? (
                        <>
                          <Link to={`/characters/${member.character.id}`} className="text-link">
                            {member.character.name}
                          </Link>{' '}
                          • {member.character.race} • {member.character.className} • Ур. {member.character.level}
                        </>
                      ) : 'Персонаж ещё не выбран'}
                    </p>
                  </div>

                  {room.canManageMembers && !member.isOwner ? (
                    <label className="room-role-editor">
                      Роль
                      <select
                        value={member.role}
                        onChange={(event) => void handleRoleChange(member.userId, event.target.value)}
                        disabled={isSaving}
                      >
                        <option value="Player">Игрок</option>
                        <option value="GameMaster">Ведущий</option>
                      </select>
                    </label>
                  ) : null}
                </div>
              ))}
            </div>
          </section>
        </>
      ) : null}
    </div>
  )
}
