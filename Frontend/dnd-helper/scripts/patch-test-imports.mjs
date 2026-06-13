import { readdir, readFile, writeFile } from 'node:fs/promises'
import { join } from 'node:path'

const root = new URL('../.test-dist', import.meta.url).pathname

async function walk(directory) {
  const entries = await readdir(directory, { withFileTypes: true })
  const files = await Promise.all(entries.map(async (entry) => {
    const path = join(directory, entry.name)
    return entry.isDirectory() ? walk(path) : path
  }))

  return files.flat()
}

function patchRelativeSpecifiers(source) {
  return source.replace(
    /(from\s+['"])(\.{1,2}\/[^'"]+?)(['"])/g,
    (match, prefix, specifier, suffix) => {
      if (specifier.endsWith('.js') || specifier.endsWith('.json') || specifier.endsWith('.css')) {
        return match
      }

      return `${prefix}${specifier}.js${suffix}`
    },
  )
}

const files = (await walk(root)).filter((file) => file.endsWith('.js'))
await Promise.all(files.map(async (file) => {
  const source = await readFile(file, 'utf8')
  const patched = patchRelativeSpecifiers(source)
  if (patched !== source) {
    await writeFile(file, patched)
  }
}))
