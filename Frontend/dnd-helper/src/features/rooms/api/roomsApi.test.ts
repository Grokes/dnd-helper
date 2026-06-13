import test from 'node:test'
import assert from 'node:assert/strict'
import {
  addRoomMonster,
  applyRoomMonsterDamage,
  attackRoomCharacterByMonster,
  createRoom,
  getRoomById,
  getRoomMonsters,
  getRooms,
  joinRoom,
  joinRoomByInvite,
  removeRoomMonster,
  rollRoomMonsterDamage,
  selectRoomCharacter,
  updateRoomMemberRole,
  updateRoomPresence,
} from './roomsApi'

test('rooms api maps room endpoints and payloads', async () => {
  const { restore, calls } = mockFetchSequence([
    jsonResponse([]),
    jsonResponse({ id: 'room-1' }),
    jsonResponse({ id: 'room-1' }),
    jsonResponse({ id: 'room-1' }),
    jsonResponse({ id: 'room-1' }),
    new Response(null, { status: 204 }),
    jsonResponse({ id: 'room-1' }),
  ])

  try {
    await getRooms()
    await getRoomById('room-1')
    await createRoom({ name: 'Башня' })
    await selectRoomCharacter('room-1', { characterId: 'character-1' })
    await updateRoomMemberRole('room-1', 'user-2', { role: 'GameMaster' })
    await updateRoomPresence('room-1')
    await removeRoomMonster('room-1', 'monster-1')

    assert.deepEqual(calls.map((call) => [call.path, call.init.method ?? 'GET']), [
      ['/api/rooms', 'GET'],
      ['/api/rooms/room-1', 'GET'],
      ['/api/rooms', 'POST'],
      ['/api/rooms/room-1/character', 'PUT'],
      ['/api/rooms/room-1/members/user-2/role', 'PUT'],
      ['/api/rooms/room-1/presence', 'POST'],
      ['/api/rooms/room-1/monsters/monster-1', 'DELETE'],
    ])
    assert.equal(calls[2].init.body, '{"name":"Башня"}')
    assert.equal(calls[3].init.body, '{"characterId":"character-1"}')
    assert.equal(calls[4].init.body, '{"role":"GameMaster"}')
  } finally {
    restore()
  }
})

test('rooms api maps join by code and invite endpoints', async () => {
  const { restore, calls } = mockFetchSequence([
    jsonResponse({ id: 'room-1' }),
    jsonResponse({ id: 'room-2' }),
  ])

  try {
    await joinRoom({ joinCode: 'ABC123' })
    await joinRoomByInvite({ inviteToken: 'token-1' })

    assert.deepEqual(calls.map((call) => [call.path, call.init.method]), [
      ['/api/rooms/join', 'POST'],
      ['/api/rooms/join/invite', 'POST'],
    ])
    assert.equal(calls[0].init.body, '{"joinCode":"ABC123"}')
    assert.equal(calls[1].init.body, '{"inviteToken":"token-1"}')
  } finally {
    restore()
  }
})

test('rooms api maps monster list endpoint', async () => {
  const { restore, calls } = mockFetchSequence([jsonResponse([])])

  try {
    await getRoomMonsters('room-1')

    assert.equal(calls[0].path, '/api/rooms/room-1/monsters')
    assert.equal(calls[0].init.method, undefined)
  } finally {
    restore()
  }
})

test('rooms monster api maps monster combat endpoints and payloads', async () => {
  const { restore, calls } = mockFetchSequence([
    jsonResponse({ id: 'monster-1' }),
    jsonResponse({ monsterId: 'monster-1' }),
    jsonResponse({ monsterId: 'monster-1' }),
    jsonResponse({ monsterId: 'monster-1' }),
  ])

  try {
    await addRoomMonster('room-1', 'goblin')
    await applyRoomMonsterDamage('room-1', 'monster-1', 7)
    await rollRoomMonsterDamage('room-1', 'monster-1')
    await attackRoomCharacterByMonster('room-1', 'monster-1', 'character-1')

    assert.deepEqual(calls.map((call) => [call.path, call.init.method]), [
      ['/api/rooms/room-1/monsters', 'POST'],
      ['/api/rooms/room-1/monsters/monster-1/damage', 'POST'],
      ['/api/rooms/room-1/monsters/monster-1/roll-damage', 'POST'],
      ['/api/rooms/room-1/monsters/monster-1/attack', 'POST'],
    ])
    assert.equal(calls[0].init.body, '{"monsterSlug":"goblin"}')
    assert.equal(calls[1].init.body, '{"damage":7}')
    assert.equal(calls[3].init.body, '{"targetCharacterId":"character-1"}')
  } finally {
    restore()
  }
})

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
