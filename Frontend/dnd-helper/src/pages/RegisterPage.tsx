import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../features/auth/model/AuthProvider'
import type { ApiValidationError } from '../types/character'

export function RegisterPage() {
  const navigate = useNavigate()
  const { user, register } = useAuth()
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (user) {
    return <Navigate to="/profile" replace />
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setSuccessMessage(null)

    if (password !== confirmPassword) {
      setError('Пароль и подтверждение должны совпадать.')
      return
    }

    setIsSubmitting(true)

    try {
      await register({ displayName, email, password })
      setSuccessMessage('Аккаунт создан. Выполняется вход...')
      navigate('/profile', { replace: true })
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось зарегистрироваться.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section className="surface-card auth-card">
      <div className="stack">
        <div>
          <h2>Регистрация</h2>
          <p className="section-text">Создай учётную запись, чтобы хранить собственных персонажей и управлять ими из личного кабинета.</p>
        </div>

        <form className="form-grid compact" onSubmit={handleSubmit}>
          <label className="full-span">
            Отображаемое имя
            <input value={displayName} onChange={(event) => setDisplayName(event.target.value)} required />
          </label>

          <label className="full-span">
            Почта
            <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required />
          </label>

          <label>
            Пароль
            <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required />
          </label>

          <label>
            Подтверждение пароля
            <input type="password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} required />
          </label>

          {error ? <p className="inline-error full-span">{error}</p> : null}
          {successMessage ? <p className="success-text full-span">{successMessage}</p> : null}

          <button type="submit" className="primary-button button-reset" disabled={isSubmitting}>
            {isSubmitting ? 'Создание аккаунта...' : 'Зарегистрироваться'}
          </button>
        </form>

        <p className="muted">
          Уже есть аккаунт?{' '}
          <Link to="/login" className="text-link">
            Войти
          </Link>
        </p>
      </div>
    </section>
  )
}
