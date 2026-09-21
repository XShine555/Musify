# Musify Mobile — demo Kotlin Multiplatform

Cliente Kotlin Multiplatform (Compose Multiplatform) para Android, Desktop (Windows/macOS/Linux) e iOS,
compartiendo el 100% de la lógica de red, autenticación y estado con solo la UI y unos pocos archivos
`expect`/`actual` por plataforma (navegador de login, reproductor de audio y almacenamiento de tokens).

Cubre exactamente el alcance pedido:

- **Iniciar sesión / Registrarse** — OAuth 2.0 Authorization Code + PKCE contra Zitadel (el mismo
  proveedor que ya usa `web-player`). La pantalla de login hospedada de Zitadel ya incluye el enlace
  "¿No tienes cuenta? Regístrate", así que un solo flujo cubre ambos casos — no existe (ni falta) un
  endpoint de registro propio en el backend.
- **Ver canciones generales** — `GET /tracks`, que es público.
- **Reproducirlas** — `GET /tracks/{id}/stream` (requiere el token Bearer) + reproducción nativa por
  plataforma del `manifestUrl?t=ticket` que devuelve.

## Por qué Kotlin Multiplatform (resumen de la investigación)

| | |
|---|---|
| **UI compartida** | [Compose Multiplatform](https://www.jetbrains.com/compose-multiplatform/) — el mismo árbol de composables corre en Android (Jetpack Compose), Desktop (Skiko sobre Swing/AWT) e iOS (Skia sobre UIKit vía un `UIViewController`). |
| **Lógica compartida** | Networking (Ktor client), modelos (`kotlinx.serialization`), auth (PKCE, hecho a mano), estado (`StateFlow`) — todo en `commonMain`, cero duplicación entre plataformas. |
| **Lo no compartido (`expect`/`actual`)** | Abrir el navegador del sistema para el login, reproducir audio, y persistir el token — porque no hay una API común decente para eso en las tres plataformas. Son ~5 archivos pequeños por plataforma. |
| **Alternativa descartada** | UI nativa por plataforma (Jetpack Compose / SwiftUI / Swing a mano) — mucho más código, y el usuario pidió explícitamente poder compartir la app entre desktop/Android/iOS. |

Referencia oficial: <https://www.jetbrains.com/help/kotlin-multiplatform-dev/get-started.html>

## Estructura

```
mobile/
  composeApp/
    src/
      commonMain/   ← toda la lógica + toda la UI (App.kt, AppViewModel.kt, MusifyApi.kt, auth/…)
      androidMain/   ← MainActivity, AuthCallbackActivity, ExoPlayer, DataStore, Custom Tabs
      desktopMain/   ← main(), loopback HTTP server para el login, JavaFX MediaPlayer
      iosMain/       ← MainViewController(), ASWebAuthenticationSession, AVPlayer, NSUserDefaults
```

Puntos de extensión `expect`/`actual` (commonMain → cada plataforma):

- `AppConfig` — URLs del backend y del redirect URI nativo.
- `BrowserAuthLauncher` — abre el navegador y espera el redirect de Zitadel.
- `TokenStore` — persiste el token localmente.
- `AudioPlayer` — reproduce la URL firmada.
- `sha256()` / `nowEpochSeconds()` — primitivas de PKCE/expiración.

## Cómo se conecta al backend (ya provisionado en este repo)

Se dio de alta un tercer cliente OIDC público **"Musify Native"** en Zitadel (PKCE, sin secreto), igual
al patrón que ya usa Scalar para probar la API — ver `deploy/compose.yml` (`zitadel-init`, sección
"native app"). Quedó escrito en `deploy/.env` como `NATIVE_CLIENT_ID` y copiado a
`mobile/gradle.properties`.

Redirect URIs registrados:
- Desktop: `http://127.0.0.1:17453/callback` (RFC 8252, loopback con puerto fijo).
- Android / iOS: `musify://callback` (custom scheme).

`mobile/gradle.properties` controla a qué backend apunta Android/Desktop:

```properties
musify.apiBaseUrl=http://localhost:5111
musify.authIssuer=http://host.docker.internal:8080
musify.authClientId=<NATIVE_CLIENT_ID de deploy/.env>
```

(`host.docker.internal` resuelve al host en Windows/Mac con Docker Desktop corriendo — es el mismo
valor que usa el resto del stack. En Linux, o para reproducir en un dispositivo/simulador real, cambia
esto por la IP LAN de la máquina que corre `dotnet run`.)

iOS no puede leer `gradle.properties` en tiempo de ejecución (es un binario nativo, no una JVM) — sus
valores están hardcodeados en `composeApp/src/iosMain/kotlin/com/musify/app/Config.ios.kt`, edítalo si
cambias de backend.

### Levantar el backend

La infraestructura (Postgres, Zitadel, RabbitMQ, SeaweedFS) corre con Docker:

```bash
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml up -d
```

La API y el Streaming Gateway se corren aparte (así se probó esta demo):

```bash
cd backend/Hosts/Musify.Api && dotnet run
cd backend/Hosts/Musify.StreamingGateway && dotnet run
```

> El catálogo puede estar vacío en una base nueva — `GET /tracks` responderá `200` con `items: []`.
> Subir una canción real requiere además el Worker (`backend/Hosts/Musify.Worker`) + `ffmpeg` en el
> PATH, porque el pipeline transcodea el audio subido antes de que quede reproducible.

## Cómo correr cada plataforma

### Desktop (la única que se pudo ejecutar y verificar en este entorno)

```bash
cd mobile
./gradlew :composeApp:run
```

Abre una ventana nativa (`MusifyMobile.msi`/`.exe` vía `./gradlew :composeApp:packageMsi` para
distribuirla). "Iniciar sesión" abre tu navegador por defecto contra Zitadel; al volver, un servidor
HTTP local efímero en `127.0.0.1:17453` recibe el código de autorización.

### Android

```bash
cd mobile
./gradlew :composeApp:installDebug
```

Necesita el Android SDK (`ANDROID_HOME` o `mobile/local.properties` con `sdk.dir=...`) — no estaba
instalado en esta máquina, así que el target **se escribió pero no se compiló aquí**; `compileKotlinDesktop`
sí confirma que el código común que comparte es válido, y `compileDebugKotlinAndroid` falló únicamente
en el paso de localizar el SDK (evidencia de que el resto del Gradle DSL es correcto). Ábrelo con
Android Studio (Hedgehog+ con soporte KMP) y debería sincronizar sin tocar nada.

### iOS

No hay Mac/Xcode disponible en este entorno, así que **no se pudo compilar ni probar** — el código
Kotlin/Native (`iosMain/`) sigue las APIs estándar de interop (`AVFoundation`, `AuthenticationServices`,
`CoreCrypto`) pero no fue verificado por un compilador. Para probarlo:

1. Genera un proyecto Xcode con el asistente "Kotlin Multiplatform" de Android Studio, o crea uno vacío
   e importa `composeApp` como framework (`./gradlew :composeApp:embedAndSignAppleFrameworkForXcode`).
2. Registra el scheme `musify` en `Info.plist`:
   ```xml
   <key>CFBundleURLTypes</key>
   <array>
     <dict>
       <key>CFBundleURLSchemes</key>
       <array><string>musify</string></array>
     </dict>
   </array>
   ```
3. `ContentView.swift`:
   ```swift
   import SwiftUI
   import ComposeApp

   struct ComposeView: UIViewControllerRepresentable {
       func makeUIViewController(context: Context) -> UIViewController { MainViewControllerKt.MainViewController() }
       func updateUIViewController(_ uiViewController: UIViewController, context: Context) {}
   }

   struct ContentView: View {
       var body: some View { ComposeView().ignoresSafeArea(.keyboard) }
   }
   ```
4. Ajusta `Config.ios.kt` para que `apiBaseUrl`/`authIssuer` apunten a una dirección alcanzable desde el
   simulador/dispositivo (ver arriba).

## Qué se verificó de verdad en esta máquina

- ✅ `./gradlew :composeApp:compileKotlinDesktop` — compila limpio (valida todo `commonMain` +
  `desktopMain`).
- ✅ `./gradlew :composeApp:run` — la app arranca, se conecta a la API local (`GET /tracks`) y queda
  corriendo sin excepciones.
- ✅ Cliente OIDC "Musify Native" creado de verdad en el Zitadel local del repo (`deploy/.env`,
  `NATIVE_CLIENT_ID`).
- ✅ `Musify.Api` (`:5111`) y `Musify.StreamingGateway` (`:8081`) corriendo localmente contra el resto
  del stack en Docker.
- ⚠️ El catálogo de canciones está vacío en esta base — la pantalla de lista funciona pero no hay nada
  que reproducir todavía. Pide que te ayude a subir una canción de prueba (necesita `ffmpeg` + el Worker
  corriendo) si quieres ver la reproducción de punta a punta.
- ❌ Android — compila el código común, pero no se probó el target completo (sin Android SDK aquí).
- ❌ iOS — código escrito siguiendo las APIs estándar, sin verificar (sin Mac/Xcode aquí).
