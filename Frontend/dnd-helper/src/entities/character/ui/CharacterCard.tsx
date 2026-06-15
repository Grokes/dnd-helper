import { Link } from 'react-router-dom'
import type { CharacterSummary } from '../../../types/character'
import { formatSkillLevel, getCharacterPortrait } from '../../../utils/characterPresentation'

type CharacterCardProps = {
  character: CharacterSummary
}

export function CharacterCard({ character }: CharacterCardProps) {
  return (
    <article className="character-card">
      <img
        className="character-card__portrait"
        src={getCharacterPortrait(character.name, character.race, character.className)}
        alt={`Портрет персонажа ${character.name}`}
      />

      <div className="character-card__body">
        <div className="character-card__header">
          <div>
            <h3>{character.name}</h3>
            <p>
              {character.race} • {character.className}
            </p>
          </div>
          <span className="pill">Ур. {character.level}</span>
        </div>

        <div className="character-card__stats" aria-label="Ключевые показатели">
          <div className="character-card__stat">
            <span>КД</span>
            <strong>{character.armorClass}</strong>
          </div>
          <div className="character-card__stat">
            <span>Хиты</span>
            <strong>
              {character.currentHitPoints}/{character.maxHitPoints}
            </strong>
          </div>
          <div className="character-card__stat">
            <span>Вним.</span>
            <strong>{character.passivePerception}</strong>
          </div>
        </div>

        <div className="character-card__footer">
          <div className="skill-tags">
            {character.skills.slice(0, 3).map((skill) => (
              <span key={skill.skillId} className="skill-tag">
                {formatSkillLevel(skill)}
              </span>
            ))}
          </div>
          <Link to={`/characters/${character.id}`} className="text-link">
            Открыть лист
          </Link>
        </div>
      </div>
    </article>
  )
}
