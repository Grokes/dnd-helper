import { useEffect, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { useAuth } from '../components/AuthProvider'
import { CharacterCard } from '../components/CharacterCard'
import { getMyCharacters } from '../services/charactersApi'
import type { CharacterSummary } from '../types/character'

export function ProfilePage() {
  const { user, isLoading, logout } = useAuth()
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [isCharactersLoading, setIsCharactersLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isCancelled = false

    async function loadCharacters() {
      if (!user) {
        setIsCharactersLoading(false)
        return
      }

      try {
        const response = await getMyCharacters()
        if (!isCancelled) {
          setCharacters(response)
          setError(null)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить персонажей из личного кабинета.')
        }
      } finally {
        if (!isCancelled) {
          setIsCharactersLoading(false)
        }
      }
    }

    void loadCharacters()

    return () => {
      isCancelled = true
    }
  }, [user])

  if (!isLoading && !user) {
    return <Navigate to="/login" replace state={{ from: '/profile' }} />
  }

  if (isLoading || !user) {
    return <section className="surface-card loading-state">Загрузка профиля...</section>
  }

  return (
    <div className="stack">
      <section className="surface-card profile-hero">
        <div>
          <h2>{user.displayName}</h2>
          <p className="section-text">{user.email}</p>
          <div className="badge-cluster">
            {user.roles.map((role) => (
              <span key={role} className="pill">
                {role === 'GameMaster' ? 'Гейм-мастер' : 'Пользователь'}
              </span>
            ))}
          </div>
        </div>

        <div className="action-row">
          <Link to="/characters/new/identity" className="primary-button">
            Создать персонажа
          </Link>
          <button type="button" className="secondary-button button-reset" onClick={() => void logout()}>
            Выйти
          </button>
        </div>
      </section>

      <section className="surface-card">
        <div className="section-header-row">
          <div>
            <h2>Мои персонажи</h2>
          </div>
          <Link to="/rooms" className="text-link">
            Мои комнаты
          </Link>
        </div>

        {isCharactersLoading ? <p className="loading-state">Загрузка персонажей...</p> : null}
        {error ? <p className="error-state">{error}</p> : null}

        {!isCharactersLoading && !error ? (
          characters.length > 0 ? (
            <div className="grid two-columns">
              {characters.map((character) => (
                <CharacterCard key={character.id} character={character} />
              ))}
            </div>
          ) : (
            <div className="empty-state">
              <p>Пока у тебя нет собственных персонажей.</p>
              <Link to="/characters/new/identity" className="primary-button">
                Создать первого персонажа
              </Link>
            </div>
          )
        ) : null}
      </section>
    </div>
  )
}
