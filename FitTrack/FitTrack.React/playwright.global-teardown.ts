import { readFile, rm } from 'node:fs/promises'
import { execFile } from 'node:child_process'
import { promisify } from 'node:util'

import { playwrightServerStatePath } from './playwright.e2e-env'

const execFileAsync = promisify(execFile)

type ServerState = {
  backendPid?: number
  frontendPid?: number
}

export default async function globalTeardown() {
  try {
    let raw: string
    try {
      raw = await readFile(playwrightServerStatePath, 'utf-8')
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code === 'ENOENT') {
        return
      }

      throw error
    }

    const state = JSON.parse(raw) as ServerState

    await Promise.allSettled([killProcessTree(state.frontendPid), killProcessTree(state.backendPid)])
  } finally {
    await rm(playwrightServerStatePath, { force: true })
  }
}

async function killProcessTree(pid: number | undefined) {
  if (!pid) {
    return
  }

  if (process.platform === 'win32') {
    await execFileAsync('taskkill', ['/PID', String(pid), '/T', '/F'])
    return
  }

  process.kill(-pid, 'SIGTERM')
}
