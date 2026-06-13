import { Link } from 'react-router-dom'
import type { Character } from '../../../types/character'
import { getCharacterPortrait } from '../../../utils/characterPresentation'

type CharacterDetailHeroProps = {
  character: Character
  canEdit: boolean
}

export function CharacterDetailHero({ character, canEdit }: CharacterDetailHeroProps) {
  return (
    <section className="character-hero">
      <div className="character-hero__identity">
        <img
          className="character-detail__portrait"
          src={getCharacterPortrait(character.name, character.race, character.className)}
          alt={`Портрет персонажа ${character.name}`}
        />
        <div>
          <h2>{character.name}</h2>
          <p className="section-text">
            {character.race} • {character.className}
            {character.subclass ? ` • ${character.subclass}` : ''} •{' '}
            {character.background || 'Без предыстории'}
          </p>
        </div>
      </div>

      <div className="badge-cluster">
        <span className="pill">Уровень {character.level}</span>
        {character.alignment ? <span className="pill">{character.alignment}</span> : null}
        {canEdit ? (
          <Link to={`/characters/${character.id}/edit/identity`} className="secondary-button">
            Редактировать
          </Link>
        ) : null}
      </div>
    </section>
  )
}
