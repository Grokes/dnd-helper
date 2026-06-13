import { readdir } from 'node:fs/promises'
import { join } from 'node:path'
import { spawn } from 'node:child_process'

const root = new URL('../.test-dist', import.meta.url).pathname

async function walk(directory) {
  const entries = await readdir(directory, { withFileTypes: true })
  const files = await Promise.all(entries.map(async (entry) => {
    const path = join(directory, entry.name)
    return entry.isDirectory() ? walk(path) : path
  }))

  return files.flat()
}

const testFiles = (await walk(root)).filter((file) => file.endsWith('.test.js')).sort()

if (testFiles.length === 0) {
  console.error('No frontend test files were found in .test-dist.')
  process.exit(1)
}

const child = spawn(process.execPath, ['--test', ...testFiles], { stdio: 'inherit' })
child.on('exit', (code) => process.exit(code ?? 1))
