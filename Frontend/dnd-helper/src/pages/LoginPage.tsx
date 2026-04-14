import { useState } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../components/AuthProvider'
import type { ApiValidationError } from '../types/character'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { user, login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (user) {
    return <Navigate to="/profile" replace />
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await login({ email, password, rememberMe })
      const nextPath = (location.state as { from?: string } | null)?.from ?? '/profile'
      navigate(nextPath, { replace: true })
    } catch (caughtError) {
      const apiError = caughtError as ApiValidationError
      const firstFieldError = apiError.errors ? Object.values(apiError.errors).flat()[0] : null
      setError(firstFieldError ?? apiError.message ?? 'Не удалось выполнить вход.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section className="surface-card auth-card">
      <div className="stack">
        <div>
          <h2>Вход</h2>
          <p className="section-text">Войди по почте, чтобы создавать своих персонажей и открывать личный кабинет.</p>
        </div>

        <form className="form-grid compact" onSubmit={handleSubmit}>
          <label className="full-span">
            Почта
            <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required />
          </label>

          <label className="full-span">
            Пароль
            <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required />
          </label>

          <label className="checkbox-row full-span">
            <input type="checkbox" checked={rememberMe} onChange={(event) => setRememberMe(event.target.checked)} />
            <span>Запомнить меня</span>
          </label>

          {error ? <p className="inline-error full-span">{error}</p> : null}

          <button type="submit" className="primary-button button-reset" disabled={isSubmitting}>
            {isSubmitting ? 'Выполняется вход...' : 'Войти'}
          </button>
        </form>

        <p className="muted">
          Нет аккаунта?{' '}
          <Link to="/register" className="text-link">
            Зарегистрироваться
          </Link>
        </p>
      </div>
    </section>
  )
}
