import type {
  ApiValidationError,
  AuthUser,
  Character,
  CharacterOptions,
  CharacterPayload,
  CharacterSummary,
  CreateRoomPayload,
  JoinRoomByInvitePayload,
  JoinRoomPayload,
  LoginPayload,
  Room,
  RoomSummary,
  RegisterPayload,
  SelectRoomCharacterPayload,
  UpdateRoomMemberRolePayload,
  UpdateRoomSessionPayload,
} from '../types/character'

async function parseApiError(response: Response): Promise<ApiValidationError> {
  try {
    const payload = (await response.json()) as {
      title?: string
      detail?: string
      errors?: Record<string, string[]>
    }

    return {
      message: payload.detail ?? payload.title ?? `Request failed: ${response.status}`,
      errors: payload.errors,
    }
  } catch {
    return {
      message: `Request failed: ${response.status}`,
    }
  }
}

async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
    ...init,
  })

  if (!response.ok) {
    throw await parseApiError(response)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export function getMyCharacters() {
  return apiRequest<CharacterSummary[]>('/api/my/characters')
}

export function getCharacterById(id: string) {
  return apiRequest<Character>(`/api/characters/${id}`)
}

export function createCharacter(payload: CharacterPayload) {
  return apiRequest<Character>('/api/characters', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateCharacter(id: string, payload: CharacterPayload) {
  return apiRequest<Character>(`/api/characters/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function getCharacterOptions() {
  return apiRequest<CharacterOptions>('/api/reference/character-options')
}

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

export function getCurrentUser() {
  return apiRequest<AuthUser>('/api/auth/me')
}

export function getRooms() {
  return apiRequest<RoomSummary[]>('/api/rooms')
}

export function getRoomById(id: string) {
  return apiRequest<Room>(`/api/rooms/${id}`)
}

export function createRoom(payload: CreateRoomPayload) {
  return apiRequest<Room>('/api/rooms', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function joinRoom(payload: JoinRoomPayload) {
  return apiRequest<Room>('/api/rooms/join', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function joinRoomByInvite(payload: JoinRoomByInvitePayload) {
  return apiRequest<Room>('/api/rooms/join/invite', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function selectRoomCharacter(id: string, payload: SelectRoomCharacterPayload) {
  return apiRequest<Room>(`/api/rooms/${id}/character`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function updateRoomPresence(id: string) {
  return apiRequest<void>(`/api/rooms/${id}/presence`, {
    method: 'POST',
  })
}

export function updateRoomMemberRole(id: string, memberUserId: string, payload: UpdateRoomMemberRolePayload) {
  return apiRequest<Room>(`/api/rooms/${id}/members/${memberUserId}/role`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function updateRoomSession(id: string, payload: UpdateRoomSessionPayload) {
  return apiRequest<Room>(`/api/rooms/${id}/session`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}
