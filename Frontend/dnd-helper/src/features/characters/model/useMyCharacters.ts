import { useEffect, useState } from 'react'
import { getMyCharacters } from '../api/charactersApi'
import type { CharacterSummary } from '../../../types/character'

export function useMyCharacters(userId?: string) {
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!userId) {
      setCharacters([])
      setIsLoading(false)
      return
    }

    let isCancelled = false
    setIsLoading(true)

    async function loadCharacters() {
      try {
        const response = await getMyCharacters()
        if (!isCancelled) {
          setCharacters(response)
          setError(null)
        }
      } catch {
        if (!isCancelled) {
          setError('Не удалось загрузить список персонажей.')
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadCharacters()

    return () => {
      isCancelled = true
    }
  }, [userId])

  return { characters, isLoading, error }
}
