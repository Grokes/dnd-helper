import test from 'node:test'
import assert from 'node:assert/strict'
import { apiRequest, parseApiError } from './http'

test('parseApiError prefers detail, then title, then status fallback', async () => {
  const detailError = await parseApiError(jsonResponse({ detail: 'Подробная ошибка', title: 'Заголовок' }, 400))
  const titleError = await parseApiError(jsonResponse({ title: 'Ошибка заголовка' }, 422))
  const fallbackError = await parseApiError(new Response('not json', { status: 500 }))

  assert.equal(detailError.message, 'Подробная ошибка')
  assert.equal(titleError.message, 'Ошибка заголовка')
  assert.equal(fallbackError.message, 'Request failed: 500')
})

test('parseApiError keeps validation errors from backend payload', async () => {
  const error = await parseApiError(jsonResponse({
    title: 'Validation failed',
    errors: { name: ['Имя обязательно.'] },
  }, 400))

  assert.equal(error.message, 'Validation failed')
  assert.deepEqual(error.errors, { name: ['Имя обязательно.'] })
})

test('apiRequest sends json headers, includes credentials and returns parsed json', async () => {
  const { restore, calls } = mockFetch(jsonResponse({ ok: true }))

  try {
    const result = await apiRequest<{ ok: boolean }>('/api/test', {
      method: 'POST',
      headers: { 'X-Test': 'yes' },
      body: JSON.stringify({ value: 1 }),
    })

    assert.deepEqual(result, { ok: true })
    assert.equal(calls.length, 1)
    assert.equal(calls[0].path, '/api/test')
    assert.equal(calls[0].init.credentials, 'include')
    assert.equal(calls[0].init.method, 'POST')
    assert.equal((calls[0].init.headers as Record<string, string>)['Content-Type'], 'application/json')
    assert.equal((calls[0].init.headers as Record<string, string>)['X-Test'], 'yes')
    assert.equal(calls[0].init.body, '{"value":1}')
  } finally {
    restore()
  }
})

test('apiRequest allows explicit content type override for special requests', async () => {
  const { restore, calls } = mockFetch(jsonResponse({ ok: true }))

  try {
    await apiRequest('/api/upload', {
      method: 'POST',
      headers: { 'Content-Type': 'text/plain' },
      body: 'plain text',
    })

    assert.equal((calls[0].init.headers as Record<string, string>)['Content-Type'], 'text/plain')
    assert.equal(calls[0].init.body, 'plain text')
  } finally {
    restore()
  }
})

test('apiRequest returns undefined for 204 responses', async () => {
  const { restore } = mockFetch(new Response(null, { status: 204 }))

  try {
    const result = await apiRequest<void>('/api/no-content')
    assert.equal(result, undefined)
  } finally {
    restore()
  }
})

test('apiRequest uses GET by default and still includes credentials', async () => {
  const { restore, calls } = mockFetch(jsonResponse({ value: 1 }))

  try {
    await apiRequest('/api/default-get')

    assert.equal(calls[0].path, '/api/default-get')
    assert.equal(calls[0].init.method, undefined)
    assert.equal(calls[0].init.credentials, 'include')
  } finally {
    restore()
  }
})

test('apiRequest throws parsed backend errors', async () => {
  const { restore } = mockFetch(jsonResponse({ detail: 'Нет доступа' }, 403))

  try {
    await assert.rejects(
      () => apiRequest('/api/forbidden'),
      (error: unknown) => {
        assert.deepEqual(error, { message: 'Нет доступа', errors: undefined })
        return true
      },
    )
  } finally {
    restore()
  }
})

type FetchCall = {
  path: string
  init: RequestInit
}

function mockFetch(response: Response) {
  const originalFetch = globalThis.fetch
  const calls: FetchCall[] = []

  globalThis.fetch = (async (input: string | URL | Request, init?: RequestInit) => {
    calls.push({ path: input.toString(), init: init ?? {} })
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
