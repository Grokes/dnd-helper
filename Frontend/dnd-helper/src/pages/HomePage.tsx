import { Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useAuth } from '../components/AuthProvider'
import { CharacterCard } from '../components/CharacterCard'
import { getMyCharacters } from '../services/charactersApi'
import type { CharacterSummary } from '../types/character'

export function HomePage() {
  const { user } = useAuth()
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!user) {
      setCharacters([])
      setIsLoading(false)
      return
    }

    let isCancelled = false

    async function loadCharacters() {
      try {
        const response = await getMyCharacters()
        if (!isCancelled) {
          setCharacters(response.slice(0, 3))
          setError(null)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить персонажей.')
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadCharacters()

    return () => {
      isCancelled = true
    }
  }, [user])

  return (
    <div className="stack">
      <section className="hero-panel">
        <div>
          <h2>Все важные данные персонажей в одном приложении</h2>
          <p className="section-text">
            Здесь можно хранить карточки героев, быстро переходить к нужному
            листу и создавать новых персонажей без отдельного справочника или
            таблиц.
          </p>

          <div className="action-row">
            <Link to="/characters" className="primary-button">
              Мои персонажи
            </Link>
            <Link to={user ? '/characters/new/identity' : '/login'} className="secondary-button">
              {user ? 'Создать персонажа' : 'Войти для создания'}
            </Link>
          </div>
        </div>
      </section>

      <section className="surface-card">
        <div className="section-header-row">
          <div>
            <h2>Быстрый доступ</h2>
          </div>
          {user ? (
            <Link to="/characters" className="text-link">
              Все мои персонажи
            </Link>
          ) : null}
        </div>

        {!user ? (
          <div className="empty-state">
            <p>После входа здесь появятся доступные тебе персонажи и быстрые переходы.</p>
            <Link to="/login" className="primary-button">
              Войти
            </Link>
          </div>
        ) : null}

        {user && isLoading ? <p className="loading-state">Загрузка персонажей...</p> : null}
        {user && error ? <p className="error-state">{error}</p> : null}

        {user && !isLoading && !error ? (
          <div className="grid three-columns">
            {characters.map((character) => (
              <CharacterCard key={character.id} character={character} />
            ))}
          </div>
        ) : null}
      </section>
    </div>
  )
}
