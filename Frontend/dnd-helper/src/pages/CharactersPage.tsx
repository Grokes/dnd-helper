import { Link, Navigate } from 'react-router-dom'
import { useAuth } from '../features/auth/model/AuthProvider'
import { CharacterCard } from '../entities/character/ui/CharacterCard'
import { useMyCharacters } from '../features/characters/model/useMyCharacters'

export function CharactersPage() {
  const { user, isLoading: isAuthLoading } = useAuth()
  const { characters, isLoading, error } = useMyCharacters(user?.id)

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
