'use client'

import type { Components } from 'react-markdown'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import type { ComponentProps } from 'react'
import { useEffect, useMemo, useRef, useState } from 'react'

import { DailySummaryCard } from '@/components/cards/daily-summary-card'
import { DayPlanCard } from '@/components/cards/day-plan-card'
import { MealAnalysisCard } from '@/components/cards/meal-analysis-card'
import { apiFetch, apiFetchRaw, fetchBlobUrl } from '@/lib/http'
import type { ChatAttachment, ChatMessage, ConversationThread, StreamEvent, ThreadDetail } from '@/types/fittrack'

type ReactMarkdownCodeProps = ComponentProps<'code'> & {
  inline?: boolean
  node?: unknown
}

const markdownComponents: Components = {
  a: ({ node, ...props }) => (
    <a {...props} target="_blank" rel="noreferrer" className={`text-cyan-300 underline underline-offset-4 ${props.className ?? ''}`.trim()} />
  ),
  code({ inline, className, children, ...props }: ReactMarkdownCodeProps) {
    if (inline) {
      return (
        <code {...props} className={`markdown-inline-code ${className ?? ''}`.trim()}>
          {children}
        </code>
      )
    }
    return (
      <pre className="markdown-pre">
        <code {...props} className={className}>
          {children}
        </code>
      </pre>
    )
  },
}

export function ChatView() {
  const [threads, setThreads] = useState<ConversationThread[]>([])
  const [activeThreadId, setActiveThreadId] = useState<string | null>(null)
  const [threadDetail, setThreadDetail] = useState<ThreadDetail | null>(null)
  const [message, setMessage] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [creatingThread, setCreatingThread] = useState(false)
  const [streamText, setStreamText] = useState('')
  const [toolEvents, setToolEvents] = useState<string[]>([])
  const [mealPhoto, setMealPhoto] = useState<{ dataUrl: string; name?: string; mimeType?: string; size?: number } | null>(null)
  const listRef = useRef<HTMLDivElement | null>(null)

  const lastSnapshot = threadDetail?.snapshots?.[0] ?? null

  useEffect(() => {
    void bootstrap()
  }, [])

  useEffect(() => {
    if (!activeThreadId) return
    void loadThread(activeThreadId)
  }, [activeThreadId])

  useEffect(() => {
    const el = listRef.current
    if (!el) return
    el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' })
  }, [threadDetail?.messages.length, streamText])

  const messages = useMemo(() => {
    const base = threadDetail?.messages ?? []
    if (!streamText || !activeThreadId) return base
    return [
      ...base,
        {
          id: 'streaming',
          threadId: activeThreadId,
          role: 'assistant',
          kind: 'text',
          contentText: streamText,
          contentJson: null,
          attachments: [],
          turnIndex: base.length + 1,
          createdAt: new Date().toISOString(),
        } satisfies ChatMessage,
    ]
  }, [activeThreadId, streamText, threadDetail?.messages])

  async function bootstrap() {
    setLoading(true)
    try {
      const items = await refreshThreads()
      if (items.length > 0) {
        setActiveThreadId(items[0]!.id)
        return
      }

      const created = await createThread()
      setActiveThreadId(created.id)
    } catch (bootstrapError) {
      setError(bootstrapError instanceof Error ? bootstrapError.message : 'Unable to load coach workspace')
    } finally {
      setLoading(false)
    }
  }

  async function refreshThreads() {
    const items = await apiFetch<ConversationThread[]>('/api/chat/threads')
    setThreads(items)
    return items
  }

  async function createThread() {
    setCreatingThread(true)
    try {
      const created = await apiFetch<ConversationThread>('/api/chat/threads', {
        method: 'POST',
        body: JSON.stringify({ title: `Session ${new Date().toLocaleString()}` }),
      })
      setThreads((prev) => [created, ...prev.filter((item) => item.id !== created.id)])
      setThreadDetail({
        ...created,
        messages: [],
        snapshots: [],
      })
      return created
    } finally {
      setCreatingThread(false)
    }
  }

  async function loadThread(threadId: string) {
    try {
      const detail = await apiFetch<ThreadDetail>(`/api/chat/threads/${threadId}`)
      setThreadDetail(detail)
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Unable to load thread')
    }
  }

  async function sendMessage() {
    if (!activeThreadId || (!message.trim() && !mealPhoto)) return
    setError(null)
    setStreamText('')
    setToolEvents([])
    setLoading(true)

    try {
      const response = await apiFetchRaw('/api/chat/messages', {
        method: 'POST',
        body: JSON.stringify({
          threadId: activeThreadId,
          contentText: message.trim() || null,
          mealPhoto,
        }),
      })

      if (!response.ok || !response.body) {
        setError('Unable to stream coach response')
        return
      }

      setMessage('')
      setMealPhoto(null)

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''

      while (true) {
        const { value, done } = await reader.read()
        if (done) break
        buffer += decoder.decode(value, { stream: true })
        const lines = buffer.split('\n')
        buffer = lines.pop() ?? ''

        for (const line of lines) {
          if (!line.trim()) continue
          const event = JSON.parse(line) as StreamEvent
          switch (event.type) {
            case 'user_message':
              setThreadDetail((prev) => (prev ? { ...prev, messages: [...prev.messages, event.message] } : prev))
              break
            case 'assistant_message':
              setThreadDetail((prev) => (prev ? { ...prev, messages: [...prev.messages, event.message] } : prev))
              setStreamText('')
              break
            case 'snapshot':
              setThreadDetail((prev) => (prev ? { ...prev, snapshots: [event.snapshot, ...prev.snapshots] } : prev))
              break
            case 'tool_event':
              setToolEvents((prev) => [...prev, event.value])
              break
            case 'token':
              setStreamText((prev) => prev + event.value)
              break
            case 'error':
              setError(event.error ?? 'Coach failed to answer')
              break
            case 'done':
              await Promise.all([loadThread(activeThreadId), refreshThreads()])
              break
            default:
              break
          }
        }
      }
    } catch (sendError) {
      setError(sendError instanceof Error ? sendError.message : 'Unable to stream coach response')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="grid min-h-[calc(100vh-12rem)] grid-cols-1 gap-6 xl:grid-cols-[270px_minmax(0,1fr)_320px]">
      <aside className="custom-scrollbar overflow-y-auto rounded-[32px] border border-white/10 bg-slate-950/50 p-5">
        <div className="flex items-center justify-between">
          <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Threads</p>
          <button
            className="rounded-full border border-white/10 px-3 py-2 text-xs uppercase tracking-[0.2em] text-slate-200"
            disabled={creatingThread}
            onClick={async () => {
              const created = await createThread()
              setActiveThreadId(created.id)
            }}
          >
            {creatingThread ? '...' : 'New'}
          </button>
        </div>
        <div className="mt-4 space-y-3">
          {threads.map((thread) => (
            <button
              key={thread.id}
              className={`w-full rounded-2xl border px-4 py-3 text-left transition ${
                thread.id === activeThreadId ? 'border-cyan-300/40 bg-cyan-300/10' : 'border-white/10 bg-white/5 hover:bg-white/10'
              }`}
              onClick={() => setActiveThreadId(thread.id)}
            >
              <p className="font-medium text-white">{thread.title}</p>
              <p className="mt-1 text-xs text-slate-400">{new Date(thread.updatedAt).toLocaleString()}</p>
            </button>
          ))}
        </div>
      </aside>

      <section className="flex min-h-0 flex-col rounded-[32px] border border-white/10 bg-slate-950/50">
        <header className="border-b border-white/10 px-6 py-5">
          <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Supervisor + Specialists</p>
          <h3 className="mt-2 text-2xl font-semibold text-white">{threadDetail?.title ?? 'Coach session'}</h3>
        </header>
        <div ref={listRef} className="custom-scrollbar flex-1 overflow-y-auto px-6 py-5">
          <div className="space-y-5">
            {messages.map((item) => (
              <article key={item.id} className={`max-w-[85%] rounded-[28px] px-5 py-4 ${item.role === 'assistant' ? 'bg-white/5 text-white' : 'ml-auto bg-cyan-300 text-slate-950'}`}>
                {item.role === 'assistant' ? <Markdown text={item.contentText ?? ''} /> : <p className="text-sm">{item.contentText}</p>}
                {item.attachments.length > 0 ? (
                  <div className="mt-4 space-y-3">
                    {item.attachments.map((attachment) => (
                      <AttachmentPreview key={attachment.id} attachment={attachment} />
                    ))}
                  </div>
                ) : null}
                {renderStructuredCard(item)}
              </article>
            ))}
            {!loading && messages.length === 0 ? <p className="text-sm text-slate-400">Start with a meal, training, or weekly progress question.</p> : null}
          </div>
        </div>
        <div className="border-t border-white/10 px-6 py-5">
          <div className="rounded-[28px] border border-white/10 bg-white/5 p-4">
            <textarea
              rows={4}
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              placeholder="Ask about calories, meals, workouts, progress, or upload a food photo."
              className="w-full resize-none bg-transparent text-sm text-white outline-none placeholder:text-slate-500"
            />
            <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
              <label className="rounded-full border border-white/10 px-4 py-2 text-xs uppercase tracking-[0.25em] text-slate-300">
                Add meal photo
                <input
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={async (event) => {
                    const file = event.target.files?.[0]
                    if (!file) return
                    const dataUrl = await readFileAsDataUrl(file)
                    setMealPhoto({ dataUrl, name: file.name, mimeType: file.type, size: file.size })
                  }}
                />
              </label>
              <button
                disabled={loading}
                className="rounded-full bg-cyan-300 px-5 py-3 text-xs font-semibold uppercase tracking-[0.3em] text-slate-950 disabled:opacity-60"
                onClick={() => void sendMessage()}
              >
                {loading ? 'Working' : 'Send'}
              </button>
            </div>
            {mealPhoto ? <p className="mt-3 text-xs text-slate-400">Attached {mealPhoto.name}</p> : null}
            {toolEvents.length > 0 ? <p className="mt-3 text-xs text-cyan-200">{toolEvents[toolEvents.length - 1]}</p> : null}
            {error ? <p className="mt-3 text-sm text-rose-300">{error}</p> : null}
          </div>
        </div>
      </section>

      <aside className="space-y-5">
        <div className="rounded-[32px] border border-white/10 bg-slate-950/50 p-5">
          <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Latest Snapshot</p>
          <div className="mt-4 space-y-3 text-sm text-slate-300">
            <p>Calories: {lastSnapshot?.consumedCalories ?? '--'}</p>
            <p>Protein: {lastSnapshot?.consumedProteinG ?? '--'} g</p>
            <p>Carbs: {lastSnapshot?.consumedCarbsG ?? '--'} g</p>
            <p>Fat: {lastSnapshot?.consumedFatG ?? '--'} g</p>
          </div>
        </div>
        <DailySummaryCard
          content={{
            readiness: { label: 'Calories', value: `${lastSnapshot?.consumedCalories ?? '--'}` },
            weight: { label: 'Protein', value: `${lastSnapshot?.consumedProteinG ?? '--'} g` },
            sleep: { label: 'Carbs', value: `${lastSnapshot?.consumedCarbsG ?? '--'} g` },
            callout: typeof lastSnapshot?.nextSuggestions?.protein === 'string' ? lastSnapshot.nextSuggestions.protein : undefined,
            actions: Object.values(lastSnapshot?.nextSuggestions ?? {}).map(String),
          }}
        />
      </aside>
    </div>
  )
}

function AttachmentPreview({ attachment }: { attachment: ChatAttachment }) {
  const [src, setSrc] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    let objectUrl: string | null = null

    async function loadAttachment() {
      try {
        objectUrl = await fetchBlobUrl(attachment.downloadPath)
        if (active) {
          setSrc(objectUrl)
        }
      } catch (attachmentError) {
        if (active) {
          setError(attachmentError instanceof Error ? attachmentError.message : 'Unable to load attachment')
        }
      }
    }

    void loadAttachment()

    return () => {
      active = false
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl)
      }
    }
  }, [attachment.downloadPath])

  if (error) {
    return <p className="text-xs text-rose-300">{error}</p>
  }

  if (!src) {
    return <p className="text-xs text-slate-400">Loading attachment...</p>
  }

  return (
    <div>
      <img src={src} alt={attachment.fileName ?? 'Meal photo'} className="max-h-72 rounded-2xl border border-white/10 object-cover" />
      <p className="mt-2 text-xs text-slate-400">{attachment.fileName ?? 'Meal photo'}</p>
    </div>
  )
}

function Markdown({ text }: { text: string }) {
  return (
    <div className="markdown-body">
      <ReactMarkdown remarkPlugins={[remarkGfm]} components={markdownComponents}>
        {text}
      </ReactMarkdown>
    </div>
  )
}

function renderStructuredCard(message: ChatMessage) {
  const content = message.contentJson
  if (!content || message.role !== 'assistant') return null

  if (Array.isArray(content.items)) {
    return (
      <div className="mt-4">
        <MealAnalysisCard
          content={{
            meal: 'Vision Nutrition Result',
            macroBreakdown: (content.items as Array<Record<string, unknown>>).map((item) => `${item.name} ${Math.round(Number(item.calories ?? 0))} kcal`),
            score: 'AI',
            notes: [String(content.summary ?? 'Image estimate')],
          }}
        />
      </div>
    )
  }

  if (content.dayType || content.trainingType) {
    return (
      <div className="mt-4">
        <DayPlanCard
          content={{
            day: `${content.trainingType ?? 'Day'} / ${content.dayType ?? 'Plan'}`,
            headline: 'Coach-generated target',
            macros: [
              { label: 'Calories', value: `${content.targetCalories ?? '--'} kcal` },
              { label: 'Protein', value: `${content.targetProteinG ?? '--'} g` },
              { label: 'Carbs', value: `${content.targetCarbsG ?? '--'} g` },
              { label: 'Fat', value: `${content.targetFatG ?? '--'} g` },
            ],
          }}
        />
      </div>
    )
  }

  return null
}

function readFileAsDataUrl(file: File) {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => {
      if (typeof reader.result === 'string') resolve(reader.result)
      else reject(new Error('Invalid file result'))
    }
    reader.onerror = () => reject(reader.error ?? new Error('Unable to read file'))
    reader.readAsDataURL(file)
  })
}
