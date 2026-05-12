import { ChatView } from '@/components/chat/chat-view'
import { AppShell } from '@/components/layout/app-shell'

type ChatPageProps = {
  searchParams?: Promise<{
    draft?: string | string[]
  }>
}

export default async function ChatPage({ searchParams }: ChatPageProps) {
  const resolvedSearchParams = (await searchParams) ?? {}
  const initialDraft = typeof resolvedSearchParams.draft === 'string' ? resolvedSearchParams.draft : ''

  return (
    <AppShell title="Fitness Coach" hideHeader immersive>
      <ChatView initialDraft={initialDraft} />
    </AppShell>
  )
}
