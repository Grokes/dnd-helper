import type { ApiValidationError } from '../../types/character'

export async function parseApiError(response: Response): Promise<ApiValidationError> {
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

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
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
