import { Link, Navigate } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useAuth } from '../components/AuthProvider'
import { CharacterCard } from '../components/CharacterCard'
import { getMyCharacters } from '../services/charactersApi'
import type { CharacterSummary } from '../types/character'

export function CharactersPage() {
  const { user, isLoading: isAuthLoading } = useAuth()
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
          setCharacters(response)
          setError(null)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить список персонажей.')
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

  if (!isAuthLoading && !user) {
    return <Navigate to="/login" replace state={{ from: '/characters' }} />
  }

  return (
    <div className="stack">
      <section className="section-header-row">
        <div>
          <p className="eyebrow">Персонажи</p>
          <h2>Мои персонажи</h2>
        </div>

        <Link to="/characters/new/identity" className="primary-button">
          Новый персонаж
        </Link>
      </section>

      {isLoading ? <section className="surface-card loading-state">Загрузка...</section> : null}
      {error ? <section className="surface-card error-state">{error}</section> : null}

      {!isLoading && !error ? (
        <section className="grid two-columns">
          {characters.map((character) => (
            <CharacterCard key={character.id} character={character} />
          ))}
        </section>
      ) : null}
    </div>
  )
}
