import { ChatView } from '@/components/chat/chat-view'
import { AppShell } from '@/components/layout/app-shell'

export default function ChatPage() {
  return (
    <AppShell title="Fitness Coach" hideHeader immersive>
      <ChatView />
    </AppShell>
  )
}
