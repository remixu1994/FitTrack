export default function Home() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top_left,_rgba(37,99,235,0.26),_transparent_30%),linear-gradient(180deg,_#07111f_0%,_#050b14_100%)] px-6 text-white">
      <div className="max-w-3xl text-center">
        <p className="text-xs uppercase tracking-[0.45em] text-cyan-300">FitTrack</p>
        <h1 className="mt-4 text-5xl font-semibold">Next.js workspace for the .NET 10 coaching backend</h1>
        <p className="mx-auto mt-6 max-w-2xl text-base text-slate-300">
          The frontend now lives separately from the ASP.NET host. Sign in, start from Today, then move into guided nutrition,
          training, and review workflows backed by the coaching API.
        </p>
        <div className="mt-8 flex justify-center gap-4">
          <a href="/login" className="rounded-full bg-cyan-300 px-6 py-3 text-sm font-semibold uppercase tracking-[0.3em] text-slate-950">
            Login
          </a>
          <a href="/today" className="rounded-full border border-white/10 px-6 py-3 text-sm uppercase tracking-[0.3em] text-white">
            Open Today
          </a>
        </div>
      </div>
    </main>
  )
}
