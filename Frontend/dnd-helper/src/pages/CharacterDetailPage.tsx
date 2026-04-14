import { Link, Navigate, useParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useAuth } from '../components/AuthProvider'
import { getCharacterById } from '../services/charactersApi'
import type { Character } from '../types/character'
import {
  formatSkillLevel,
  getCharacterPortrait,
  translateAbility,
} from '../utils/characterPresentation'

export function CharacterDetailPage() {
  const { user, isLoading: isAuthLoading } = useAuth()
  const { id = '' } = useParams()
  const [character, setCharacter] = useState<Character | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!user) {
      setIsLoading(false)
      return
    }

    let isCancelled = false

    async function loadCharacter() {
      try {
        const response = await getCharacterById(id)
        if (!isCancelled) {
          setCharacter(response)
          setError(null)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить персонажа.')
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false)
        }
      }
    }

    if (id) {
      void loadCharacter()
    } else {
      setIsLoading(false)
      setError('Некорректный идентификатор персонажа.')
    }

    return () => {
      isCancelled = true
    }
  }, [id, user])

  if (!isAuthLoading && !user) {
    return <Navigate to="/login" replace state={{ from: `/characters/${id}` }} />
  }

  if (isLoading) {
    return <section className="surface-card loading-state">Загрузка персонажа...</section>
  }

  if (error || !character) {
    return (
      <section className="surface-card error-state">
        <p>{error ?? 'Персонаж не найден.'}</p>
        <Link to="/characters" className="text-link">
          Вернуться к списку
        </Link>
      </section>
    )
  }

  return (
    <div className="stack">
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
          {character.canEdit ? (
            <Link to={`/characters/${character.id}/edit/identity`} className="secondary-button">
              Редактировать
            </Link>
          ) : null}
        </div>
      </section>

      <section className="grid detail-layout">
        <article className="surface-card">
          <h3>Характеристики</h3>
          <div className="ability-grid">
            {character.abilities.map((ability) => (
              <div className="ability-card" key={ability.key}>
                <span>{translateAbility(ability.key)}</span>
                <strong>{ability.score}</strong>
                <small>{ability.modifier >= 0 ? `+${ability.modifier}` : ability.modifier}</small>
              </div>
            ))}
          </div>
        </article>

        <article className="surface-card">
          <h3>Ключевые показатели</h3>
            <div className="compact-stats">
              <div>
                <span>Класс доспеха</span>
                <strong>{character.armorClass}</strong>
              </div>
              <div>
                <span>Хиты</span>
                <strong>{character.hitPoints}</strong>
              </div>
            <div>
              <span>Скорость</span>
              <strong>{character.speed}</strong>
            </div>
              <div>
                <span>Бонус мастерства</span>
                <strong>+{character.proficiencyBonus}</strong>
              </div>
              <div>
                <span>Пассивная внимательность</span>
                <strong>{character.passivePerception}</strong>
              </div>
          </div>
        </article>

        <article className="surface-card">
          <h3>Навыки</h3>
          <ul className="plain-list">
            {character.skills.length > 0 ? character.skills.map((skill) => <li key={skill.skillId}>{formatSkillLevel(skill)}</li>) : <li>Нет данных</li>}
          </ul>
        </article>

        <article className="surface-card">
          <h3>Спасброски</h3>
          <ul className="plain-list">
            {character.savingThrows.map((savingThrow) => (
              <li key={savingThrow.ability}>
                {translateAbility(savingThrow.ability)} {savingThrow.bonus >= 0 ? `+${savingThrow.bonus}` : savingThrow.bonus}
                {savingThrow.isProficient ? ' • владение' : ''}
              </li>
            ))}
          </ul>
        </article>

        <article className="surface-card">
          <h3>Заклинания</h3>
          <ul className="plain-list">
            {character.spells.length > 0 ? character.spells.map((spell) => <li key={spell}>{spell}</li>) : <li>Нет данных</li>}
          </ul>
        </article>

        <article className="surface-card">
          <h3>Инвентарь</h3>
          <ul className="plain-list">
            {character.inventory.length > 0 ? character.inventory.map((item) => <li key={item}>{item}</li>) : <li>Нет данных</li>}
          </ul>
        </article>

        <article className="surface-card">
          <h3>Заметки</h3>
          <p>{character.notes || 'Пока без заметок.'}</p>
        </article>
      </section>
    </div>
  )
}
