import type { Metadata } from 'next'

import { AppProvider } from '@/components/providers/app-provider'

import './globals.css'

export const metadata: Metadata = {
  title: 'FitTrack React',
  description: 'Next.js frontend for the FitTrack .NET 10 agent backend',
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en">
      <body>
        <AppProvider>{children}</AppProvider>
      </body>
    </html>
  )
}

