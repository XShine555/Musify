# Musify — Frontend web

Web pública de Musify. Para diseño y recetas de UI usa el skill **`musify-web`**.

## Stack

SvelteKit 2 (`adapter-node`) · Svelte 5 **runes** · Tailwind 4 · TypeScript strict · auth OIDC Zitadel (`openid-client` + `jose`).

No hay `svelte.config.js` (config inline en `vite.config.ts`) ni `tailwind.config.js` (config en `src/routes/layout.css`).

## Comandos

```sh
npm run dev      # servidor de desarrollo (puerto 5173)
npm run build
npm run check    # svelte-check — debe salir 0 errores antes de cerrar un cambio
```

## Reglas

- **Sin comentarios** en el código salvo que sean imprescindibles.
- **Svelte 5 runes siempre:** `$props()` con `interface Props`, `$state`, `$derived`, `$effect`. Nada de `export let` ni `$:`. `{#each}` siempre con clave.
- **Server-only bajo `$lib/server/`**; nunca importado desde cliente.
- Datos SSR vía `load` en `+page.server.ts`/`+layout.server.ts` → prop `data`.
- Enlaces a endpoints `+server.ts` (login/logout) llevan `data-sveltekit-reload`.
- Reutiliza los tokens/recetas del sistema de diseño; extrae a `$lib/components/ui/` lo que se repita.

## Auth

`hooks.server.ts` valida la cookie de sesión cifrada (`mf_session`), refresca el `access_token` y puebla `locals.user` / `locals.accessToken`. Para proteger una ruta, comprueba `locals.user` en su `load` y redirige a `/login?returnTo=…`. Para el backend, envía `locals.accessToken` como Bearer hacia `API_BASE_URL`.

Variables de entorno en `.env` (ver `.env.example`): `ZITADEL_*`, `AUTH_REDIRECT_URI`, `AUTH_POST_LOGOUT_URI`, `SESSION_SECRET`, `API_BASE_URL`.
