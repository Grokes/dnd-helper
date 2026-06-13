import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { getCurrentUser, loginUser, logoutUser, registerUser } from '../api/authApi'
import type { ApiValidationError, AuthUser, LoginPayload, RegisterPayload } from '../../../types/character'

type AuthContextValue = {
  user: AuthUser | null
  isLoading: boolean
  login: (payload: LoginPayload) => Promise<void>
  register: (payload: RegisterPayload) => Promise<void>
  logout: () => Promise<void>
  refreshUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  async function refreshUser() {
    try {
      const currentUser = await getCurrentUser()
      setUser(currentUser)
    } catch (error) {
      const apiError = error as ApiValidationError
      if (apiError.message.includes('401') || apiError.message.includes('Unauthorized')) {
        setUser(null)
        return
      }

      setUser(null)
    }
  }

  useEffect(() => {
    async function loadUser() {
      await refreshUser()
      setIsLoading(false)
    }

    void loadUser()
  }, [])

  async function login(payload: LoginPayload) {
    await loginUser(payload)
    await refreshUser()
  }

  async function register(payload: RegisterPayload) {
    await registerUser(payload)
    await loginUser({ email: payload.email, password: payload.password, rememberMe: false })
    await refreshUser()
  }

  async function logout() {
    await logoutUser()
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, register, logout, refreshUser }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider')
  }

  return context
}
