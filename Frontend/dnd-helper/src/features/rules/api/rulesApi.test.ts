import test from 'node:test'
import assert from 'node:assert/strict'
import { getEquipmentCatalog, getMonstersCatalog, getRulesConditions, getRulesSpells } from './rulesApi'

test('rules api maps reference catalog endpoints', async () => {
  const { restore, calls } = mockFetchSequence([
    jsonResponse([]),
    jsonResponse([]),
    jsonResponse([]),
    jsonResponse([]),
  ])

  try {
    await getEquipmentCatalog()
    await getMonstersCatalog()
    await getRulesSpells()
    await getRulesConditions()

    assert.deepEqual(calls.map((call) => [call.path, call.init.method ?? 'GET']), [
      ['/api/equipment', 'GET'],
      ['/api/monsters', 'GET'],
      ['/api/rules/spells', 'GET'],
      ['/api/rules/conditions', 'GET'],
    ])
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
