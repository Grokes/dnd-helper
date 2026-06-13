import test from 'node:test'
import assert from 'node:assert/strict'
import { deleteAccount, getCurrentUser, loginUser, logoutUser, registerUser } from './authApi'

test('auth api maps endpoints, methods and request bodies', async () => {
  const { restore, calls } = mockFetchSequence([
    jsonResponse({ message: 'registered' }),
    jsonResponse({ message: 'logged in' }),
    jsonResponse({ id: 'user-1', email: 'a@test.local', displayName: 'A', roles: [] }),
    jsonResponse({ message: 'logged out' }),
    jsonResponse({ message: 'deleted' }),
  ])

  try {
    await registerUser({ email: 'a@test.local', password: 'secret', displayName: 'A' })
    await loginUser({ email: 'a@test.local', password: 'secret', rememberMe: true })
    await getCurrentUser()
    await logoutUser()
    await deleteAccount()

    assert.deepEqual(calls.map((call) => [call.path, call.init.method ?? 'GET']), [
      ['/api/auth/register', 'POST'],
      ['/api/auth/login', 'POST'],
      ['/api/auth/me', 'GET'],
      ['/api/auth/logout', 'POST'],
      ['/api/auth/account', 'DELETE'],
    ])
    assert.equal(calls[0].init.body, '{"email":"a@test.local","password":"secret","displayName":"A"}')
    assert.equal(calls[1].init.body, '{"email":"a@test.local","password":"secret","rememberMe":true}')
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
