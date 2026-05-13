'use client'

import Image from 'next/image'
import type { Components } from 'react-markdown'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import type { ComponentProps } from 'react'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'

import { DailySummaryCard, type DailySummaryCardContent } from '@/components/cards/daily-summary-card'
import { DayPlanCard, type DayPlanCardContent } from '@/components/cards/day-plan-card'
import { MealAnalysisCard, type MealAnalysisCardContent } from '@/components/cards/meal-analysis-card'
import { useLanguage } from '@/components/providers/language-provider'
import { apiFetch, apiFetchRaw, fetchBlobUrl } from '@/lib/http'
import type { ChatAttachment, ChatMessage, ConversationThread, NutritionSnapshot, StreamEvent, ThreadDetail, UserProfile } from '@/types/fittrack'

type ReactMarkdownCodeProps = ComponentProps<'code'> & {
  inline?: boolean
  node?: unknown
}

type MealPhotoDraft = {
  dataUrl: string
  name?: string
  mimeType?: string
  size?: number
}

type PromptAction = {
  id: string
  label: string
  description: string
  prompt: string
}

type SubmittedDraft = {
  text: string
  mealPhoto: MealPhotoDraft | null
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

export function ChatView({ initialDraft = '' }: { initialDraft?: string }) {
  const { language, locale } = useLanguage()
  const isChinese = language === 'zh-CN'
  const [threads, setThreads] = useState<ConversationThread[]>([])
  const [activeThreadId, setActiveThreadId] = useState<string | null>(null)
  const [threadDetail, setThreadDetail] = useState<ThreadDetail | null>(null)
  const [profile, setProfile] = useState<UserProfile | null>(null)
  const [message, setMessage] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [creatingThread, setCreatingThread] = useState(false)
  const [streamText, setStreamText] = useState('')
  const [toolEvents, setToolEvents] = useState<string[]>([])
  const [mealPhoto, setMealPhoto] = useState<MealPhotoDraft | null>(null)
  const [lastSubmittedDraft, setLastSubmittedDraft] = useState<SubmittedDraft | null>(null)
  const [isNearBottom, setIsNearBottom] = useState(true)
  const listRef = useRef<HTMLDivElement | null>(null)
  const textareaRef = useRef<HTMLTextAreaElement | null>(null)
  const autoScrollRef = useRef(true)
  const appliedDraftRef = useRef<string | null>(null)
  const draftMessage = initialDraft.trim()

  const lastSnapshot = threadDetail?.snapshots?.[0] ?? null
  const latestMealAnalysis = useMemo(
    () => [...(threadDetail?.messages ?? [])].reverse().map(extractMealAnalysisCard).find(Boolean) ?? null,
    [threadDetail?.messages],
  )
  const dailySummaryContent = useMemo(() => buildDailySummaryContent(lastSnapshot), [lastSnapshot])
  const dayPlanContent = useMemo(() => buildDayPlanContent(lastSnapshot), [lastSnapshot])
  const suggestedPrompts = useMemo(() => buildSuggestedPrompts(profile, language), [profile, language])
  const welcomeCopy = useMemo(() => buildWelcomeCopy(profile, language), [profile, language])
  const activeThread = threads.find((thread) => thread.id === activeThreadId) ?? threadDetail
  const hasMessages = (threadDetail?.messages.length ?? 0) > 0 || streamText.length > 0

  const refreshThreads = useCallback(async () => {
    const items = await apiFetch<ConversationThread[]>('/api/chat/threads')
    setThreads(items)
    return items
  }, [isChinese, locale])

  const createThread = useCallback(async () => {
    setCreatingThread(true)
    try {
      const created = await apiFetch<ConversationThread>('/api/chat/threads', {
        method: 'POST',
        body: JSON.stringify({ title: buildNewThreadTitle(locale, isChinese) }),
      })
      setThreads((prev) => [created, ...prev.filter((item) => item.id !== created.id)])
      setThreadDetail({
        ...created,
        messages: [],
        snapshots: [],
      })
      setMessage('')
      setMealPhoto(null)
      setStreamText('')
      setToolEvents([])
      setError(null)
      return created
    } finally {
      setCreatingThread(false)
    }
  }, [isChinese])

  const loadThread = useCallback(async (threadId: string) => {
    try {
      const detail = await apiFetch<ThreadDetail>(`/api/chat/threads/${threadId}`)
      setThreadDetail(detail)
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : isChinese ? '无法加载会话' : 'Unable to load thread')
    }
  }, [])

  const loadProfile = useCallback(async () => {
    try {
      const item = await apiFetch<UserProfile>('/api/profile')
      setProfile(item)
    } catch {
      setProfile(null)
    }
  }, [])

  const bootstrap = useCallback(async () => {
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
      setError(bootstrapError instanceof Error ? bootstrapError.message : isChinese ? '无法加载教练工作台' : 'Unable to load coach workspace')
    } finally {
      setLoading(false)
    }
  }, [createThread, isChinese, refreshThreads])

  useEffect(() => {
    void bootstrap()
    void loadProfile()
  }, [bootstrap, loadProfile])

  useEffect(() => {
    if (!activeThreadId) return
    void loadThread(activeThreadId)
  }, [activeThreadId, loadThread])

  useEffect(() => {
    if (!activeThreadId || !draftMessage) return
    if (appliedDraftRef.current === draftMessage) return

    setMessage((current) => current || draftMessage)
    appliedDraftRef.current = draftMessage
    textareaRef.current?.focus()
  }, [activeThreadId, draftMessage])

  const handleScroll = useCallback(() => {
    const el = listRef.current
    if (!el) return
    const { scrollHeight, scrollTop, clientHeight } = el
    const nearBottom = scrollHeight - scrollTop - clientHeight < 120
    setIsNearBottom(nearBottom)
    if (nearBottom) {
      autoScrollRef.current = true
    } else {
      autoScrollRef.current = false
    }
  }, [])

  const scrollToBottom = useCallback(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: 'smooth' })
    autoScrollRef.current = true
    setIsNearBottom(true)
  }, [])

  useEffect(() => {
    const el = listRef.current
    if (!el) return
    const { scrollHeight, scrollTop, clientHeight } = el
    const wasNearBottom = scrollHeight - scrollTop - clientHeight < 120
    if (wasNearBottom || autoScrollRef.current) {
      el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' })
      setIsNearBottom(true)
    } else {
      setIsNearBottom(false)
    }
  }, [threadDetail?.messages.length, streamText, toolEvents.length])

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

  async function sendMessage(options?: { overrideText?: string; overrideMealPhoto?: MealPhotoDraft | null }) {
    const threadId = activeThreadId
    const draftText = options?.overrideText ?? message
    const draftMealPhoto = options?.overrideMealPhoto ?? mealPhoto
    const trimmedText = draftText.trim()

    if (!threadId || (!trimmedText && !draftMealPhoto)) return
    setError(null)
    setStreamText('')
    setToolEvents([])
    setLoading(true)
    setLastSubmittedDraft({ text: trimmedText, mealPhoto: draftMealPhoto })

    try {
      const response = await apiFetchRaw('/api/chat/messages', {
        method: 'POST',
        body: JSON.stringify({
          threadId,
          contentText: trimmedText || null,
          mealPhoto: draftMealPhoto,
          languageCode: language,
        }),
      })

      if (!response.ok || !response.body) {
        setError(isChinese ? '无法流式返回教练回复' : 'Unable to stream coach response')
        setMessage((current) => current || trimmedText)
        setMealPhoto((current) => current ?? draftMealPhoto)
        return
      }

      setMessage('')
      setMealPhoto(null)

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let buffer = ''
      let restoreDraft = false

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
              restoreDraft = true
              setError(event.error ?? (isChinese ? '教练未能完成回复' : 'Coach failed to answer'))
              break
            case 'done':
              await Promise.all([loadThread(threadId), refreshThreads()])
              break
            default:
              break
          }
        }
      }

      if (restoreDraft) {
        setMessage((current) => current || trimmedText)
        setMealPhoto((current) => current ?? draftMealPhoto)
      }
    } catch (sendError) {
      setError(sendError instanceof Error ? sendError.message : isChinese ? '无法流式返回教练回复' : 'Unable to stream coach response')
      setMessage((current) => current || trimmedText)
      setMealPhoto((current) => current ?? draftMealPhoto)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="grid grid-cols-1 gap-6 xl:h-full xl:min-h-0 xl:grid-cols-[290px_minmax(0,1fr)_360px] xl:overflow-hidden">
      <aside className="custom-scrollbar overflow-y-auto rounded-[32px] border border-white/10 bg-slate-950/55 p-5 xl:min-h-0">
        <div className="rounded-[28px] border border-white/10 bg-[radial-gradient(circle_at_top,_rgba(34,211,238,0.12),_transparent_55%),linear-gradient(180deg,_rgba(15,23,42,0.9),_rgba(2,6,23,0.55))] p-4">
          <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Thread Memory</p>
          <h3 className="mt-3 text-lg font-semibold text-white">{isChinese ? '把计划、餐食和复盘留在同一个连续会话里。' : 'Keep plan, meals, and review in one running session.'}</h3>
          <p className="mt-2 text-sm text-slate-400">{isChinese ? '当一天都在同一个工作区里进行时，教练能保留更完整的上下文。' : 'Your coach keeps better context when the day stays in one workspace.'}</p>
          <button
            className="mt-4 w-full rounded-full border border-cyan-300/30 bg-cyan-300/10 px-4 py-3 text-xs font-semibold uppercase tracking-[0.25em] text-cyan-100 transition hover:bg-cyan-300/15 disabled:opacity-60"
            disabled={creatingThread}
            onClick={async () => {
              const created = await createThread()
              setActiveThreadId(created.id)
            }}
          >
            {creatingThread ? (isChinese ? '创建中...' : 'Creating...') : isChinese ? '新建今日会话' : 'New Today Session'}
          </button>
        </div>

        <div className="mt-5 flex items-center justify-between">
          <p className="text-xs uppercase tracking-[0.35em] text-slate-400">{isChinese ? '会话线程' : 'Conversation threads'}</p>
          <span className="rounded-full border border-white/10 px-3 py-1 text-[11px] uppercase tracking-[0.25em] text-slate-400">
            {isChinese ? `${threads.length} 个活跃` : `${threads.length} active`}
          </span>
        </div>

        <div className="mt-4 space-y-3">
          {threads.length === 0 ? (
            <div className="rounded-[24px] border border-dashed border-white/10 bg-white/5 px-4 py-5 text-sm text-slate-400">
              {isChinese ? '还没有会话。可以从今日计划或餐食识别开始。' : 'No threads yet. Start with today&apos;s plan or a meal scan.'}
            </div>
          ) : null}
          {threads.map((thread) => {
            const active = thread.id === activeThreadId
            return (
              <button
                key={thread.id}
                className={`w-full rounded-[24px] border px-4 py-4 text-left transition ${
                  active
                    ? 'border-cyan-300/35 bg-cyan-300/10 shadow-lg shadow-cyan-950/20'
                    : 'border-white/10 bg-white/5 hover:border-white/20 hover:bg-white/10'
                }`}
                onClick={() => setActiveThreadId(thread.id)}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="truncate font-medium text-white">{formatThreadTitle(thread)}</p>
                    <p className="mt-1 text-xs text-slate-400">{formatThreadTimestamp(thread.updatedAt)}</p>
                  </div>
                  {active ? <span className="mt-1 inline-flex h-2.5 w-2.5 rounded-full bg-cyan-300" /> : null}
                </div>
              </button>
            )
          })}
        </div>
      </aside>

      <section className="flex min-h-0 flex-col rounded-[32px] border border-white/10 bg-slate-950/55 xl:overflow-hidden">
        <header className="shrink-0 border-b border-white/10 px-6 py-5">
          <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">Integrated Fitness Coach</p>
          <div className="mt-3 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <h3 className="text-2xl font-semibold text-white">{activeThread?.title ?? (isChinese ? '教练会话' : 'Coach session')}</h3>
              <p className="mt-2 max-w-3xl text-sm text-slate-400">
                {isChinese ? '在不切换上下文的前提下完成训练规划、餐食分析和恢复复盘。' : 'Plan training, analyze meals, and close the loop on recovery without switching contexts.'}
              </p>
            </div>
            <div className="flex flex-wrap gap-2 text-xs uppercase tracking-[0.25em] text-slate-300">
              <MetricPill label={isChinese ? '消息' : 'Messages'} value={String(threadDetail?.messages.length ?? 0)} />
              <MetricPill label={isChinese ? '快照' : 'Snapshots'} value={String(threadDetail?.snapshots.length ?? 0)} />
              <MetricPill label={isChinese ? '模型' : 'Model'} value={profile?.preferredModelConnectorId ? (isChinese ? '自定义模型' : 'Custom model') : isChinese ? '租户默认' : 'Tenant default'} />
            </div>
          </div>
        </header>
        <div ref={listRef} onScroll={handleScroll} className="custom-scrollbar relative flex-1 overflow-y-auto px-6 py-5">
          {!hasMessages ? (
            <WelcomePanel
              copy={welcomeCopy}
              prompts={suggestedPrompts}
              loading={loading}
              onAttachClick={() => document.getElementById('coach-meal-photo-input')?.click()}
              onPromptClick={(prompt) => {
                void sendMessage({ overrideText: prompt.prompt })
              }}
            />
          ) : (
            <div className="space-y-5">
              {messages.map((item) => (
                <article
                  key={item.id}
                  className={`max-w-[90%] rounded-[30px] border px-5 py-4 shadow-lg ${
                    item.role === 'assistant'
                      ? 'border-white/8 bg-white/5 text-white shadow-slate-950/20'
                      : 'ml-auto border-cyan-300/20 bg-cyan-300/10 text-cyan-50 shadow-cyan-950/20'
                  }`}
                >
                  <div className="mb-3 flex items-center justify-between gap-4">
                    <span className="text-[11px] uppercase tracking-[0.32em] text-slate-400">
                      {item.role === 'assistant'
                        ? item.id === 'streaming'
                          ? isChinese ? '教练回复中' : 'Coach Live'
                          : isChinese ? '教练' : 'Coach'
                        : isChinese ? '你' : 'You'}
                    </span>
                    <span className="text-[11px] text-slate-500">{formatMessageTimestamp(item.createdAt, locale)}</span>
                  </div>
                  {item.role === 'assistant' ? (
                    <Markdown text={item.contentText ?? ''} />
                  ) : (
                    <p className="whitespace-pre-wrap text-sm leading-6">{item.contentText}</p>
                  )}
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
            </div>
          )}
          {!isNearBottom && (
            <button
              type="button"
              onClick={scrollToBottom}
              className="absolute bottom-4 right-6 flex items-center gap-2 rounded-full border border-cyan-300/30 bg-slate-950/90 px-4 py-2 text-xs text-cyan-200 backdrop-blur-sm shadow-lg transition hover:border-cyan-300/50"
            >
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M7 3v8M3 7l4 4 4-4" />
              </svg>
              {isChinese ? '滚动到最新消息' : 'Scroll to latest'}
            </button>
          )}
        </div>
        <div className="shrink-0 border-t border-white/10 px-6 py-5">
          <div className="rounded-[30px] border border-white/10 bg-[linear-gradient(180deg,_rgba(15,23,42,0.82),_rgba(2,6,23,0.6))] p-4">
            <div className="flex flex-wrap gap-2">
              {suggestedPrompts.slice(0, 3).map((prompt) => (
                <button
                  key={prompt.id}
                  type="button"
                  disabled={loading}
                  className="rounded-full border border-white/10 px-3 py-2 text-[11px] uppercase tracking-[0.25em] text-slate-300 transition hover:border-cyan-300/30 hover:text-cyan-100 disabled:opacity-60"
                  onClick={() => {
                    setMessage(prompt.prompt)
                    textareaRef.current?.focus()
                  }}
                >
                  {prompt.label}
                </button>
              ))}
            </div>

            {toolEvents.length > 0 ? (
              <div className="mt-4 flex items-center gap-2 rounded-full border border-cyan-300/20 bg-cyan-300/10 px-4 py-2 text-xs text-cyan-100">
                <span className="inline-flex h-2 w-2 rounded-full bg-cyan-300" />
                {toolEvents[toolEvents.length - 1]}
              </div>
            ) : null}

            {mealPhoto ? (
              <div className="mt-4 flex items-center gap-3 rounded-[24px] border border-white/10 bg-white/5 p-3">
                <Image
                  src={mealPhoto.dataUrl}
                  alt={mealPhoto.name ?? 'Selected meal'}
                  width={64}
                  height={64}
                  unoptimized
                  className="h-16 w-16 rounded-2xl object-cover"
                />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-white">{mealPhoto.name ?? 'Meal photo ready'}</p>
                  <p className="mt-1 text-xs text-slate-400">
                    {mealPhoto.size ? `${Math.max(1, Math.round(mealPhoto.size / 1024))} KB` : 'Ready for analysis'}
                  </p>
                </div>
                <button
                  type="button"
                  className="rounded-full border border-white/10 px-3 py-2 text-[11px] uppercase tracking-[0.25em] text-slate-300"
                  onClick={() => setMealPhoto(null)}
                >
                  Remove
                </button>
              </div>
            ) : null}

            <textarea
              ref={textareaRef}
              rows={4}
              value={message}
              disabled={loading}
              onChange={(event) => setMessage(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' && !event.shiftKey) {
                  event.preventDefault()
                  void sendMessage()
                }
              }}
              placeholder={isChinese ? '告诉教练你练了什么、吃了什么，或者上传餐食图片让他分析。' : 'Tell your coach what you trained, what you ate, or drop in a meal photo for analysis.'}
              className="mt-4 w-full resize-none bg-transparent text-sm leading-6 text-white outline-none placeholder:text-slate-500 disabled:cursor-not-allowed disabled:opacity-70"
            />

            <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
              <div className="flex flex-wrap items-center gap-3">
                <label
                  htmlFor="coach-meal-photo-input"
                  className="cursor-pointer rounded-full border border-white/10 px-4 py-2 text-xs uppercase tracking-[0.25em] text-slate-300 transition hover:border-cyan-300/30 hover:text-cyan-100"
                >
                  {isChinese ? '添加餐食图片' : 'Add meal photo'}
                </label>
                <input
                  id="coach-meal-photo-input"
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
                <p className="text-xs text-slate-500">
                  {mealPhoto
                    ? isChinese
                      ? '图片已附加。可以让教练打分、估算宏量，或给出替换建议。'
                      : 'Photo attached. Ask for a score, macro estimate, or swap suggestion.'
                    : isChinese
                      ? '可以文字和图片一起发在同一轮。'
                      : 'Text first, image second. Both can go in one turn.'}
                </p>
              </div>

              <button
                disabled={loading || (!message.trim() && !mealPhoto)}
                className="rounded-full bg-cyan-300 px-5 py-3 text-xs font-semibold uppercase tracking-[0.3em] text-slate-950 transition disabled:opacity-60"
                onClick={() => void sendMessage()}
              >
                {loading ? (isChinese ? '处理中' : 'Working') : isChinese ? '发送' : 'Send'}
              </button>
            </div>

            {error ? (
              <div className="mt-4 rounded-[22px] border border-rose-400/30 bg-rose-400/10 p-4 text-sm text-rose-100">
                <p>{error}</p>
                {lastSubmittedDraft ? (
                  <div className="mt-3 flex flex-wrap gap-2">
                    <button
                      type="button"
                      className="rounded-full border border-rose-200/20 px-3 py-2 text-[11px] uppercase tracking-[0.25em]"
                      onClick={() => void sendMessage({ overrideText: lastSubmittedDraft.text, overrideMealPhoto: lastSubmittedDraft.mealPhoto })}
                    >
                      {isChinese ? '重试发送' : 'Retry send'}
                    </button>
                    <button
                      type="button"
                      className="rounded-full border border-white/10 px-3 py-2 text-[11px] uppercase tracking-[0.25em] text-slate-200"
                      onClick={() => {
                        setMessage(lastSubmittedDraft.text)
                        setMealPhoto(lastSubmittedDraft.mealPhoto)
                        textareaRef.current?.focus()
                      }}
                    >
                      {isChinese ? '恢复草稿' : 'Restore draft'}
                    </button>
                  </div>
                ) : null}
              </div>
            ) : null}
          </div>
        </div>
      </section>

      <aside className="space-y-5 xl:custom-scrollbar xl:min-h-0 xl:overflow-y-auto xl:pr-1">
        <div className="rounded-[32px] border border-white/10 bg-slate-950/55 p-5">
          <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">{isChinese ? '教练焦点' : 'Coach Focus'}</p>
          <h3 className="mt-3 text-lg font-semibold text-white">
            {isChinese ? '重要输出应该沉淀成卡片，而不是消失在聊天气泡里。' : 'Important outputs should settle into cards, not disappear in chat bubbles.'}
          </h3>
          <p className="mt-2 text-sm text-slate-400">
            {isChinese ? '每日摄入、日计划和餐食反馈会固定显示在这里，聊天继续推进时也不会丢。' : 'Daily intake, day plan, and meal feedback stay visible here while the conversation keeps moving.'}
          </p>
        </div>

        {dailySummaryContent ? (
          <DailySummaryCard content={dailySummaryContent} />
        ) : (
          <InsightPlaceholder
            eyebrow={isChinese ? '每日摘要' : 'Daily Summary'}
            title={isChinese ? '今天的摄入和下一步动作会固定在这里。' : "Today&apos;s intake and next actions will settle here."}
            body={isChinese ? '一旦教练拿到足够上下文，热量、宏量和后续动作都会固定显示在这条右侧栏。' : 'Once the coach has enough context, calories, macros, and follow-up actions stay pinned in this rail.'}
          />
        )}

        {dayPlanContent ? (
          <DayPlanCard content={dayPlanContent} />
        ) : (
          <InsightPlaceholder
            eyebrow={isChinese ? '日计划' : 'Day Plan'}
            title={isChinese ? '向教练索要今天的训练和饮食计划。' : "Ask for today&apos;s training and nutrition plan."}
            body={isChinese ? '第一轮规划完成后，宏量、训练类型和执行建议会沉淀成一张稳定卡片。' : 'The first planning turn should promote macros, training type, and execution guidance into a stable card.'}
          />
        )}

        {latestMealAnalysis ? (
          <MealAnalysisCard content={latestMealAnalysis} />
        ) : (
          <InsightPlaceholder
            eyebrow={isChinese ? '餐食分析' : 'Meal Analysis'}
            title={isChinese ? '上传餐食图片，或者描述你面前这盘食物。' : 'Upload a meal photo or describe a plate.'}
            body={isChinese ? '基于图片的分析和餐食反馈会堆叠在这里，方便你不翻聊天记录也能比较不同决策。' : 'Photo-based analysis and meal feedback will stack here so you can compare decisions without scrolling chat.'}
          />
        )}
      </aside>
    </div>
  )
}

function AttachmentPreview({ attachment }: { attachment: ChatAttachment }) {
  const { language } = useLanguage()
  const isChinese = language === 'zh-CN'
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
          setError(attachmentError instanceof Error ? attachmentError.message : isChinese ? '无法加载附件' : 'Unable to load attachment')
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
    return <p className="text-xs text-slate-400">{isChinese ? '正在加载附件...' : 'Loading attachment...'}</p>
  }

  return (
    <div>
      <Image
        src={src}
        alt={attachment.fileName ?? (isChinese ? '餐食图片' : 'Meal photo')}
        width={720}
        height={720}
        unoptimized
        className="max-h-72 rounded-2xl border border-white/10 object-cover"
      />
      <p className="mt-2 text-xs text-slate-400">{attachment.fileName ?? (isChinese ? '餐食图片' : 'Meal photo')}</p>
    </div>
  )
}

function WelcomePanel({
  copy,
  prompts,
  loading,
  onPromptClick,
  onAttachClick,
}: {
  copy: ReturnType<typeof buildWelcomeCopy>
  prompts: PromptAction[]
  loading: boolean
  onPromptClick: (prompt: PromptAction) => void
  onAttachClick: () => void
}) {
  const { language } = useLanguage()
  const isChinese = language === 'zh-CN'
  return (
    <div className="rounded-[34px] border border-white/10 bg-[radial-gradient(circle_at_top,_rgba(34,211,238,0.16),_transparent_55%),radial-gradient(circle_at_right,_rgba(16,185,129,0.12),_transparent_40%),linear-gradient(180deg,_rgba(8,15,33,0.94),_rgba(2,6,23,0.75))] p-6 shadow-2xl shadow-cyan-950/20">
      <p className="text-xs uppercase tracking-[0.35em] text-cyan-300">{copy.eyebrow}</p>
      <h3 className="mt-4 max-w-3xl text-3xl font-semibold leading-tight text-white">{copy.title}</h3>
      <p className="mt-3 max-w-3xl text-sm leading-7 text-slate-300">{copy.description}</p>

      <div className="mt-5 flex flex-wrap gap-2">
        {copy.chips.map((chip) => (
          <span key={chip} className="rounded-full border border-white/10 bg-white/5 px-3 py-1 text-[11px] uppercase tracking-[0.25em] text-slate-300">
            {chip}
          </span>
        ))}
      </div>

      <div className="mt-6 grid gap-3 md:grid-cols-2">
        {prompts.map((prompt) => (
          <button
            key={prompt.id}
            type="button"
            disabled={loading}
            className="rounded-[26px] border border-white/10 bg-white/5 p-4 text-left transition hover:border-cyan-300/30 hover:bg-white/10 disabled:opacity-60"
            onClick={() => onPromptClick(prompt)}
          >
            <p className="text-xs uppercase tracking-[0.28em] text-cyan-200">{prompt.label}</p>
            <p className="mt-2 text-base font-semibold text-white">{prompt.prompt}</p>
            <p className="mt-2 text-sm text-slate-400">{prompt.description}</p>
          </button>
        ))}
      </div>

      <div className="mt-6 flex flex-wrap gap-3">
        <button
          type="button"
          disabled={loading}
          className="rounded-full border border-cyan-300/30 bg-cyan-300/10 px-4 py-3 text-xs font-semibold uppercase tracking-[0.25em] text-cyan-100 disabled:opacity-60"
          onClick={onAttachClick}
        >
          {isChinese ? '先添加餐食图片' : 'Add meal photo first'}
        </button>
        <span className="rounded-full border border-white/10 px-4 py-3 text-xs uppercase tracking-[0.25em] text-slate-400">
          {isChinese ? '引导式开场，不是空输入框' : 'Guided opening, not an empty box'}
        </span>
      </div>
    </div>
  )
}

function MetricPill({ label, value }: { label: string; value: string }) {
  return (
    <span className="rounded-full border border-white/10 px-3 py-2">
      {label}: {value}
    </span>
  )
}

function InsightPlaceholder({ eyebrow, title, body }: { eyebrow: string; title: string; body: string }) {
  return (
    <div className="rounded-[30px] border border-dashed border-white/12 bg-white/[0.03] p-5">
      <p className="text-xs uppercase tracking-[0.35em] text-slate-400">{eyebrow}</p>
      <h3 className="mt-3 text-base font-semibold text-white">{title}</h3>
      <p className="mt-2 text-sm leading-6 text-slate-400">{body}</p>
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
    const mealContent = extractMealAnalysisCard(message)
    if (!mealContent) return null
    return (
      <div className="mt-4">
        <MealAnalysisCard content={mealContent} />
      </div>
    )
  }

  if (content.dayType || content.trainingType) {
    return (
      <div className="mt-4">
        <DayPlanCard
          content={{
            day: `${toDisplayLabel(content.trainingType) ?? 'Training'} / ${toDisplayLabel(content.dayType) ?? 'Plan'}`,
            headline: 'Coach-generated target',
            macros: [
              { label: 'Calories', value: formatCalories(content.targetCalories) },
              { label: 'Protein', value: formatGrams(content.targetProteinG) },
              { label: 'Carbs', value: formatGrams(content.targetCarbsG) },
              { label: 'Fat', value: formatGrams(content.targetFatG) },
            ],
          }}
        />
      </div>
    )
  }

  return null
}

function buildSuggestedPrompts(profile: UserProfile | null, language: 'en' | 'zh-CN'): PromptAction[] {
  const isChinese = language === 'zh-CN'
  const firstName = profile?.displayName?.split(' ')[0]
  const salutation = firstName ? `${firstName}, ` : ''

  return [
    {
      id: 'plan',
      label: isChinese ? '规划今天' : 'Plan Today',
      prompt: isChinese ? `${firstName ? `${firstName}，` : ''}帮我规划今天的训练和饮食。` : `${salutation}help me plan today's training and nutrition.`,
      description: isChinese ? '适合作为第一轮消息，一次拿到宏量目标、训练类型和执行建议。' : 'Best first turn when you want macros, training type, and execution guidance in one pass.',
    },
    {
      id: 'meal',
      label: isChinese ? '分析餐食' : 'Analyze Meal',
      prompt: isChinese ? '分析这顿饭，并告诉我该怎么调整。' : 'Analyze this meal and tell me what to change.',
      description: isChinese ? '适合在上传餐食图片后，快速拿到宏量估算和评分反馈。' : 'Use this after attaching a meal photo or if you want quick macro and score feedback.',
    },
    {
      id: 'review',
      label: isChinese ? '晚间复盘' : 'Evening Review',
      prompt: isChinese ? '帮我做今晚的恢复和营养复盘。' : "Help me do tonight's recovery and nutrition review.",
      description: isChinese ? '适合一天结束时收口，确认今天做得如何、明天该修正什么。' : 'Good for closing the loop on intake, readiness, and what to fix tomorrow.',
    },
    {
      id: 'carb',
      label: isChinese ? '碳水决策' : 'Carb Decision',
      prompt: isChinese ? '结合我的目标和今天的训练，我今天应该走高碳还是低碳？' : "Given my goal and today's training, should I run a higher-carb or lower-carb day?",
      description: isChinese ? '当你想快速做燃料决策，不想展开整套日计划时使用。' : 'Use this when you want a direct fuel decision without opening a full daily plan.',
    },
  ]
}

function buildWelcomeCopy(profile: UserProfile | null, language: 'en' | 'zh-CN') {
  const isChinese = language === 'zh-CN'
  const displayName = profile?.displayName?.trim()
  const goal = toDisplayLabel(profile?.goal)
  const activityLevel = toDisplayLabel(profile?.activityLevel)

  return {
    eyebrow: isChinese ? '引导式开场' : 'Guided Opening',
    title: displayName
      ? isChinese
        ? `${displayName}，我们先把今天的训练、饮食和恢复安排对齐。`
        : `${displayName}, let's align training, food, and recovery for today.`
      : isChinese
        ? '先在一段对话里把今天的训练、饮食和恢复串起来。'
        : "Let's line up today's training, meals, and recovery in one conversation.",
    description:
      profile
        ? isChinese
          ? '我会结合你的基础档案、最近会话上下文和今天的输入，帮你制定计划、评估餐食，或完成一天的复盘。'
          : "I can use your baseline profile, recent thread context, and today's inputs to build a plan, score a meal, or close the day with a review."
        : isChinese
          ? '可以从今日计划、餐食图片或晚间复盘开始。重要结果会固定显示在右侧洞察栏。'
          : 'Start with a daily plan, a meal photo, or an evening review. Important outcomes will stay pinned in the insight rail on the right.',
    chips: [
      goal ? (isChinese ? `目标 ${goal}` : `Goal ${goal}`) : isChinese ? '目标已就绪' : 'Goal ready',
      activityLevel ? (isChinese ? `活动 ${activityLevel}` : `Activity ${activityLevel}`) : isChinese ? '活动基线' : 'Activity baseline',
      profile?.preferredModelConnectorId ? (isChinese ? '已选择自定义模型' : 'Custom model selected') : isChinese ? '租户默认模型' : 'Tenant default model',
    ],
  }
}

function buildDailySummaryContent(snapshot: NutritionSnapshot | null): DailySummaryCardContent | null {
  if (!snapshot) return null

  const actions = Object.values(snapshot.nextSuggestions ?? {})
    .map((value) => String(value))
    .filter(Boolean)

  return {
    readiness: {
      label: 'Calories',
      value: `${formatNullableNumber(snapshot.consumedCalories)}/${formatNullableNumber(snapshot.targetCalories)}`,
      delta: snapshot.remainingCalories != null ? `${formatSigned(snapshot.remainingCalories)} kcal vs target` : undefined,
    },
    weight: {
      label: 'Protein',
      value: formatGrams(snapshot.consumedProteinG),
      delta: snapshot.targetProteinG != null ? `Target ${Math.round(snapshot.targetProteinG)} g` : undefined,
    },
    sleep: {
      label: 'Carbs',
      value: formatGrams(snapshot.consumedCarbsG),
      delta: snapshot.remainingCarbsG != null ? `${formatSigned(snapshot.remainingCarbsG)} g left` : undefined,
    },
    callout: actions[0] ?? buildSnapshotCallout(snapshot),
    actions: actions.slice(0, 3),
  }
}

function buildDayPlanContent(snapshot: NutritionSnapshot | null): DayPlanCardContent | null {
  if (!snapshot) return null

  const guidance = Object.values(snapshot.nextSuggestions ?? {})
    .map((value) => String(value))
    .filter(Boolean)

  if (
    snapshot.targetCalories == null &&
    snapshot.targetProteinG == null &&
    snapshot.targetCarbsG == null &&
    snapshot.targetFatG == null &&
    !snapshot.dayType &&
    !snapshot.trainingType
  ) {
    return null
  }

  return {
    day: `${toDisplayLabel(snapshot.trainingType) ?? 'Training'} / ${toDisplayLabel(snapshot.dayType) ?? 'Day'}`,
    headline: 'Coach-generated focus for the rest of today',
    macros: [
      { label: 'Calories', value: formatCalories(snapshot.targetCalories), annotation: snapshot.remainingCalories != null ? `${Math.round(snapshot.remainingCalories)} kcal left` : undefined },
      { label: 'Protein', value: formatGrams(snapshot.targetProteinG), annotation: snapshot.remainingProteinG != null ? `${Math.round(snapshot.remainingProteinG)} g left` : undefined },
      { label: 'Carbs', value: formatGrams(snapshot.targetCarbsG), annotation: snapshot.remainingCarbsG != null ? `${Math.round(snapshot.remainingCarbsG)} g left` : undefined },
      { label: 'Fats', value: formatGrams(snapshot.targetFatG), annotation: snapshot.remainingFatG != null ? `${Math.round(snapshot.remainingFatG)} g left` : undefined },
    ],
    guidance: guidance.slice(0, 3),
  }
}

function extractMealAnalysisCard(message: ChatMessage): MealAnalysisCardContent | null {
  const content = message.contentJson
  if (!content || !Array.isArray(content.items)) return null

  const items = content.items.filter((item): item is Record<string, unknown> => typeof item === 'object' && item !== null)
  if (items.length === 0) return null

  const notes = [
    typeof content.summary === 'string' ? content.summary : null,
    ...items.slice(0, 2).map((item) => {
      const servingHint = typeof item.servingHint === 'string' ? item.servingHint : null
      const source = typeof item.source === 'string' ? item.source : null
      if (servingHint) return `${String(item.name ?? 'Item')} uses ${servingHint}.`
      if (source && source !== 'unknown') return `${String(item.name ?? 'Item')} matched via ${source}.`
      return null
    }),
  ].filter(Boolean) as string[]

  return {
    meal: message.attachments.length > 0 ? 'Latest meal scan' : 'Meal analysis',
    macroBreakdown: items.slice(0, 4).map((item) => `${String(item.name ?? 'Item')} ${formatNullableNumber(item.calories)} kcal`),
    score: typeof content.score === 'string' ? content.score : 'AI',
    notes,
  }
}

function formatThreadTitle(thread: ConversationThread) {
  if (thread.title.startsWith('Session ')) {
    return `Session / ${formatShortTime(thread.createdAt)}`
  }

  return thread.title
}

function formatThreadTimestamp(value: string) {
  return new Date(value).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatMessageTimestamp(value: string, locale: string) {
  return new Date(value).toLocaleTimeString(locale, {
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatShortTime(value: string) {
  return new Date(value).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  })
}

function buildNewThreadTitle(locale: string, isChinese: boolean) {
  const now = new Date()
  return isChinese
    ? `今日 / ${now.toLocaleTimeString(locale, { hour: '2-digit', minute: '2-digit' })}`
    : `Today / ${now.toLocaleTimeString(locale, { hour: '2-digit', minute: '2-digit' })}`
}

function buildSnapshotCallout(snapshot: NutritionSnapshot) {
  if (snapshot.remainingCalories != null && snapshot.remainingCalories < 0) {
    return 'You are above the calorie target. Keep the next meal lighter and bias toward lean protein.'
  }

  if (snapshot.remainingProteinG != null && snapshot.remainingProteinG > 0) {
    return 'Protein is still short of target. Add a clean protein source before the day closes.'
  }

  if (snapshot.dayType || snapshot.trainingType) {
    return `Current focus: ${toDisplayLabel(snapshot.trainingType) ?? 'training'} on a ${toDisplayLabel(snapshot.dayType) ?? 'planned'} day.`
  }

  return 'The coach will keep updating this summary as your day plan and intake evolve.'
}

function toDisplayLabel(value: unknown) {
  if (typeof value !== 'string' || !value.trim()) return null
  return value
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, (match) => match.toUpperCase())
}

function formatCalories(value: unknown) {
  return `${formatNullableNumber(value)} kcal`
}

function formatGrams(value: unknown) {
  return `${formatNullableNumber(value)} g`
}

function formatNullableNumber(value: unknown) {
  const numeric = typeof value === 'number' ? value : Number(value)
  if (!Number.isFinite(numeric)) return '--'
  return `${Math.round(numeric)}`
}

function formatSigned(value: number) {
  const rounded = Math.round(value)
  return rounded > 0 ? `+${rounded}` : `${rounded}`
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
