import { apiRequest } from '../../../shared/api/http'
import type { AuthUser, LoginPayload, RegisterPayload } from '../../../types/character'

export function registerUser(payload: RegisterPayload) {
  return apiRequest<{ message: string }>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function loginUser(payload: LoginPayload) {
  return apiRequest<{ message: string }>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function logoutUser() {
  return apiRequest<{ message: string }>('/api/auth/logout', {
    method: 'POST',
  })
}

export function deleteAccount() {
  return apiRequest<{ message: string }>('/api/auth/account', {
    method: 'DELETE',
  })
}

export function getCurrentUser() {
  return apiRequest<AuthUser>('/api/auth/me')
}
