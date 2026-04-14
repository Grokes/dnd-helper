import { useState } from 'react'
import { Link, Navigate, NavLink, Route, Routes, useNavigate } from 'react-router-dom'
import './App.css'
import { useAuth } from './components/AuthProvider'
import { CharacterCreatePage } from './pages/CharacterCreatePage'
import { CharacterDetailPage } from './pages/CharacterDetailPage'
import { CharactersPage } from './pages/CharactersPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { ProfilePage } from './pages/ProfilePage'
import { RegisterPage } from './pages/RegisterPage'
import { RoomDetailPage } from './pages/RoomDetailPage'
import { RoomInvitePage } from './pages/RoomInvitePage'
import { RoomsPage } from './pages/RoomsPage'

function App() {
  const navigate = useNavigate()
  const { user, isLoading, logout } = useAuth()
  const [isProfileMenuOpen, setIsProfileMenuOpen] = useState(false)

  async function handleLogout() {
    await logout()
    setIsProfileMenuOpen(false)
    navigate('/', { replace: true })
  }

  return (
    <div className="app-shell">
      <header className="topbar app-header">
        <div className="brand-panel">
          <span className="brand-mark">DH</span>
          <div>
            <p className="brand-name">DND Helper</p>
            <p className="brand-subtitle">Листы персонажей и комнаты</p>
          </div>
        </div>

        <nav className="header-nav" aria-label="Main navigation">
          <NavLink to="/" end className="nav-item">
            Главная
          </NavLink>
          {user ? (
            <>
              <NavLink to="/characters" className="nav-item">
                Мои персонажи
              </NavLink>
              <NavLink to="/rooms" className="nav-item">
                Комнаты
              </NavLink>
              <div className="profile-menu">
                <button
                  type="button"
                  className="nav-item profile-menu__trigger"
                  onClick={() => setIsProfileMenuOpen((current) => !current)}
                >
                  {user.displayName}
                </button>

                {isProfileMenuOpen ? (
                  <div className="profile-menu__popover">
                    <p className="profile-menu__label">{user.email}</p>
                    <Link to="/profile" className="profile-menu__item" onClick={() => setIsProfileMenuOpen(false)}>
                      Личный кабинет
                    </Link>
                    <Link to="/characters/new/identity" className="profile-menu__item" onClick={() => setIsProfileMenuOpen(false)}>
                      Новый персонаж
                    </Link>
                    <Link to="/rooms" className="profile-menu__item" onClick={() => setIsProfileMenuOpen(false)}>
                      Мои комнаты
                    </Link>
                    <button type="button" className="profile-menu__item profile-menu__button" onClick={() => void handleLogout()}>
                      Выйти
                    </button>
                  </div>
                ) : null}
              </div>
            </>
          ) : (
            <>
              <NavLink to="/login" className="nav-item">
                Вход
              </NavLink>
              <NavLink to="/register" className="nav-item">
                Регистрация
              </NavLink>
            </>
          )}
        </nav>
      </header>

      <main className="page-content">
        {isLoading ? (
          <section className="surface-card loading-state">Загрузка приложения...</section>
        ) : (
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/characters" element={<CharactersPage />} />
            <Route path="/characters/new" element={<Navigate to="/characters/new/identity" replace />} />
            <Route path="/characters/new/:step" element={<CharacterCreatePage />} />
            <Route path="/characters/:id/edit" element={<Navigate to="identity" replace />} />
            <Route path="/characters/:id/edit/:step" element={<CharacterCreatePage />} />
            <Route path="/characters/:id" element={<CharacterDetailPage />} />
            <Route path="/rooms" element={<RoomsPage />} />
            <Route path="/rooms/invite/:inviteToken" element={<RoomInvitePage />} />
            <Route path="/rooms/:id" element={<RoomDetailPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="*" element={<Navigate to={user ? '/profile' : '/'} replace />} />
          </Routes>
        )}
      </main>
    </div>
  )
}

export default App
