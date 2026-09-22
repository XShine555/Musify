# Musify — web-player

Web pública de Musify (carpeta `web-player/`). Para diseño y recetas de UI usa el skill **`musify-web`**.

## Stack

SvelteKit 2 (`adapter-node`) · Svelte 5 **runes** · Tailwind 4 · TypeScript strict · auth OIDC Zitadel (`openid-client` + `jose`).

No hay `svelte.config.js` (config inline en `vite.config.ts`) ni `tailwind.config.js` (config en `src/routes/layout.css`).

## Comandos

```sh
npm run dev         # servidor de desarrollo (puerto 5173)
npm run build
npm run check       # svelte-check — debe salir 0 errores antes de cerrar un cambio
npm run format      # prettier --write .
npm run lint        # prettier --check . && eslint . && npm run lint:tokens
npm run lint:tokens # scripts/check-tokens.mjs — guardrail del sistema de diseño (ver abajo)
```

## Reglas

- **Sin comentarios** en el código salvo que sean imprescindibles.
- `npm run lint` y `npm run check` deben salir limpios antes de cerrar un cambio.
- **Svelte 5 runes siempre:** `$props()` con `interface Props`, `$state`, `$derived`, `$effect`. Nada de `export let` ni `$:`. `{#each}` siempre con clave.
- **Server-only bajo `$lib/server/`**; nunca importado desde cliente.
- Datos SSR vía `load` en `+page.server.ts`/`+layout.server.ts` → prop `data`.
- Enlaces a endpoints `+server.ts` (`/auth/login`, `/auth/logout`) llevan `data-sveltekit-reload`.
- Reutiliza los tokens/recetas del sistema de diseño; extrae a `$lib/components/ui/` lo que se repita.

## Estilos

El sistema de diseño vive en `src/routes/layout.css` (tokens `@theme` + recetas `@utility`) y `src/lib/theme/` (`theme.css` para neutros estáticos, `tokens.ts` para los tokens dinámicos por hue de usuario). `scripts/check-tokens.mjs` (`npm run lint:tokens`, parte de `lint`) falla si `src/**/*.svelte` reintroduce lo que el refactor de `docs/frontend-refactor-plan.md` (P0–P14) eliminó — léelo antes de añadir un valor arbitrario o un radio/tamaño "de fábrica".

- **Radios:** siempre `rounded-tag/thumb/control/art/art-lg/panel/panel-lg` (o `rounded-full` para píldoras/círculos). Nunca `rounded-sm/md/lg/xl/2xl/3xl` ni `rounded` a secas.
- **Tamaños cuadrados:** `size-*` en vez de `h-N w-N` repetido (icon: `size-icon-xs/sm/md/lg/xl`; portadas: `size-cover-xs…hero`).
- **Tipografía:** recetas `text-display-1/2/3/4`, `text-eyebrow`, `text-body`, `text-count`; tracking con `tracking-display`/`tracking-eyebrow`, no `tracking-[…]` suelto.
- **Espaciado:** escala de Tailwind en pasos de `.5` (evita cuartos como `.25`/`.75`; solo aceptables si el token semántico lo pide, p. ej. `mb-4.5` en `SectionHeading`).
- **Colores:** siempre `--mf-*`/`text-*`/`bg-*`/`border-*` de `theme.css`/`tokens.ts`. Nunca hex ni `rgba()` literales en un `.svelte` (esos literales solo existen como fuente de verdad dentro de `theme.css`/`tokens.ts`).
- **`style="…"` y arbitrarios `-[…]`:** solo la lista blanca de B.4 en `docs/frontend-refactor-plan.md` (stagger `--i`, hue de tile, posición de menú, anchos/alturas dinámicos vía `var(--mf-*)`, `text-[clamp(…)]`, `grid-cols-[auto_1fr]`, `max-h-[85dvh]`, paddings en `vh` para centrado vertical). Todo lo demás, a un token.
- Componentes base a reutilizar antes de crear uno nuevo: `Artwork`, `Avatar`, `ListRow`, `MediaIdentity`, `TrackList`, `MediaCard`, `PageHeader`, `ContextMenu`, `ConfirmDialog`, `PlayButton`, `Slider`, `Logo`, `Chip`, `SegmentedControl`, `CoverForm`.
- Gaps conocidos y aceptados (documentados en `scripts/check-tokens.mjs`): `strokeWidth={n}` en iconos no está unificado (13 valores distintos repartidos por casi todos los componentes; migrarlo a las recetas `stroke-thin/regular/bold` de forma segura requeriría poder verificar visualmente que `lucide-svelte` respeta `stroke-width` por CSS, así que se dejó fuera de esta pasada).

## Auth

`hooks.server.ts` valida la cookie de sesión cifrada (`mf_session`), refresca el `access_token` y puebla `locals.user` / `locals.accessToken`. Para proteger una ruta, comprueba `locals.user` en su `load` y redirige a `/login?returnTo=…`. Para el backend, envía `locals.accessToken` como Bearer hacia `API_BASE_URL`.

Variables de entorno en `.env` (ver `.env.example`): `ZITADEL_*`, `AUTH_REDIRECT_URI`, `AUTH_POST_LOGOUT_URI`, `SESSION_SECRET`, `API_BASE_URL`.

## Backend API

Cliente tipado con `openapi-fetch` sobre el OpenAPI de `Musify.Api`. En un `load`/action server:

```ts
import { createApiClient } from '$lib/server/api';
const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
const { data, error: err } = await api.GET('/tracks');
```

Devuelve `{ data, error }` (estilo ErrorOr), no lanza. Los tipos viven en `src/lib/api/schema.d.ts` y se regeneran con `npm run gen:api` (requiere la API dev levantada en `API_BASE_URL`, que expone `/openapi/v1.json` solo en Development).
