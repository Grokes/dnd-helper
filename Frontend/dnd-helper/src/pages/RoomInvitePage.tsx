import { useEffect, useState } from 'react'
import { Navigate, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../features/auth/model/AuthProvider'
import { joinRoomByInvite } from '../features/rooms/api/roomsApi'
import type { ApiValidationError } from '../types/character'

export function RoomInvitePage() {
  const { inviteToken = '' } = useParams()
  const navigate = useNavigate()
  const { user, isLoading: isAuthLoading } = useAuth()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!user || !inviteToken) {
      return
    }

    let isCancelled = false

    async function join() {
      try {
        const room = await joinRoomByInvite({ inviteToken })
        if (!isCancelled) {
          navigate(`/rooms/${room.id}`, { replace: true })
        }
      } catch (caughtError) {
        if (!isCancelled) {
          const apiError = caughtError as ApiValidationError
          const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
          setError(firstFieldError ?? apiError.message ?? 'Не удалось присоединиться к комнате.')
        }
      }
    }

    void join()

    return () => {
      isCancelled = true
    }
  }, [inviteToken, navigate, user])

  if (!isAuthLoading && !user) {
    return <Navigate to="/login" replace state={{ from: `/rooms/invite/${inviteToken}` }} />
  }

  return (
    <section className="surface-card loading-state">
      {error ?? 'Подключаем тебя к игровой комнате...'}
    </section>
  )
}
