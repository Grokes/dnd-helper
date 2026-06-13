import test from 'node:test'
import assert from 'node:assert/strict'
import {
  castCharacterSpell,
  createCharacter,
  getCharacterById,
  getCharacterOptions,
  getMyCharacters,
  restCharacter,
  updateCharacter,
} from './charactersApi'
import type { CharacterPayload } from '../../../types/character'

test('characters api maps endpoints and payloads', async () => {
  const payload = createCharacterPayload()
  const { restore, calls } = mockFetchSequence([
    jsonResponse([]),
    jsonResponse({ id: 'character-1' }),
    jsonResponse({ id: 'character-1' }),
    jsonResponse({ id: 'character-1' }),
    jsonResponse({ restType: 'short' }),
    jsonResponse({ spellSlug: 'shield' }),
    jsonResponse({ races: [], classes: [], backgrounds: [] }),
  ])

  try {
    await getMyCharacters()
    await getCharacterById('character-1')
    await createCharacter(payload)
    await updateCharacter('character-1', payload)
    await restCharacter('character-1', { restType: 'short', hitDiceToSpend: 1 })
    await castCharacterSpell('character-1', { spellSlug: 'shield', slotLevel: 1 })
    await getCharacterOptions()

    assert.deepEqual(calls.map((call) => [call.path, call.init.method ?? 'GET']), [
      ['/api/my/characters', 'GET'],
      ['/api/characters/character-1', 'GET'],
      ['/api/characters', 'POST'],
      ['/api/characters/character-1', 'PUT'],
      ['/api/characters/character-1/rest', 'POST'],
      ['/api/characters/character-1/cast-spell', 'POST'],
      ['/api/reference/character-options', 'GET'],
    ])
    assert.equal(calls[2].init.body, JSON.stringify(payload))
    assert.equal(calls[4].init.body, '{"restType":"short","hitDiceToSpend":1}')
    assert.equal(calls[5].init.body, '{"spellSlug":"shield","slotLevel":1}')
  } finally {
    restore()
  }
})

test('characters api sends full-heal rest payload without hit dice', async () => {
  const { restore, calls } = mockFetchSequence([jsonResponse({ restType: 'full-heal' })])

  try {
    await restCharacter('character-1', { restType: 'full-heal' })

    assert.equal(calls[0].path, '/api/characters/character-1/rest')
    assert.equal(calls[0].init.method, 'POST')
    assert.equal(calls[0].init.body, '{"restType":"full-heal"}')
  } finally {
    restore()
  }
})

test('characters api sends cantrip cast payload without slot level', async () => {
  const { restore, calls } = mockFetchSequence([jsonResponse({ spellSlug: 'fire-bolt' })])

  try {
    await castCharacterSpell('character-1', { spellSlug: 'fire-bolt' })

    assert.equal(calls[0].path, '/api/characters/character-1/cast-spell')
    assert.equal(calls[0].init.method, 'POST')
    assert.equal(calls[0].init.body, '{"spellSlug":"fire-bolt"}')
  } finally {
    restore()
  }
})

function createCharacterPayload(): CharacterPayload {
  return {
    name: 'Ада',
    raceId: 'human',
    classId: 'fighter',
    backgroundId: 'soldier',
    level: 1,
    alignment: '',
    notes: '',
    baseAbilities: [
      { key: 'STR', score: 10 },
      { key: 'DEX', score: 10 },
      { key: 'CON', score: 10 },
      { key: 'INT', score: 10 },
      { key: 'WIS', score: 10 },
      { key: 'CHA', score: 10 },
    ],
    bonusAbilitySelections: [],
    raceSkillSelections: [],
    classSkillSelections: [],
    spells: [],
    inventory: [],
  }
}

type FetchCall = {
  path: string
  init: RequestInit
}

function mockFetchSequence(responses: Response[]) {
  const originalFetch = globalThis.fetch
  const calls: FetchCall[] = []
  let index = 0

  globalThis.fetch = (async (input: string | URL | Request, init?: RequestInit) => {
    calls.push({ path: input.toString(), init: init ?? {} })
    const response = responses[Math.min(index, responses.length - 1)]
    index += 1
    return response.clone()
  }) as typeof fetch

  return {
    calls,
    restore: () => {
      globalThis.fetch = originalFetch
    },
  }
}

function jsonResponse(payload: unknown, status = 200) {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}
