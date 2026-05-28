# FitTrack.React

`FitTrack.React` is the React + Next.js frontend site for FitTrack.

## Developmentr

```bash
npm install
npm run dev
```

The app runs on [http://localhost:3000](http://localhost:3000) by default.

## Environment

Copy `.env.example` to `.env.local` before local development:

```bash
cp .env.example .env.local
```

Configure `NEXT_PUBLIC_COPILOT_PORT` to match the local FitTrack Copilot port. If it is not set, the app defaults to `http://localhost:5097`.

If you need to target a non-local backend, set `NEXT_PUBLIC_API_BASE_URL` instead.

## Docker

```bash
docker build -t fittrack-react .
docker run --env-file .env.local -p 3000:3000 fittrack-react
```

## Playwright

Install the browser once:

```bash
npx playwright install chromium
```

Run the React page tests:

```bash
npm run test:e2e
```

The tests start the Next.js app automatically and mock the Copilot API in the browser, so they do not require the backend to be running.
