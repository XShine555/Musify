# Handoff: Musify — Web Music Player

## Overview

Musify es un reproductor de música web: un player permanente, navegación lateral, y vistas de Home / Descubrir / Playlists / Me gusta / Reciente, más estados de carga, error, vacío y sin resultados. Su rasgo de identidad es que **toda la paleta de acento y los fondos derivan de la canción en reproducción**: al cambiar de pista, el tinte del hero, el glow ambiental, el sidebar, la barra del player, el logo, la barra de progreso y los acentos viran en bloque con una transición de 0.6s.

## About the Design Files

Los archivos de este paquete son **referencias de diseño hechas en HTML** — prototipos que muestran aspecto y comportamiento previstos, **no código de producción para copiar tal cual**. La tarea es **recrear estos diseños en el entorno del codebase destino** (React, Vue, SwiftUI, nativo…) siguiendo sus patrones y librerías ya establecidos. Si todavía no hay entorno, elige el framework más apropiado para el proyecto e implementa allí.

El prototipo está escrito como un componente único con estado local y estilos inline. En producción conviene separarlo en componentes (Sidebar, TopBar, Player, Queue, TrackRow, MediaCard, sections de Home) y mover los tokens a la solución de estilos del proyecto.

## Fidelity

**Alta fidelidad (hifi).** Colores, tipografía, espaciado, radios, estados y microinteracciones son definitivos. Se espera una recreación fiel usando las librerías del codebase. Las carátulas son fotografías genéricas de marcador de posición; el contenido musical (artistas, álbumes, canciones, playlists, duraciones, reproducciones) es **mock data** y debe sustituirse por datos reales.

**Regla de producto:** la UI nunca menciona la procedencia de la música. Sin etiquetas de fuente, proveedor ni "powered by". Para el usuario esto es solo Musify.

---

## Layout global

Altura fija de viewport, sin scroll de página; solo el área de contenido y la cola scrollean.

```
┌──────────┬──────────────────────────────┬──────────┐
│ Sidebar  │ TopBar                       │  Queue   │
│ 254px    ├──────────────────────────────┤  304px   │
│          │ Main (scroll)                │ (258px   │
│          │                              │  <1180)  │
└──────────┴──────────────────────────────┴──────────┘
   Player flotante (absolute, left/right 0, bottom 0, padding 0 16px 16px)
```

- Raíz: `position:relative; height:100vh; overflow:hidden; background: pageBg; transition: background .6s ease`.
- Dos capas decorativas absolutas y no interactivas sobre el fondo: el **ambient glow** y un scrim inferior `radial-gradient(130% 85% at 50% 118%, rgba(0,0,0,.8), transparent 62%)`.
- Fila principal `display:flex; height:100%`.
- El main tiene `padding: 6px 26px` y `padding-bottom` dinámico: `126px` en desktop, `206px` cuando hay barra de pestañas móvil.

### Breakpoints

| Ancho | Comportamiento |
|---|---|
| `>= 1180px` | Sidebar visible (254px), cola 304px, player completo |
| `< 1180px` (tablet) | Sidebar oculto → barra de pestañas bajo el player; cola 258px; padding inferior 206px |
| `< 760px` (móvil) | Cola oculta; se ocultan shuffle/repeat/volumen/letra/cola del player (queda carátula + título + transporte + progreso); el bloque de info del player pasa a `width:auto` |

---

## Sistema de color dinámico

Cada pista lleva un `hue` (0–360) asignado. Todo el color de la app se deriva de ese hue en oklch. Los valores están repartidos por la rueda para que el cambio entre pistas consecutivas sea evidente (paso mínimo ~50°).

```js
// hue = hue de la pista en reproducción
accent      = oklch(0.74 0.145 H)          // acción, barra de progreso, corazón activo, EQ
accentTitle = oklch(0.87 0.07  H)          // títulos sobre superficie tintada
accentMuted = oklch(0.79 0.035 H)          // título de la pista activa en tarjetas
accentSoft  = oklch(0.74 0.145 H / .08)    // fondo de fila/tarjeta activa
accentLine  = oklch(0.74 0.145 H / .2)     // borde de superficie acentuada
accentHair  = oklch(0.74 0.145 H / .09)    // borde de tarjeta activa
accentBtnBg       = oklch(0.74 0.145 H / .15)
accentBtnBgHover  = oklch(0.74 0.145 H / .24)

// superficies
pageBg     = oklch(0.068 0.008 H)
sidebarBg  = linear-gradient(180deg, oklch(0.095 0.012 H), oklch(0.068 0.007 H))
panelBg    = oklch(0.09  0.010 H / .28)     // panel de cola
barBg      = oklch(0.105 0.012 H / .9)      // player y tabs (con backdrop-filter: blur(22px))
hairline   = oklch(0.62  0.030 H / .08)     // todos los separadores

// gradientes: tc(a, l) = oklch(0.34 - l/380 , 0.11 - l/1600 , H / a)
ambient      = radial-gradient(52% 38% at 14% 0%, tc(.26,30), transparent 54%),
               radial-gradient(46% 34% at 78% 0%, tc(.20,40), transparent 56%)
heroBg       = linear-gradient(105deg, tc(.34,42) 0%, tc(.11,70) 28%, tc(.03,88) 50%, rgba(6,6,9,0) 70%)
spotlightBg  = linear-gradient(100deg, tc(.28,46) 0%, tc(.10,74) 30%, tc(.03,88) 52%, rgba(6,6,9,0) 72%)
logoGrad     = linear-gradient(140deg, oklch(0.78 0.15 H), oklch(0.62 0.13 H+40) 92%)
logoGlow     = 0 0 20px oklch(0.74 0.145 H / .3)
```

**Importante:** hero y spotlight terminan en `alpha 0` antes del borde derecho (70–72%) y **no llevan borde**, para fundirse con el fondo sin corte visible.

Los elementos que cambian de color reciben `transition: background .6s ease` (y `box-shadow .6s ease` en el logo): raíz, sidebar, panel de cola, barra del player, tabs.

### Grises fijos (no dependen del hue)

| Rol | Hex |
|---|---|
| Texto principal (títulos de sección, h1/h2) | `#d3d2d6` – `#d9d8dc` |
| Texto de tarjeta / fila | `#cbcacf` / `#cdccd1` |
| Texto base del documento | `#c9c8cc` |
| Secundario | `#8a8a8e`, `#8f8f93`, `#919195` |
| Terciario | `#7f7f83`, `#7d7d81`, `#848488` |
| Cuaternario / contadores | `#6c6c70`, `#696a6d`, `#646468`, `#616165`, `#5c5c60` |
| Iconos inactivos player | `#747478` |
| Botón primario (fondo) | `#c6c4cb`, hover `#d8d7dc`; texto `#0a0a0d` |
| Superficies neutras | `rgba(255,255,255,.022)` base, `.045` hover; bordes `rgba(255,255,255,.04)` / `.075` |

Los grises son **neutros a propósito** (sin tinte), para que solo el acento lleve color.

---

## Tipografía

- Display / títulos: **Sora** 400/500/600/700.
- UI / texto: **Plus Jakarta Sans** 400/500/600/700.
- `-webkit-font-smoothing: antialiased`.

| Uso | Fuente | Tamaño | Peso | Tracking |
|---|---|---|---|---|
| H1 hero | Sora | 36px / 1.1 | 600 | -0.035em |
| H1 vista (Descubrir, Playlists, Tracklist) | Sora | 28–33px | 600 | -0.03 / -0.035em |
| H2 sección | Sora | 17.5px | 600 | -0.025em |
| Título spotlight | Sora | 23px | 600 | -0.03em |
| Wordmark | Sora | 17.5px | 600 | -0.02em |
| Subtítulo de sección | Jakarta | 12px | 400 | — |
| Eyebrow / cabecera de columna | Jakarta | 10.5px uppercase | 600 | 0.12–0.17em |
| Título de tarjeta | Jakarta | 13–13.5px | 600 | -0.01em |
| Fila de canción (título) | Jakarta | 12.5–13px | 500 | — |
| Meta / artista | Jakarta | 11–11.5px | 400 | — |
| Números (duración, índice, reproducciones) | Jakarta | 11–12px | 400 | `font-variant-numeric: tabular-nums` |

Todo texto de una línea en tarjetas y filas usa `white-space:nowrap; overflow:hidden; text-overflow:ellipsis` con `min-width:0` en el contenedor flex. Los titulares llevan `text-wrap: pretty`.

---

## Escalas

**Radios:** 8px (miniaturas pequeñas, chips) · 9–11px (carátulas 36–48px, botones) · 12–13px (filas activas, carátulas de tarjeta) · 14–16px (tarjetas, portadas grandes) · 18px (player, tabs, portada de cabecera) · 22–24px (hero, spotlight) · 50% (avatares, botón de play, artistas) · 999px (chips de sugerencia).

**Sombras:** carátula de tarjeta `0 14px 32px rgba(0,0,0,.45)` · artista `0 12px 28px rgba(0,0,0,.4)` · portada de cabecera `0 24px 56px rgba(0,0,0,.55)` · player `0 20px 56px rgba(0,0,0,.6)` · botón primario hero `0 10px 28px rgba(0,0,0,.45)`.

**Gaps de grid:** tarjetas 20px · artistas 18px · playlists 14–22px · filas compactas 6–14px.

**Anchos mínimos de columna (auto-fill):** mixes/álbumes/descubrir 164px · artistas 132px · playlists (tarjeta ancha) 258px · playlists (índice) 196px · continuar escuchando 268px · populares 330px · géneros 228px.

---

## Pantallas

### 1. Sidebar (`>= 1180px`)

`width:254px; padding:22px 12px 0 20px; border-right:1px solid hairline; background: sidebarBg`.

- **Wordmark**: cuadrado 24px con `logoGrad` + `logoGlow`, gap 11px, texto "Musify". Padding inferior 26px.
- **Nav** (columna, gap 2px). Ítem: `padding:9px 12px; border-radius:10px; font-size:13.5px; weight:500`, icono SVG 17px stroke 1.6, etiqueta flexible, contador a la derecha (11px, `#5c5c60`, tabular).
  - Activo: color `#d5d4d8`, fondo `rgba(255,255,255,.06)`.
  - Inactivo: color `#8b8b8f`, fondo transparente. Hover: fondo `rgba(255,255,255,.04)`, color `#d5d4d8`. Transición `.16s`.
  - Ítems: Inicio · Descubrir · Playlists (7) · Me gusta (312) · Reciente.
- **Separador**: `margin:24px 12px 14px; height:1px; background:rgba(255,255,255,.05)`.
- **Cabecera "TUS PLAYLISTS"** (10.5px uppercase, 0.13em, `#616165`) con un "+" a la derecha que navega al índice de playlists.
- **Lista de playlists** (scroll, gap 1px). Fila: miniatura 34px radio 8px (imagen `opacity:.88`), nombre 12.5px, meta 11px `#6c6c70`. Si esa playlist está sonando, un ecualizador de 3 barras de 2px a la derecha.

### 2. TopBar

`padding:16px 26px 12px; gap:14px`.

- Atrás / adelante: círculos de 30px, fondo `rgba(255,255,255,.035)`, borde `rgba(255,255,255,.05)`, icono 15px. Atrás activo (`#8b8b8f`, hover `#dbdadf`), adelante deshabilitado (`#555559`).
- Buscador: `flex:1; max-width:480px; height:38px; radius:11px`, fondo `rgba(255,255,255,.035)`, borde `rgba(255,255,255,.055)`, lupa 15px `#737377`, input 13px transparente. Placeholder: "Canciones, artistas, álbumes o playlists".
- Spacer.
- Botón "Cola": alto 33px, radio 10px, 12.5px/500, icono 15px. Activo: color `#d6d5da`, fondo `rgba(255,255,255,.06)`. Inactivo: `#8b8b8f`, fondo `rgba(255,255,255,.028)`.
- Avatar 33px circular con borde `rgba(255,255,255,.1)`.

### 3. Home

Ritmo editorial deliberado — no filas iguales encadenadas:

**a) Hero** — `border-radius:24px; margin-bottom:42px; background: heroBg`, sin borde, `padding:38px 36px 34px; max-width:620px`.
- Saludo H1 dinámico por hora y día (ver *Copy dinámico*).
- Blurb 13.5px `#8a8a8e`, `line-height:1.65`, `max-width:440px`.
- Botones: primario "Reproducir" (`#c6c4cb` sobre `#0a0a0d`, 12px 21px, radio 12px, icono play 14px) + secundario "Guardar" (borde `rgba(255,255,255,.11)`), y un meta a la derecha en 12px.
- Tres métricas: valor Sora 26px/600 y etiqueta 11.5px — "Canciones esta semana", "Tiempo de escucha", "Artistas nuevos".

**b) Continuar escuchando** — grid `auto-fill minmax(268px,1fr)`, gap 10px. Fila-tarjeta: `padding:9px 14px 9px 9px; radius:13px`, carátula 46px radio 9px, título 13px/600, sub 11.5px, y un icono play 15px en `accent` que aparece en hover. La pista en curso usa `accentSoft` de fondo, `accentHair` de borde y `accentMuted` en el título.

**c) Playlist destacada (spotlight)** — grid de dos columnas `auto-fit minmax(290px,1fr)`, `radius:22px`, `background: spotlightBg`, sin borde ni divisor central.
- Izquierda: portada 88px radio 14px, eyebrow "PLAYLIST DESTACADA", nombre Sora 23px, meta 12px, blurb 13px/1.7, botón "Reproducir" (`accentBtnBg` + `accentLine` + `accentTitle`, hover `accentBtnBgHover`) y "Mezclar" secundario.
- Derecha: 5 canciones con índice 20px, título 12.5px, artista 11px, duración tabular; fondo `rgba(0,0,0,.26)` que se desvanece a transparente hacia la derecha.

**d) Mixes** — H2 "Mixes" + sub "Generados a partir de lo que más repites". Grid `minmax(164px,1fr)` gap 20px. Tarjeta cuadrada radio 13px con scrim `linear-gradient(180deg,transparent 45%,rgba(0,0,0,.42))` y botón de play circular 38px abajo a la derecha (`rgba(10,10,13,.76)`, blur 8px, borde `rgba(255,255,255,.12)`) que aparece en hover.

**e) Artistas que sigues** — grid `minmax(132px,1fr)` gap 18px, retratos circulares, texto centrado, nombre 12.5px/600, oyentes 11px.

**f) Populares esta semana** — grid de dos columnas `auto-fit minmax(330px,1fr)`, `gap:6px 34px`. Fila: índice/EQ 18px, carátula 38px, título + artista, reproducciones 11px y duración 11.5px.

**g) Tus playlists** — tarjetas anchas `minmax(258px,1fr)` gap 14px: portada 62px radio 11px, nombre 13.5px/600, nº de canciones 11.5px, artistas principales 11px `#696a6d`, play en hover. Enlace "VER TODAS" en la cabecera.

### 4. Descubrir

H1 28px + sub. Mosaico de géneros `minmax(228px,1fr)` gap 14px: tarjetas de 112px, radio 16px, fondo `linear-gradient(140deg, oklch(0.24 0.07 Hgénero), rgba(9,9,12,.95))`, borde `rgba(255,255,255,.05)`, nombre Sora 15.5px, meta 11.5px, y una miniatura de 80px rotada 20° asomando en la esquina inferior derecha (`opacity:.82`). Debajo, "Fuera de tu burbuja" con la misma tarjeta cuadrada de Mixes.

### 5. Tracklist (Me gusta / Reciente / Playlist)

Cabecera: portada 164px radio 18px + eyebrow (`Colección` / `Historial` / `Playlist`), H1 33px, meta 12.5px, botón "Reproducir" y un botón cuadrado 40px de "aleatorio".

Tabla: `grid-template-columns: 32px 1fr 1fr 92px; gap:16px`. Cabecera 10.5px uppercase `#616165` con borde inferior `rgba(255,255,255,.05)`. Fila `padding:10px 14px; radius:12px`:
- Col 1: número o, si es la pista activa, EQ de 3 barras de 2.5px en `accent`.
- Col 2: carátula 40px radio 9px + título 13px/500 + artista 11.5px.
- Col 3: álbum (o `—` si falta).
- Col 4: corazón (14px; visible siempre si está marcado, si no solo en hover) + duración tabular.
- Activa: fondo `accentSoft`, título `accentTitle`. Hover: `rgba(255,255,255,.032)`.

### 5b. Resultados de búsqueda

**Vista propia, no una tracklist.** La búsqueda devuelve canciones, álbumes, artistas y playlists, así que no lleva portada-hero ni columnas de tabla: es una lista agrupada donde cada tipo se reconoce a simple vista.

**Cabecera** (`margin:12px 0 18px`): eyebrow `RESULTADOS DE BÚSQUEDA` (10.5px uppercase, tracking .16em, `#8a8a8e`, 600) · H1 Sora 26px/600 tracking -0.03em `#d9d8dc` con el texto `Resultados para «{query}»` · meta 12.5px `#8a8a8e`: `{n} coincidencias en canciones, álbumes, artistas y playlists` (singular "coincidencia" con 1).

**Chips de filtro** (`display:flex; gap:7px; flex-wrap:wrap; margin-bottom:26px`): `Todo`, `Canciones`, `Álbumes`, `Artistas`, `Playlists`. Cada chip `padding:7px 14px; radius:999px; font-size:12px/500` y lleva a su derecha el contador en 10.5px tabular-nums.
- Activo: fondo `#cfccd9`, texto `#0a0a0d`, contador `rgba(10,10,13,.55)`, sin borde.
- Inactivo: fondo `rgba(255,255,255,.03)`, texto `#9b9b9f`, contador `#65656a`, borde `1px solid rgba(255,255,255,.07)`.
- Transición `background .16s, color .16s`.

**Mejor resultado** (solo con el filtro `Todo`): fila-tarjeta `display:flex; gap:20px; padding:18px; radius:18px; border:1px solid rgba(255,255,255,.06)`, fondo `rgba(255,255,255,.025)` → `.05` en hover, `margin-bottom:34px`. Miniatura 88px con el radio del tipo y `box-shadow:0 14px 34px rgba(0,0,0,.5)`. Eyebrow `Mejor resultado · {Tipo}` 10.5px uppercase tracking .14em en el **tinte del tipo**; título Sora 22px/600 tracking -0.03em `#dcdbe0`; subtítulo 12.5px `#84848a`. A la derecha, botón circular 46px `#c6c4cb` / icono `#0a0a0d`, `opacity .55` → `1` en hover.
Selección: el primer elemento cuyo nombre **empieza** por la consulta, recorriendo Artista → Canción → Álbum → Playlist; si ninguno empieza por ella, el primer resultado del primer grupo no vacío.

**Grupos** (`margin-bottom:30px` cada uno). Cabecera de grupo: título Sora 14.5px/600 `#cfccd9` + regla `1px rgba(255,255,255,.06)` que ocupa el espacio libre + contador 11px `#6e6e74` (`{n} resultados`). Con filtro `Todo` cada grupo muestra **máximo 4** elementos; al filtrar por un tipo se muestran todos los de ese tipo.

**Fila de resultado:** `grid-template-columns: 52px 1fr auto; gap:15px; padding:9px 12px; radius:12px; border-bottom:1px solid rgba(255,255,255,.035)`; fondo transparente → `rgba(255,255,255,.035)` en hover (`.15s ease`), sin elevación.
- Miniatura 52px con degradado de respaldo del hue del elemento y overlay `rgba(6,6,9,.55)` + play 15px `#e2e1e6` que aparece en hover (`opacity 0 → 1`).
- Título 13.5px/500 `#cbcacf` → `#e0dfe4` en hover, seguido del **badge de tipo**: 9.5px uppercase, tracking .1em, 600, `padding:3px 7px; radius:5px`, color del tinte del tipo sobre el mismo tinte al 11%.
- Subtítulo 11.5px `#7f7f83` (una línea, ellipsis).
- Meta a la derecha 11.5px `#76767c` tabular-nums.

**Diferenciación por tipo** — la forma de la miniatura es la señal primaria; el badge la confirma. El tinte se deriva del hue de la canción en reproducción con un desplazamiento fijo, así que la vista sigue siendo cromáticamente coherente con el resto de la app:

| Tipo | Radio de miniatura | Tinte | Subtítulo | Meta |
|---|---|---|---|---|
| Canción | `9px` (cuadrado suave) | `oklch(0.80 0.10 H)` | `artista · álbum` | duración |
| Álbum | `5px` (casi recto) | `oklch(0.80 0.10 H+46)` | `artista · año` | `Álbum` |
| Artista | `50%` (círculo) | `oklch(0.80 0.10 H-52)` | oyentes | `Artista` |
| Playlist | `13px` (muy redondeado) | `oklch(0.80 0.10 H+92)` | artistas principales | `{n} canciones` |

Orden de los grupos: Canciones, Álbumes, Artistas, Playlists. Los grupos vacíos no se renderizan.

**Coincidencias:** canciones por título + artista + álbum; álbumes por título + `artista · año`; artistas por nombre; playlists por nombre + artistas principales. Todo en minúsculas, `includes`.

**Acciones:** canción → fija la pista y reproduce; álbum y artista → reproducen; playlist → abre la vista de playlist con su contexto y limpia la búsqueda.

### 6. Índice de playlists

H1 "Tus playlists" + meta agregada, botón "Nueva playlist" secundario, grid `minmax(196px,1fr)` gap 22px de tarjetas cuadradas con nombre, nº de canciones y artistas principales.

### 7. Cola (aside derecho)

`border-left:1px solid hairline; background: panelBg; padding:20px 16px` + padding inferior dinámico.
- Cabecera "En cola" (Sora 14.5px) + cerrar "×".
- "REPRODUCIENDO": tarjeta con fondo `accentSoft`, borde `accentLine`, carátula 40px, título en `accentTitle`.
- Cabecera "A CONTINUACIÓN · {playlist}" con `gap:12px`; la etiqueta trunca con elipsis (`flex:1 1 auto; min-width:0; nowrap`) y "Vaciar" es `flex:0 0 auto`.
- Lista scrollable: miniatura 36px, título 12px, artista 11px, duración.

### 8. Player (siempre visible)

Barra flotante: `radius:18px; padding:11px 16px; background: barBg; backdrop-filter: blur(22px); border:1px solid hairline; box-shadow:0 20px 56px rgba(0,0,0,.6)`; `pointer-events:none` en el contenedor y `auto` en la barra.

- **Izquierda (256px):** carátula 48px radio 11px, título 13px/600, artista · álbum 11.5px, corazón 16px (activo en `accent`).
- **Centro (flex:1):** shuffle · anterior · play/pausa · siguiente · repetir, gap 14px. Play: círculo 40px `#c6c4cb` con icono `#0a0a0d`, hover `#d6d5da` (**sin scale**). Shuffle/repeat activos en `accent`, inactivos `#747478`.
  Progreso: tiempo transcurrido (10.5px tabular, ancho mín. 32px) · pista `rgba(255,255,255,.09)` de 3.5px con relleno en `accent` · duración. Clic en la pista = seek proporcional.
- **Derecha (256px):** letra, volumen (icono + pista de 84px con relleno `#8e8c98`), y botón de cola. Oculto por completo en móvil.

### 9. Barra de pestañas (`< 1180px`)

Bajo el player, `margin-top:10px; padding:7px; radius:18px`, mismo `barBg` y blur. Icono 17px + etiqueta 10px, distribuidos con `space-around`. Muestra los 5 ítems de navegación.

---

## Estados

- **Loading (skeleton):** bloque de hero de 260px + 2 secciones de 6 tarjetas. Gradiente `linear-gradient(90deg, rgba(255,255,255,.025), .055, .025)` con `background-size:420px 100%` y `animation: shim 1.25s linear infinite` (`-420px 0` → `420px 0`). Se muestra 900ms al arrancar.
- **Error:** cuadro 52px con "!" (`rgba(226,130,130,.08)` / borde `.18` / texto `#e09292`), título 20px, cuerpo 13px, botones "Reintentar" (primario) y "Ver descargas". Copy: "No se pudo cargar tu música" / "La conexión se interrumpió. Lo que tengas descargado sigue disponible sin conexión." Reintentar vuelve al skeleton 800ms.
- **Vacío:** icono 70px radio 19px con degradado tenue, título 20px, cuerpo 13px, CTA primario + "Volver al inicio". Dos variantes de copy: playlists sin crear y "me gusta" sin canciones.
- **Sin resultados:** se muestra cuando **ningún** tipo tiene coincidencias (canciones + álbumes + artistas + playlists = 0). Título `Sin resultados para "{query}"`, cuerpo de ayuda y 4 chips de sugerencia (radio 999px, borde `rgba(255,255,255,.08)`, 12px).
- **Datos incompletos:** si falta el álbum, la columna muestra `—` y el player omite el `· álbum`; si falta el subtítulo de una tarjeta, "Sin información". Toda carátula tiene un degradado de respaldo detrás de la imagen.

---

## Interacciones

| Elemento | Comportamiento |
|---|---|
| Nav / tabs | Cambia de vista y limpia la búsqueda |
| Playlist (sidebar, tarjeta, índice, spotlight) | Abre la vista de playlist con su contexto |
| Tarjeta / fila de canción | Fija la pista, `pos = 0`, `playing = true` |
| Play/pausa | Alterna reproducción (el reloj avanza 1s por segundo) |
| Siguiente / anterior | Cíclicos sobre el array de pistas, resetean la posición |
| Pista de progreso | Clic → `pos = (x / ancho) * duración` |
| Pista de volumen | Clic → `vol` entre 0 y 1 |
| Corazón (fila o player) | Alterna en el mapa `liked` |
| Shuffle / repeat | Alternan, se tiñen de `accent` |
| Botón "Cola" | Abre/cierra el aside |
| Buscar | Filtra canciones, álbumes, artistas y playlists → vista de resultados agrupada; vacío → vuelve a la vista normal; 0 coincidencias en total → estado "sin resultados" |
| Chip de filtro de resultados | Fija `sfilter`; `Todo` recorta cada grupo a 4, un tipo concreto muestra todos |
| Nueva playlist (sidebar / índice) | Abre el modal de crear playlist |

**Animación — reglas:**
- **Nada de elevación en hover.** Se eliminó a propósito: el feedback es solo un cambio de fondo y la aparición del botón de play.
- Hover de fondo/color: `.15–.18s ease`. Play del player: `.14s` solo de color, sin `scale`.
- Cambio de tinte por canción: `.6s ease`.
- Entrada de vista: `rise` (`opacity 0 → 1`, `translateY(12px) → 0`) en `.3s ease`. Cola: `.22s`.
- EQ de pista activa: `eq` `.9s ease-in-out infinite` alternando `scaleY(.25)` y `scaleY(1)`, con desfases de 0.25/0.5s (0.3/0.6s en el sidebar).

---

## Estado necesario

```
view        'home' | 'explore' | 'playlists' | 'playlist' | 'liked' | 'recent'
listCtx     nombre de la playlist abierta
idx         índice de la pista en reproducción
playing     boolean
pos         segundos transcurridos (intervalo de 1s, vuelve a 0 al llegar a la duración)
vol         0..1
queueOpen   boolean (forzado a false por debajo de 760px)
query       texto de búsqueda
sfilter     'Todo' | 'Canciones' | 'Álbumes' | 'Artistas' | 'Playlists'  (filtro de resultados)
liked       { [trackId]: true }
shuffle / repeat  boolean
hover       clave del elemento con hover (o null)
w           ancho de ventana (listener de resize)
demoState   'normal' | 'loading' | 'error' | 'empty'  (solo para demostrar estados)
```

### Datos esperados

`Track { id, title, artist, album?, duration, cover?, hue, plays? }` ·
`Album { title, artist, year, cover? }` ·
`Artist { name, listeners, photo? }` ·
`Playlist { name, trackCount, topArtists, cover?, isPlaying? }`

Todos los campos opcionales deben tener degradado o texto de respaldo. El `hue` debería calcularse en backend desde la carátula (color dominante, con saturación mínima garantizada) y venir en el payload; en el prototipo está precalculado por pista.

---

## Modal: crear playlist

Diseño aparte en `Crear playlist.dc.html`. Se invoca desde "Nueva playlist" (sidebar e índice de playlists).

**Scrim:** `rgba(4,4,6,.55)` + `backdrop-filter: blur(3px)` sobre el fondo `radial-gradient(120% 90% at 50% 0%, #15131c, #08080b 58%, #050507)`. Modal centrado con `display:grid; place-items:center; padding:40px 20px`.

**Contenedor:** `max-width:560px; radius:24px; border:1px solid rgba(255,255,255,.075)`, fondo `linear-gradient(180deg,#141419,#0d0d11 62%,#0b0b0e)`, `box-shadow:0 32px 90px rgba(0,0,0,.66)` + `inset 0 2px 0 rgba(255,255,255,.035)`. Entrada `modalIn .28s cubic-bezier(.2,.8,.3,1)` (`opacity 0 → 1`, `translateY(14px) scale(.985)` → sin transformar).

**Aura:** capa absoluta no interactiva `radial-gradient(110% 60% at 18% -10%, oklch(.55 .13 H / .16), transparent 62%)`, con `transition: background .5s` — vira con el acento elegido.

**Header** (`padding:24px 26px 0`): eyebrow `TU BIBLIOTECA` 10.5px uppercase tracking .16em `#8f8f93`; título Sora 21px/600 tracking -0.03em `#d8d7db` "Crear playlist". Cerrar: 32px, radio 10px, borde `rgba(255,255,255,.06)`, fondo `rgba(255,255,255,.025)` → `.07` y color `#87878c` → `#d0cfd4` en hover.

**Portada** (columna izquierda, 124px):
- Cuadrado 124px radio 16px, `linear-gradient(150deg, oklch(.66 .16 H), oklch(.34 .11 H) 58%, #0e0e13)`, borde `rgba(255,255,255,.07)`, `box-shadow:0 14px 34px oklch(.5 .13 H / .22)`, con el icono de nota 26px `rgba(255,255,255,.62)` centrado. Transición `.45s` al cambiar de acento.
- **Selector de acento:** 5 franjas `flex:1; height:20px; radius:7px` (gap 6px, `margin-top:11px`), cada una `linear-gradient(145deg, oklch(.66 .16 h), oklch(.38 .11 h))`. Hues: Ámbar 68, Violeta 288 (por defecto), Azul 244, Verde 158, Rojo 22. Seleccionada: `box-shadow:0 0 0 1.5px rgba(255,255,255,.85), 0 0 0 4px rgba(0,0,0,.5)`; el resto `0 0 0 1px rgba(255,255,255,.07)`.
- **Subir imagen:** botón de texto 11px `#8a8a8f`, `padding:7px 0; radius:9px`, borde `1px dashed rgba(255,255,255,.1)`; hover texto `#cfccd9` y borde `rgba(255,255,255,.2)`.

**Campos** (columna derecha, `gap:20px` respecto a la portada):
- Etiquetas 11px/600 `#87878c`.
- Nombre: `padding:12px 14px; radius:12px`, fondo `rgba(255,255,255,.035)`, borde `rgba(255,255,255,.075)`, texto **Sora 16px/600** tracking -0.02em `#dcdbe0`, placeholder "Playlist sin título" `#5d5d63`, `maxlength 60`. Foco: borde `rgba(255,255,255,.2)`, fondo `.055`. Contador `{n}/60` 10.5px `#65656a` alineado a la derecha.
- Descripción (opcional): `textarea` 3 filas, `resize:none`, 12.5px/1.6 `#c4c3c9`, mismo tratamiento de superficie y foco. Placeholder "Para qué sirve esta playlist, cuándo la escuchas…".

**Opciones** (`padding:20px 26px 0`, dos bloques `radius:13px; padding:13px 15px`, fondo `rgba(255,255,255,.026)`, borde `rgba(255,255,255,.055)`, gap 9px):
- **Visibilidad** — segmentado de 2 (`Privada` por defecto / `Pública`) en un riel `radius:10px` `rgba(0,0,0,.34)`; opción activa fondo `#cfccd9` texto `#0a0a0d`, inactiva `#8e8e94`. El texto de ayuda cambia: "Solo tú la ves en tu biblioteca" / "Aparece en tu perfil y se puede compartir".
- **Colaborativa** — toggle 40×23px, riel `rgba(255,255,255,.12)` → `oklch(.68 .15 H)` al activar, perilla 17px `#0c0c10` con `translateX(17px)`, `.2s cubic-bezier(.3,.8,.3,1)`. Ayuda: "Quien tenga el enlace puede añadir canciones". Toda la fila es clicable.

**Footer** (`border-top:1px solid rgba(255,255,255,.055)`, fondo `rgba(0,0,0,.2)`, `padding:17px 26px 20px`): nota 11px `#6e6e74` "Podrás añadir canciones desde cualquier pantalla."; a la derecha "Cancelar" (secundario, borde `rgba(255,255,255,.1)`, `#a9a7b2`) y "Crear playlist" (primario con icono +).

**Validación:** el CTA solo se habilita con nombre no vacío (`trim`). Deshabilitado: fondo `rgba(255,255,255,.08)`, texto `#6c6c72`, `cursor:not-allowed`. Habilitado: `#c6c4cb` sobre `#0a0a0d`.

**Confirmación:** al crear, toast fijo abajo-centro (`bottom:34px`, `radius:13px`, fondo `#16161c`, borde `rgba(255,255,255,.09)`, `box-shadow:0 18px 46px rgba(0,0,0,.6)`, `fadeIn .22s`) con un cuadrado 26px del degradado elegido y el texto «{nombre}» creada. Se oculta a los 2.6s.

**Estado:** `{ name, desc, hue: 0..4, vis: 'Privada'|'Pública', collab: boolean, created: boolean, createdName }`.
**Salida esperada:** `POST /playlists { name, description, coverHue, visibility, collaborative }` → redirigir a la playlist recién creada.

---

## Copy dinámico

Saludo del hero según hora y día de la semana:

- `< 5h` → "¿Sesión de madrugada, {nombre}?"
- `< 13h` → "Buenos días, {nombre}"
- `< 20h` → "Buenas tardes, {nombre}" (miércoles → "Feliz miércoles"; viernes → "Feliz viernes")
- `>= 20h` → "Buenas noches, {nombre}"
- Sábado/domingo → "Que tengas buen finde, {nombre}" (o "¿Madrugada de finde, {nombre}?" antes de las 5h)

---

## Assets

- **Carátulas y retratos:** marcadores de posición fotográficos servidos por `picsum.photos` con una semilla por elemento, pintados como `background-image` (no `<img>`) para evitar peticiones fallidas mientras se resuelven los datos. Sustituir por las imágenes reales del catálogo.
- **Iconos:** SVG inline, `stroke-width` 1.6–1.8, `stroke-linecap:round`. Reemplazables por el icon set del codebase.
- **Fuentes:** Sora y Plus Jakarta Sans (Google Fonts).
- Sin logotipos de terceros. El wordmark de Musify es tipográfico + el cuadrado con degradado.

## Files

- `Crear playlist.dc.html` — modal de creación de playlist (diseño aparte).
- `Musify.dc.html` — prototipo completo (plantilla + lógica + datos de ejemplo). Contiene todas las vistas y estados; el selector `demoState` permite ver loading / error / empty.
- `support.js` — runtime del prototipo. **No portar**: solo hace funcionar el HTML de referencia.
- `screenshots/` — capturas de referencia (viewport ~924px, es decir en modo tablet: sidebar plegado a barra de pestañas inferior):
  - `01-home-hero.png` … `04-home-populares-playlists.png` — recorrido vertical de Home
  - `05-descubrir.png`, `06-playlists.png`, `07-tracklist-me-gusta.png`, `08-sin-resultados.png`
  Nota: las capturas son anteriores al rediseño de la vista de resultados de búsqueda (5b) y al modal de crear playlist; para esos dos, la referencia son los archivos HTML y este documento.
  Todas con "Yankee" sonando, de ahí el tinte ámbar: con otra pista el mismo layout se ve en otro color.
