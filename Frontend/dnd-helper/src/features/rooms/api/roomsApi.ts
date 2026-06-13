import { apiRequest } from '../../../shared/api/http'
import type {
  CreateRoomPayload,
  JoinRoomByInvitePayload,
  JoinRoomPayload,
  MonsterAttackResult,
  MonsterDamageRoll,
  Room,
  RoomMonster,
  RoomMonsterDamageResult,
  RoomSummary,
  SelectRoomCharacterPayload,
  UpdateRoomMemberRolePayload,
} from '../../../types/character'

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
