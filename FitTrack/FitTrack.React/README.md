# FitTrack.React

`FitTrack.React` is the React + Next.js frontend site for FitTrack.

## Development

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

Configure `NEXT_PUBLIC_API_BASE_URL` to point at the FitTrack Copilot API. If it is not set, the app defaults to `https://localhost:7291`.

## Docker

```bash
docker build -t fittrack-react .
docker run --env-file .env.local -p 3000:3000 fittrack-react
```
