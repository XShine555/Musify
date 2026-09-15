# Plan — Unificación del sistema de diseño y limpieza del front (web-player)

> Este documento es un **plan de trabajo para otro agente/IA**, no un informe de lo ya
> hecho. Recoge conocimiento concreto (bugs reales, patrones, cifras) obtenido durante
> una sesión larga de ajustes de UI, para que quien continúe no tenga que re-descubrirlo.
> No es necesario ejecutar todo de una vez: está pensado para trabajarse página por
> página o componente por componente, en PRs pequeños y verificables.

> **Estado (sesión de seguimiento posterior):** §1–§3 y §5 de este documento (hex
> literales, tamaños de fuente arbitrarios, menú "de cristal" duplicado, componentes
> huérfanos listados) ya están resueltos. El problema había reaparecido con otra forma:
> valores de opacidad sueltos sobre `white`/`black` (`bg-white/[6%]`, `border-white/10`,
> etc.) fuera de los tokens `--mf-surface*`/`--mf-border*`, sombras/gradientes de portada
> duplicados sin token, un bloque de "cabecera de colección" reimplementado 5 veces, un
> menú de cuenta duplicado en `TopBar`/`MobileHeader`, y textos de estado vacío con
> redacción inconsistente ("Aún no..." vs "Todavía no..."). Todo eso se corrigió: se
> añadieron tokens `--shadow-cover-lg/md/sm/xs` y `--mf-liked-grad` a
> `layout.css`/`theme.css`, se migraron los literales `white`/`black` a
> `bg-surface*`/`border-line*`/`hover:bg-hover`/`text-on-art` según si el color está
> sobre el "chrome" de la app o sobre arte/gradiente saturado, se extrajeron
> `lib/components/ui/CollectionHeader.svelte` (portada + título + meta + acciones, usado
> en `AlbumHeader`, `PlaylistHeader`, `liked/+page.svelte`, `mixes/[id]/+page.svelte`,
> `albums/external/.../+page.svelte` y `u/[id]/+page.svelte`) y
> `lib/components/ui/AccountMenu.svelte` (estado de apertura + panel del menú de cuenta,
> usado en `TopBar`/`MobileHeader`), y se corrigió `--color-hover` en `layout.css`, que
> estaba hardcodeado en vez de apuntar a `--mf-surface-2` (rompía el modo claro para
> cualquier `hover:bg-hover`).
>
> **Repaso posterior a la verificación visual** (ya con sesión autenticada): se detectó
> que el fix de §2 anterior para la tarjeta de género de `explore/+page.svelte` estaba
> mal — usaba `var(--mf-ink)` como extremo oscuro del degradado, pero `--mf-ink` es el
> token de "texto sobre botón de acento" y se invierte a blanco en modo claro, rompiendo
> la tarjeta (fondo blanco en vez de degradado oscuro). Se creó un token nuevo,
> `--mf-tile-shade` (deliberadamente **sin** variante de modo claro, igual que
> `--mf-cover-grad`/`--mf-liked-grad`, porque es "arte" decorativo, no "chrome"), y se usa
> ahí en su lugar. También se cambió la línea de meta de `CollectionHeader` de
> `text-muted` a `text-fg-2` (mejor contraste en modo claro; `text-muted` rondaba ~3:1,
> por debajo de WCAG AA para texto normal) y se hizo `actions` opcional para poder migrar
> páginas sin acciones condicionales (como `u/[id]/+page.svelte`) sin renderizar un
> `<div>` vacío.
>
> Pendiente de quien retome esto: `lib/components/ui/Rail.svelte` no tiene importadores
> (confirmado con el mismo método de §5, no porque §5 lo listara — ese hallazgo es nuevo)
> — no se tocó porque no forma parte de este barrido, pero conviene confirmarlo a mano y
> borrarlo si procede. También queda pendiente repetir la verificación visual (§6) sobre
> los cambios de este repaso: tarjetas de género de Explorar, cabeceras de colección
> (contraste de la línea de meta) y `u/[id]/+page.svelte`.

## 0. Objetivo

Tres problemas, en orden de aparición histórica pero **no** de prioridad recomendada:

1. **Colores fijos en vez de variables**: muchas páginas y componentes escriben colores
   literales (`text-[#8a8a8e]`, `bg-[#c6c4cb]`, tamaños `text-[13px]` sueltos, etc.) en
   vez de usar los tokens del sistema (`text-fg-2`, `bg-surface`, la escala tipográfica
   establecida). Esto es lo que impide que cosas como el modo claro/oscuro (ver §2)
   funcionen de forma consistente, y hace que cada página tenga ligeras variaciones de
   gris que deberían ser el mismo tono.
2. **Falta de componentización**: hay patrones (tarjeta de track, fila de "canción
   reciente", menú desplegable tipo cristal, etc.) reimplementados a mano en varias
   páginas en vez de reusar (o crear) un componente compartido. Algunos componentes ya
   existentes ni siquiera se usan donde deberían.
3. **Código muerto**: imports sin usar, componentes sin importadores, clases residuales.

**Referencia canónica**: la página de Inicio (`src/routes/+page.svelte`) es el patrón a
seguir para el modo oscuro — sus tonos, jerarquía tipográfica y look general están bien
ajustados (fue la página más iterada). Cuando una página no tenga claro qué tono/tamaño
usar para algo, mirar cómo lo resuelve Inicio para lo mismo, **no inventar uno nuevo**.
Ojo: Inicio también usa hex sueltos en vez de tokens en varios sitios (ver §3) — es la
referencia de **valores/jerarquía visual**, no de "cómo escribir la clase". Cuando se
migre Inicio a tokens, los valores resultantes deben ser los mismos que ya tiene hoy.

---

## 1. Limitación conocida: modo claro ("Modo blanco") incompleto

Se añadió un toggle de tema (`$lib/theme/mode.svelte.ts`, menú de cuenta en
`TopBar.svelte` / `MobileHeader.svelte`) que pone `data-theme="light"` en `<html>` y
persiste la preferencia en `localStorage` (`musify.theme`).

**Lo que funciona:** los tokens semánticos definidos como CSS puro en
`$lib/theme/theme.css` (`--mf-text*`, `--mf-surface*`, `--mf-border*`, `--mf-track`,
`--mf-scrim`, `--mf-ink`) tienen su contrapartida en el bloque
`:root[data-theme='light'] { ... }` al final de ese archivo, y los tokens dinámicos por
hue (`--mf-bg`, `--mf-elevated`, `--mf-sidebar-bg`, `--mf-panel-bg`, `--mf-bar-bg`,
`--mf-hairline`) se regeneran correctamente en modo claro dentro de
`buildThemeTokens(hue, mode)` (`$lib/theme/tokens.ts`).

**Gotcha importante para quien toque esto:** `+layout.svelte` reaplica esos tokens
dinámicos como **estilos inline** sobre `document.documentElement` en un `$effect` que
corre en cada cambio de hue (cada canción). Un estilo inline siempre gana sobre
cualquier regla de una hoja de estilos, así que **una variable que se anima por hue no
se puede sobrescribir con `:root[data-theme='light'] { --mf-x: ... }`** — hay que
añadirla dentro de `buildThemeTokens()` con una rama `light ? ... : ...`, no en
`theme.css`. Si en el futuro se anima algo más por hue, aplica la misma regla.

**Lo que NO funciona:** todo componente que usa un color hardcodeado (ver §3) no se
entera del cambio de tema, porque no está leyendo ningún token. En la práctica esto
significa que hoy el modo claro cambia correctamente el lienzo de fondo, la sidebar, los
paneles tipo cristal (menús, modal) y cualquier texto/superficie que ya use
`text-fg`/`bg-surface`/etc., pero deja con contraste pobre o directamente ilegible
cualquier texto en hex literal (la mayoría de títulos, subtítulos y metadatos de la
app hoy).

**No hay forma barata de arreglar esto sin la migración de §3.** El modo claro real
depende, técnicamente, en un 100%, de terminar esa migración. No merece la pena
"parchear" el modo claro sin más — es la misma tarea.

---

## 2. Inventario de bugs/patrones ya encontrados (usar como guía de búsqueda)

Estos son bugs concretos que se repitieron varias veces en distintos componentes esta
sesión. Antes de tocar cada página, buscar estos mismos patrones ahí:

### 2.1 Tamaño de fuente que falta → hereda 16px del navegador

Varios componentes tenían un texto sin ninguna clase `text-*`, lo que hace que use el
tamaño por defecto del navegador (16px) en vez de la escala real de la app (que va de
`10.5px` a `14px` en casi todo). Ya se corrigió en `MediaCard.svelte`,
`TrackTitleCell.svelte` y `MenuItem.svelte`. **Sigue sin corregir** en
`TrackContextMenu.svelte` (usa `text-base`/`text-sm` sueltos igual que tenía
`MenuItem` antes de arreglarlo — mismo fix: bajar a la escala `13–13.5px` y los iconos
de `h-6 w-6`/`h-5 w-5` a `h-[18px] w-[18px]` o similar).

Cómo detectarlo: cualquier `<div>`/`<span>` que pinte texto de usuario (título, nombre,
label) sin una clase `text-[…]`/`text-sm`/`text-xs` explícita es sospechoso.

### 2.2 Conflicto de clases Tailwind que se resuelve por orden interno, no por el HTML

Tailwind no respeta el orden en que escribes las clases en el `class="…"`; resuelve
conflictos de la misma propiedad (`background-color`, `font-weight`, etc.) por el orden
en que esas utilidades existen en su propia hoja de estilos generada. Esto causó bugs
reales y confusos:

- Un botón con `bg-white/[6%] ... {isCurrent ? 'bg-accent-tint' : ''}` — con `isCurrent`
  true, el fondo **seguía viéndose** `bg-white/[6%]` porque esa clase "ganaba" en el
  cascade aunque apareciera antes en el HTML.
- `Button.svelte` fuerza `font-semibold` en su clase base; para poner un botón en peso
  normal (`variant="secondary"` con texto normal) hay que usar `class="!font-normal"`
  (con el prefijo `!` de Tailwind, que sí fuerza especificidad), un `class="font-normal"`
  a secas no habría ganado.

**Regla a aplicar en la auditoría**: cualquier sitio donde dos clases de la misma
propiedad CSS convivan condicional + incondicionalmente en el mismo elemento (patrón
`class="base-x ... {cond ? 'other-x' : ''}"`) hay que reescribirlo para que sea
mutuamente excluyente (`class="{cond ? 'a' : 'b'}"`), nunca acumulativo. Y cualquier
override de una clase base de un componente compartido (`Button`, etc.) debe usar `!`
si toca la misma propiedad que ya fija la clase base.

### 2.3 Iconos con brillo verde/acento en `:hover` de forma inconsistente

Varias tarjetas tenían `group-hover:text-accent-soft` en el título para dar un hover
"bonito", pero era inconsistente con el resto de la app (la mayoría de textos no
cambian de color al hover, solo cuando están realmente "activos"/reproduciéndose). Se
quitó de `MediaCard.svelte` y de la fila de "Tus playlists" en Inicio. **Quedan
pendientes** los mismos `group-hover/card:text-accent-soft` /
`group-hover/row:text-accent-soft` en `src/routes/(app)/explore/+page.svelte` (líneas
con tarjetas de álbum y filas de resultado — grep por `group-hover.*text-accent-soft`
para localizarlos exactos, las líneas se mueven con cada edición).

---

## 3. Migración de colores/tamaños a tokens

### 3.1 Datos de partida (medidos en esta sesión, `src/` de web-player)

```
27 archivos .svelte con al menos un text-[#…]/bg-[#…]/border-[#…]
106 apariciones totales de color hex literal en clases Tailwind
```

Los 10 valores hex más repetidos como `text-[#…]` (comando usado:
`grep -roE "(text|bg|border)-\[#[0-9a-fA-F]{3,8}\]" --include="*.svelte" . | sort | uniq -c | sort -rn`):

| Ocurrencias | Valor     | Coincide EXACTO con token             |
| ----------- | --------- | ------------------------------------- |
| 13          | `#8a8a8e` | `--mf-text-2` → usar `text-fg-2`      |
| 9           | `#747478` | ninguno exacto (cerca de text-3/4)    |
| 7           | `#d5d4d8` | ninguno exacto (cerca de `--mf-text`) |
| 7           | `#7f7f83` | ninguno exacto (cerca de text-3)      |
| 6           | `#d9d8dc` | `--mf-text` → usar `text-fg`          |
| 6           | `#d4d3d7` | ninguno exacto                        |
| 4           | `#d0cfd4` | ninguno exacto                        |
| 4           | `#818185` | ninguno exacto                        |
| 3           | `#d8d7db` | ninguno exacto                        |
| 2           | `#7d7d81` | `--mf-text-3` → usar `text-fg-3`      |

Interpretación: hay un grupo de valores que son **copias exactas** de un token ya
existente (probablemente porque alguien copió el valor en vez de la clase) — esos son
reemplazo mecánico, bajo riesgo. Y hay un grupo mucho más grande de valores "parecidos
pero no iguales" a los 4 tonos de texto (`--mf-text`, `-2`, `-3`, `-4`) — esto sugiere
que cada componente inventó su propio gris en vez de reusar la escala, y hay que decidir
para cada uno **a qué tier semántico pertenece** (¿es un título? ¿un metadato? ¿un
label?) y mapearlo al token de esa categoría, no crear un tier nuevo por cada valor.

Solo se encontraron 3 `bg-[#…]` (en `MixTile.svelte` y `TransportControls.svelte`,
este último repite el mismo `#c6c4cb`/`#d6d5da` que tenía el botón "Reanudar" de Inicio
antes de moverlo a `Button variant="accent"` con override — es el mismo "botón claro"
reinventado dos veces; candidato a token propio, ver §4.4) y 0 `border-[#…]`.

### 3.2 Metodología sugerida (por archivo)

1. `grep -n "text-\[#\|bg-\[#\|border-\[#" archivo.svelte` para listar todos los casos.
2. Para cada uno, decidir el tier semántico (título/cuerpo/metadato/label/borde) y
   sustituir por la utilidad de Tailwind ya mapeada en
   `src/routes/layout.css` (`@theme` → `--color-fg`, `--color-fg-2`, `--color-fg-3`,
   `--color-muted`, `--color-surface`, `--color-surface-2`, `--color-line`,
   `--color-accent*`, etc.) o, si el valor es realmente un tamaño de fuente en vez de
   color, a la escala ya usada en el resto de la app (10.5 / 11 / 11.5 / 12 / 12.5 / 13 /
   13.5 / 14px — no inventar un tamaño intermedio nuevo salvo que Inicio ya lo use para
   ese mismo tipo de texto).
3. Ejecutar `npm run check` tras cada archivo — Tailwind con clases arbitrarias no da
   error de compilación aunque el token no exista, así que la única validación real es
   visual: abrir la página en el navegador (Browser pane) y comparar contra Inicio.
4. Si dos o más componentes usan **el mismo valor hex no-token** para el mismo propósito
   (p. ej. `#c6c4cb` como "botón claro"), considerar promoverlo a un token nuevo en
   `theme.css` en vez de simplemente sustituirlo por el más parecido — ver §4.4.
5. `npm run lint` (prettier + eslint) antes de dar por cerrado cada archivo, según las
   reglas del `CLAUDE.md` del proyecto.

### 3.3 Prioridad sugerida de páginas/componentes a migrar

Por impacto (páginas que ve todo usuario, no solo estados de error/edge):

1. `Sidebar.svelte`, `TopBar.svelte`, `MobileHeader.svelte` (chrome global, visible
   siempre) — MobileHeader/TopBar ya tienen el menú de cuenta arreglado, falta el resto.
2. `src/routes/(app)/playlists/[id]/components/PlaylistHeader.svelte` y
   `src/routes/(app)/albums/[id]/components/AlbumHeader.svelte` (comparten patrón,
   arreglar juntos para no divergir otra vez).
3. `src/routes/(app)/liked/+page.svelte`, `src/routes/(app)/explore/+page.svelte`,
   `src/routes/(app)/upload/+page.svelte`, `src/routes/(app)/mixes/[id]/+page.svelte`.
4. Componentes del reproductor (`lib/components/player/*.svelte`) — muchos hex propios,
   pero cambian menos visualmente entre páginas, así que menor prioridad de contraste
   pero igual de importantes para el modo claro (la barra del reproductor es visible en
   toda la app).
5. `src/routes/+page.svelte` (Inicio) al final — es la referencia, así que migrarla
   última minimiza el riesgo de que un "acabado" a medio migrar se convierta en la
   referencia visual equivocada mientras se trabaja el resto.

---

## 4. Componentización pendiente

### 4.1 Patrón de fila de track reimplementado en vez de reusar `TrackTitleCell`

`src/routes/+page.svelte` reimplementa a mano, con su propio marcado, **tres** variantes
de "fila de canción" distintas entre sí (Continuar escuchando, Populares esta semana,
filas de la Playlist destacada) en vez de usar los componentes ya existentes
`TrackTitleCell.svelte` / `TrackRow.svelte` / `TrackMeta.svelte` /
`TrackIndexCell.svelte` que sí se usan correctamente en
`PlaylistTrackTable.svelte`/`library/+page.svelte`. Evaluar si esas tres filas de Inicio
se pueden expresar con los componentes de `TrackTable` (aunque sea sin la tabla
completa, solo reusando `TrackTitleCell` para title+cover+artist) para no mantener 4
implementaciones de "fila de canción" en paralelo.

### 4.2 `MediaCard`/`PlaylistCard`/`AlbumCard` vs. tarjetas hechas a mano

`src/routes/+page.svelte` (sección "Tus playlists") y `src/routes/(app)/explore/+page.svelte`
tienen su propia tarjeta de álbum/playlist en vez de usar `MediaCard`/`PlaylistCard`/
`AlbumCard` (`lib/components/ui/`). Antes de tocar el estilo de esas tarjetas a mano,
comprobar si pueden sustituirse directamente por el componente compartido — sale gratis
el arreglo de tipografía/color si el componente compartido ya está migrado a tokens.

### 4.3 Menú desplegable "de cristal" duplicado

`TopBar.svelte` y `MobileHeader.svelte` duplican: estado `menuOpen`, el
`onDocumentClick` para cerrar al hacer click fuera, y el marcado del panel
(`animate-pop ... backdrop-blur-[22px]` + `background-color:var(--mf-panel-bg);
background-image:var(--mf-modal-glow)`). `TrackContextMenu.svelte` tiene un menú
flotante distinto (`bg-elevated` + `border-line`, sin el efecto cristal, y con el bug de
tamaño de fuente de §2.1). Candidatos:

- Extraer un primitivo `Dropdown.svelte` o `FloatingMenu.svelte` que resuelva
  posicionamiento + cierre al clicar fuera + Escape, y que ambos menús de cuenta lo usen.
- Definir la receta "panel de cristal" (el `style="background-color:var(--mf-panel-bg);
background-image:var(--mf-modal-glow)"` + `backdrop-blur-[22px]`) en una utilidad o
  clase reusable en vez de repetir el `style=` inline en cada sitio, y aplicarla también
  a `TrackContextMenu` para que los tres menús floating de la app se vean iguales.

### 4.4 Candidato a token nuevo: "botón claro" (`#c6c4cb`/`#d6d5da`/`#d8d7dc`)

Aparece en al menos: el antiguo botón "Reanudar" de Inicio (ya migrado a
`Button variant="accent"` con `class="!bg-white ..."`, ver commit reciente) y
`TransportControls.svelte` (botón de play principal del reproductor, sigue con hex
propio). Si el diseño quiere mantener un botón "casi blanco" como acento fuerte en
ciertos sitios (distinto del verde de acento normal), vale la pena darle un token
propio (p. ej. `--mf-cta-strong-bg`/`--mf-cta-strong-fg`) en vez de repetir el hex cada
vez, así el modo claro también puede darle un tratamiento coherente en vez de heredar el
mismo blanco fijo en ambos temas (que en modo claro quedaría invisible sobre el fondo).

---

## 5. Código muerto — puntos de partida

Ya se han borrado en esta rama `MobileNav.svelte` y `MixBentoTile.svelte` por no
usarse. Grep rápido (basado en "ningún otro `.svelte`/`.ts` bajo `src/` menciona el
nombre del componente fuera de su propio archivo") encontró estos candidatos
adicionales — **verificar a mano antes de borrar** (el grep no será 100% preciso: no
detecta uso vía imports dinámicos, alias, o si el componente se monta desde fuera de
`src/`):

- `src/lib/components/ui/CoverBadge.svelte`
- `src/lib/components/ui/PlayPauseOverlay.svelte`

Método para repetir esta búsqueda tras cada ronda de refactor (los nombres de
componente que queden sin importadores son limpieza segura):

```bash
for f in $(find src/lib/components -name "*.svelte"); do
  name=$(basename "$f" .svelte)
  count=$(grep -rl "$name" --include="*.svelte" --include="*.ts" src | grep -v "/$name.svelte$" | wc -l)
  [ "$count" -eq 0 ] && echo "$f"
done
```

Además de componentes huérfanos, revisar por archivo (esto no se ha auditado
sistemáticamente esta sesión, son solo los tipos de código muerto más comunes en este
código base):

- Imports de iconos (`@lucide/svelte/icons/*`) que dejaron de usarse tras quitar un
  icono de un botón (pasó varias veces esta sesión con `Play`/`Pause`/`NowPlaying`;
  `eslint` los marca como `no-unused-vars` — correr `npx eslint <archivo>` tras cada
  cambio de marcado, no solo `svelte-check`, porque `svelte-check` no siempre lo pilla).
- Props de componentes que ya no lee nadie (ej. si se elimina el único sitio que pasaba
  `description` a `PlaylistCard`, y el componente ya no la usa, quitar el prop además
  del sitio que la usaba).
- Clases Tailwind puestas "por si acaso" que no tienen efecto porque una clase
  posterior en el cascade las anula (ver §2.2) — no es código muerto en el sentido
  clásico, pero es ruido que confunde a quien lea el archivo después.

---

## 6. Cómo verificar cada cambio

Del `CLAUDE.md` del proyecto (`web-player/CLAUDE.md`), aplica igual aquí:

```sh
npm run check    # svelte-check — debe salir 0 errores
npm run lint     # prettier --check . && eslint . — debe salir limpio
```

Y para lo visual, que `svelte-check`/`eslint` no cubren:

1. Levantar `musify-web` (y `musify-api` si la página necesita datos reales) con el
   Browser pane / `.claude/launch.json`.
2. Abrir la página tocada y la página de Inicio **una al lado de otra** (o alternando)
   para comparar tono de gris, tamaño de fuente y separaciones — no solo "que no se vea
   roto", sino "que se vea igual que Inicio para el mismo tipo de elemento".
3. Si la página tiene estado activo/actual (ej. una fila de canción reproduciéndose),
   probarlo — varios de los bugs de esta sesión (§2.2) solo eran visibles con el
   `isCurrent`/hover activo, no en el estado por defecto.
4. Alternar el toggle de tema (menú de cuenta → "Modo blanco"/"Modo oscuro") en la
   página tocada para confirmar que el texto sigue siendo legible en ambos modos.

---

## 7. Orden de trabajo sugerido

1. Terminar §2 (los 3 bugs conocidos que quedan sueltos: `TrackContextMenu.svelte`
   tamaño de fuente, hover verde en `explore/+page.svelte`).
2. Migrar a tokens los componentes de chrome global (§3.3, punto 1) — máximo impacto
   visual con menor superficie de código.
3. Extraer el "panel de cristal" y/o `Dropdown.svelte` (§4.3) **antes** de tocar
   `TrackContextMenu` a mano, para no maquetarlo dos veces.
4. Migrar el resto de páginas por la prioridad de §3.3.
5. Revisar componentización (§4.1, §4.2) — mejor después de que los componentes base
   (`TrackTitleCell`, `MediaCard`, etc.) ya estén en tokens, así "adoptarlos" en Inicio
   no introduce una regresión visual.
6. Pasada final de código muerto (§5) con el listado de componentes/props ya
   consolidado tras los pasos anteriores (habrá más candidatos a huérfanos según se
   vayan unificando duplicados).
7. Migrar Inicio a tokens en último lugar (§3.3, punto 5), verificando que los valores
   resultantes son pixel-iguales a los que tiene hoy (es la referencia, no debe cambiar
   de aspecto, solo de cómo está escrito el código).
