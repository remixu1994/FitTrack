import { spawn } from 'node:child_process'
import { mkdir, writeFile } from 'node:fs/promises'
import path from 'node:path'

import {
  backendBaseUrl,
  frontendBaseUrl,
  frontendPort,
  playwrightDbPath,
  playwrightServerStatePath,
} from './playwright.e2e-env'

type RunningProcess = {
  command: string
  args: string[]
  child: ReturnType<typeof spawn>
  label: string
  output: () => string
}

export default async function globalSetup() {
  const rootDir = __dirname
  const logsDir = path.join(rootDir, 'test-results', 'server-logs')

  await mkdir(logsDir, { recursive: true })

  const backend = startProcess({
    label: 'FitTrack.Copilot',
    command: 'dotnet',
    args: ['run', '--no-launch-profile', '--project', '../FitTrack.Copilot'],
    cwd: rootDir,
    env: {
      ...process.env,
      ASPNETCORE_URLS: backendBaseUrl,
      ASPNETCORE_ENVIRONMENT: 'Development',
      DOTNET_ENVIRONMENT: 'Development',
      Frontend__BaseUrl: frontendBaseUrl,
      Cors__AllowedOrigins__0: frontendBaseUrl,
      ConnectionStrings__DefaultConnection: `Data Source=${playwrightDbPath};Cache=Shared`,
    },
    logPath: path.join(logsDir, 'backend.log'),
  })

  await waitForUrl(`${backendBaseUrl}/health`, backend, 180_000)

  const frontend = startProcess({
    label: 'FitTrack.React',
    command: process.platform === 'win32' ? 'cmd.exe' : 'npm',
    args:
      process.platform === 'win32'
        ? ['/c', 'npm', 'run', 'dev', '--', '--hostname', 'localhost', '--port', frontendPort]
        : ['run', 'dev', '--', '--hostname', 'localhost', '--port', frontendPort],
    cwd: rootDir,
    env: {
      ...process.env,
      NEXT_PUBLIC_API_BASE_URL: backendBaseUrl,
    },
    logPath: path.join(logsDir, 'frontend.log'),
  })

  await waitForUrl(frontendBaseUrl, frontend, 180_000)

  await writeFile(
    playwrightServerStatePath,
    JSON.stringify(
      {
        backendPid: backend.child.pid,
        frontendPid: frontend.child.pid,
      },
      null,
      2,
    ),
    'utf-8',
  )
}

function startProcess(options: {
  label: string
  command: string
  args: string[]
  cwd: string
  env: NodeJS.ProcessEnv
  logPath: string
}): RunningProcess {
  let output = ''

  const child = spawn(options.command, options.args, {
    cwd: options.cwd,
    env: options.env,
    stdio: ['ignore', 'pipe', 'pipe'],
    shell: false,
    windowsHide: true,
    detached: process.platform !== 'win32',
  })

  child.stdout?.on('data', (chunk) => {
    output = appendOutput(output, chunk)
  })

  child.stderr?.on('data', (chunk) => {
    output = appendOutput(output, chunk)
  })

  return {
    command: options.command,
    args: options.args,
    child,
    label: options.label,
    output: () => output,
  }
}

async function waitForUrl(url: string, processInfo: RunningProcess, timeoutMs: number) {
  const startedAt = Date.now()

  while (Date.now() - startedAt < timeoutMs) {
    if (processInfo.child.exitCode !== null) {
      throw new Error(
        `${processInfo.label} exited before ${url} became ready.\nCommand: ${processInfo.command} ${processInfo.args.join(' ')}\n\n${processInfo.output()}`,
      )
    }

    try {
      const response = await fetch(url, { redirect: 'manual' })
      if (response.ok || response.status === 302 || response.status === 307 || response.status === 308) {
        return
      }
    } catch {
      // Keep polling until the process is ready or exits.
    }

    await new Promise((resolve) => setTimeout(resolve, 1000))
  }

  throw new Error(`${processInfo.label} did not become ready at ${url} within ${timeoutMs}ms.\n\n${processInfo.output()}`)
}

function appendOutput(current: string, chunk: Buffer) {
  const next = `${current}${chunk.toString()}`
  return next.length > 20_000 ? next.slice(-20_000) : next
}
