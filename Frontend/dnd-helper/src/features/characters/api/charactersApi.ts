import { apiRequest } from '../../../shared/api/http'
import type {
  Character,
  CharacterCastSpellPayload,
  CharacterCastSpellResult,
  CharacterOptions,
  CharacterPayload,
  CharacterRestPayload,
  CharacterRestResult,
  CharacterSummary,
} from '../../../types/character'

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
