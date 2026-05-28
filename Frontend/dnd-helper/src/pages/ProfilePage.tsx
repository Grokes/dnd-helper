import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../components/AuthProvider'
import { deleteAccount } from '../services/charactersApi'
import type { ApiValidationError } from '../types/character'

export function ProfilePage() {
  const { user, isLoading, logout } = useAuth()
  const navigate = useNavigate()
  const [isDeletePanelOpen, setIsDeletePanelOpen] = useState(false)
  const [deletePhrase, setDeletePhrase] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)
  const [deleteStatus, setDeleteStatus] = useState<string | null>(null)

  async function handleDeleteAccount() {
    if (deletePhrase.trim().toUpperCase() !== 'УДАЛИТЬ') {
      setDeleteStatus('Для удаления аккаунта введи слово «УДАЛИТЬ».')
      return
    }

    setIsDeleting(true)
    setDeleteStatus(null)

    try {
      await deleteAccount()
      await logout()
      navigate('/register', { replace: true })
    } catch (error) {
      const apiError = error as ApiValidationError
      setDeleteStatus(apiError.message || 'Не удалось удалить аккаунт.')
    } finally {
      setIsDeleting(false)
    }
  }

  if (!isLoading && !user) {
    return <Navigate to="/login" replace state={{ from: '/profile' }} />
  }

  if (isLoading || !user) {
    return <section className="surface-card loading-state">Загрузка профиля...</section>
  }

  return (
    <div className="stack">
      <section className="surface-card profile-panel">
        <div className="profile-panel__header">
          <h2>Личный кабинет</h2>
          <div className="badge-cluster">
            {user.roles.map((role) => (
              <span key={role} className="pill">
                {role === 'GameMaster' ? 'Гейм-мастер' : 'Пользователь'}
              </span>
            ))}
          </div>
        </div>
        <div className="profile-panel__grid">
          <article className="surface-card profile-fact-card">
            <span>Имя пользователя</span>
            <strong>{user.displayName}</strong>
          </article>
          <article className="surface-card profile-fact-card">
            <span>Почта</span>
            <strong>{user.email}</strong>
          </article>
          <article className="surface-card profile-fact-card">
            <span>ID аккаунта</span>
            <strong>{user.id}</strong>
          </article>
          <article className="surface-card profile-fact-card">
            <span>Роли</span>
            <strong>{user.roles.map((role) => (role === 'GameMaster' ? 'Гейм-мастер' : 'Пользователь')).join(', ')}</strong>
          </article>
        </div>
        <div className="action-row">
          <Link to="/characters" className="secondary-button">
            Мои персонажи
          </Link>
          <Link to="/rooms" className="secondary-button">
            Мои комнаты
          </Link>
          <button type="button" className="secondary-button button-reset" onClick={() => void logout()}>
            Выйти
          </button>
        </div>
      </section>

      <section className="surface-card profile-danger-zone">
        <div className="section-header-row">
          <div>
            <h3>Опасная зона</h3>
            <p className="muted">Удаление аккаунта необратимо. Будут удалены связанные персонажи, комнаты и участия.</p>
          </div>
          <button
            type="button"
            className="secondary-button button-reset danger-button"
            onClick={() => setIsDeletePanelOpen((current) => !current)}
          >
            Удалить аккаунт
          </button>
        </div>

        {isDeletePanelOpen ? (
          <div className="stack">
            <label>
              Для подтверждения введи «УДАЛИТЬ»
              <input
                className="app-search-input"
                value={deletePhrase}
                onChange={(event) => setDeletePhrase(event.target.value)}
                placeholder="УДАЛИТЬ"
              />
            </label>
            <button
              type="button"
              className="primary-button button-reset danger-button danger-button--confirm"
              onClick={() => void handleDeleteAccount()}
              disabled={isDeleting}
            >
              {isDeleting ? 'Удаление...' : 'Подтвердить удаление аккаунта'}
            </button>
            {deleteStatus ? <p className="inline-error">{deleteStatus}</p> : null}
          </div>
        ) : null}
      </section>
    </div>
  )
}
