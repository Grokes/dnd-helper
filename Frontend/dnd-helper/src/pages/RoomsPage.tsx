import { useEffect, useMemo, useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../features/auth/model/AuthProvider'
import { createRoom, getRooms, joinRoom } from '../features/rooms/api/roomsApi'
import type { ApiValidationError, RoomSummary } from '../types/character'

function getRoomRoleLabel(role: string) {
  return role === 'GameMaster' ? 'Ведущий' : 'Игрок'
}

export function RoomsPage() {
  const navigate = useNavigate()
  const { user, isLoading: isAuthLoading } = useAuth()
  const [rooms, setRooms] = useState<RoomSummary[]>([])
  const [roomName, setRoomName] = useState('')
  const [joinCode, setJoinCode] = useState('')
  const [mode, setMode] = useState<'create' | 'join'>('create')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (!user) {
      setIsLoading(false)
      return
    }

    let isCancelled = false

    async function loadRooms() {
      try {
        const response = await getRooms()
        if (!isCancelled) {
          setRooms(response)
          setError(null)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить комнаты.')
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadRooms()

    return () => {
      isCancelled = true
    }
  }, [user])

  const sortedRooms = useMemo(
    () =>
      [...rooms].sort((left, right) => {
        const leftPriority = left.currentUserRole === 'GameMaster' ? 0 : 1
        const rightPriority = right.currentUserRole === 'GameMaster' ? 0 : 1
        if (leftPriority !== rightPriority) {
          return leftPriority - rightPriority
        }

        return left.name.localeCompare(right.name, 'ru')
      }),
    [rooms],
  )

  if (!isAuthLoading && !user) {
    return <Navigate to="/login" replace state={{ from: '/rooms' }} />
  }

  async function handleCreateRoom(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      const room = await createRoom({ name: roomName })
      navigate(`/rooms/${room.id}`)
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось создать комнату.')
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleJoinRoom(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    try {
      const room = await joinRoom({ joinCode })
      navigate(`/rooms/${room.id}`)
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось подключиться к комнате.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="stack">
      <section className="surface-card page-hero">
        <div>
          <p className="eyebrow">Комнаты</p>
          <h2>Игровые комнаты</h2>
          <p className="section-text">
            Создавай игровые столы, подключай игроков по коду и переходи к управлению сценой без лишних блоков на экране.
          </p>
        </div>
      </section>

      <section className="surface-card room-action-panel">
        <div className="skill-pick-grid">
          <button type="button" className={`skill-toggle ${mode === 'create' ? 'selected' : ''}`} onClick={() => setMode('create')}>
            Создать комнату
          </button>
          <button type="button" className={`skill-toggle ${mode === 'join' ? 'selected' : ''}`} onClick={() => setMode('join')}>
            Подключиться
          </button>
        </div>
        {mode === 'create' ? (
          <form className="form-grid compact" onSubmit={handleCreateRoom}>
            <label className="full-span">
              Название комнаты
              <input value={roomName} onChange={(event) => setRoomName(event.target.value)} required />
            </label>
            <button type="submit" className="primary-button button-reset" disabled={isSubmitting}>Создать</button>
          </form>
        ) : (
          <form className="form-grid compact" onSubmit={handleJoinRoom}>
            <label className="full-span">
              Код комнаты
              <input value={joinCode} onChange={(event) => setJoinCode(event.target.value.toUpperCase())} required />
            </label>
            <button type="submit" className="secondary-button button-reset" disabled={isSubmitting}>Подключиться</button>
          </form>
        )}
      </section>

      {error ? <section className="surface-card error-state">{error}</section> : null}
      {isLoading ? <section className="surface-card loading-state">Загрузка комнат...</section> : null}

      {!isLoading ? (
        <section className="surface-card">
          <div className="section-header-row">
            <div>
              <p className="eyebrow">Список</p>
              <h2>Мои комнаты</h2>
            </div>
          </div>

          {sortedRooms.length > 0 ? (
            <div className="grid two-columns">
              {sortedRooms.map((room) => (
                <article key={room.id} className="surface-card room-card">
                  <div className="room-card__header">
                    <div>
                      <h3>{room.name}</h3>
                      <p className="muted">Ведущий: {room.ownerDisplayName}</p>
                    </div>
                    <span className="pill">{getRoomRoleLabel(room.currentUserRole)}</span>
                  </div>

                  <div className="room-meta-list">
                    <div className="room-stat-grid">
                      <div className="room-stat">
                        <span>Код</span>
                        <strong>{room.joinCode}</strong>
                      </div>
                      <div className="room-stat">
                        <span>Участники</span>
                        <strong>{room.memberCount}</strong>
                      </div>
                      <div className="room-stat">
                        <span>В сети</span>
                        <strong>{room.connectedMemberCount}</strong>
                      </div>
                    </div>
                  </div>

                  <Link to={`/rooms/${room.id}`} className="text-link">
                    Открыть комнату
                  </Link>
                </article>
              ))}
            </div>
          ) : (
            <div className="empty-state">
              <p>Ты пока не состоишь ни в одной комнате.</p>
            </div>
          )}
        </section>
      ) : null}
    </div>
  )
}
