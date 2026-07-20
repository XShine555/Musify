# web-player

Musify's web client: SvelteKit 2 (`adapter-node`), Svelte 5 runes, Tailwind 4,
TypeScript. Authentication is server-side OIDC against Zitadel (`openid-client`
and `jose`) with an encrypted session cookie; the backend is called from `load`
functions and form actions through a typed `openapi-fetch` client.

## Development

```sh
npm install
npm run dev      # :5173
```

Configuration lives in `.env` (template in `.env.example`).
`deploy/zitadel/provision.ps1` fills in the Zitadel values automatically, so the
usual flow is to bring the stack up first (`./deploy/up.ps1`) and then run the
dev server.

```sh
npm run check    # svelte-check — must be clean before closing a change
npm run lint     # prettier --check . && eslint .
npm run format   # prettier --write .
npm run build    # production build into build/
npm run gen:api  # regenerate src/lib/api/schema.d.ts from the running API
```

## Production

Built and run as a container by the production stack — see
[../deploy/README.md](../deploy/README.md). There the same variables come from
`deploy/.env.prod` through compose instead of from `.env`.

Conventions and the design system are documented in [CLAUDE.md](CLAUDE.md) and
in the `musify-web` skill.
