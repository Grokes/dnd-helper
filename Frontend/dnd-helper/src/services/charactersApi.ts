import type {
  ApiValidationError,
  AuthUser,
  Character,
  CharacterCastSpellPayload,
  CharacterCastSpellResult,
  CharacterOptions,
  CharacterPayload,
  CharacterRestPayload,
  CharacterRestResult,
  CharacterSummary,
  CreateRoomPayload,
  EquipmentCatalogItem,
  JoinRoomByInvitePayload,
  JoinRoomPayload,
  LoginPayload,
  Room,
  RoomMonster,
  RoomMonsterDamageResult,
  RoomSummary,
  RuleConditionItem,
  RuleSpellItem,
  RegisterPayload,
  SelectRoomCharacterPayload,
  MonsterCatalogItem,
  UpdateRoomMemberRolePayload,
  MonsterDamageRoll,
  MonsterAttackResult,
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

export function restCharacter(id: string, payload: CharacterRestPayload) {
  return apiRequest<CharacterRestResult>(`/api/characters/${id}/rest`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function castCharacterSpell(id: string, payload: CharacterCastSpellPayload) {
  return apiRequest<CharacterCastSpellResult>(`/api/characters/${id}/cast-spell`, {
    method: 'POST',
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

export function deleteAccount() {
  return apiRequest<{ message: string }>('/api/auth/account', {
    method: 'DELETE',
  })
}

export function getCurrentUser() {
  return apiRequest<AuthUser>('/api/auth/me')
}

export function getEquipmentCatalog() {
  return apiRequest<EquipmentCatalogItem[]>('/api/equipment')
}

export function getMonstersCatalog() {
  return apiRequest<MonsterCatalogItem[]>('/api/monsters')
}

export function getRulesSpells() {
  return apiRequest<RuleSpellItem[]>('/api/rules/spells')
}

export function getRulesConditions() {
  return apiRequest<RuleConditionItem[]>('/api/rules/conditions')
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

export function getRoomMonsters(id: string) {
  return apiRequest<RoomMonster[]>(`/api/rooms/${id}/monsters`)
}

export function addRoomMonster(id: string, monsterSlug: string) {
  return apiRequest<RoomMonster>(`/api/rooms/${id}/monsters`, {
    method: 'POST',
    body: JSON.stringify({ monsterSlug }),
  })
}

export function applyRoomMonsterDamage(id: string, monsterId: string, damage: number) {
  return apiRequest<RoomMonsterDamageResult>(`/api/rooms/${id}/monsters/${monsterId}/damage`, {
    method: 'POST',
    body: JSON.stringify({ damage }),
  })
}

export function rollRoomMonsterDamage(id: string, monsterId: string) {
  return apiRequest<MonsterDamageRoll>(`/api/rooms/${id}/monsters/${monsterId}/roll-damage`, {
    method: 'POST',
  })
}

export function removeRoomMonster(id: string, monsterId: string) {
  return apiRequest<void>(`/api/rooms/${id}/monsters/${monsterId}`, {
    method: 'DELETE',
  })
}

export function attackRoomCharacterByMonster(id: string, monsterId: string, targetCharacterId: string) {
  return apiRequest<MonsterAttackResult>(`/api/rooms/${id}/monsters/${monsterId}/attack`, {
    method: 'POST',
    body: JSON.stringify({ targetCharacterId }),
  })
}
