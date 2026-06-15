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
      <section className="surface-card page-hero">
        <div>
          <p className="eyebrow">Персонажи</p>
          <h2>Мои персонажи</h2>
          <p className="section-text">
            Быстрый доступ к листам, хитам, классу доспеха и ключевым владениям. Здесь остаются только твои персонажи.
          </p>
        </div>

        <div className="page-hero__actions">
          <Link to="/characters/new/identity" className="primary-button">
            Новый персонаж
          </Link>
        </div>
      </section>

      {isLoading ? <section className="surface-card loading-state">Загрузка...</section> : null}
      {error ? <section className="surface-card error-state">{error}</section> : null}

      {!isLoading && !error ? (
        characters.length > 0 ? (
          <section className="grid two-columns">
            {characters.map((character) => (
              <CharacterCard key={character.id} character={character} />
            ))}
          </section>
        ) : (
          <section className="surface-card empty-state empty-state--centered">
            <p className="eyebrow">Пока пусто</p>
            <h3>Создай первого персонажа</h3>
            <p className="muted">Мастер создания проведёт по расе, классу, характеристикам, заклинаниям и инвентарю.</p>
            <Link to="/characters/new/identity" className="primary-button">
              Начать создание
            </Link>
          </section>
        )
      ) : null}
    </div>
  )
}
