# Autenticación e identidad

## Proveedor: Zitadel (OIDC/OAuth2)

El login y la emisión de tokens los hace **Zitadel** (no la API). La API es un **resource server**: solo **valida** el access token (JWT) que le llega en `Authorization: Bearer`.

- Config en la sección `Authentication` (`AuthenticationConfiguration`): metadata address, issuer, audience, client id, endpoints de authorize/token, scopes.
- `JwtBearerOptionsSetup` configura la validación (issuer, audience, claves vía metadata OIDC).
- En dev, `RequireHttpsMetadata = false`.

### Requisito clave en la app de Zitadel
- **Auth Token Type = JWT** (si no, Zitadel emite un token **opaco** sin puntos y `JwtBearer` no lo puede validar → 401 en todo lo protegido).
- **User Info inside Token** activado si quieres que el access token traiga claims de perfil (`name`, `email`, `picture`). Por defecto el access token es mínimo (`sub`, `aud`, `iss`, `exp`…), sin perfil.

## El `sub` es numérico → `User.Id` es `long`

El `sub` de Zitadel es un entero de 64 bits en string (p. ej. `371953080444977155`), **no un GUID**. Por eso:
- `User.Id` y todas las FKs de usuario (`PlayList.UserId`, `UserHasTrack.UserId`, `UploadIntent.UserId`) son **`long`**.
- `User.Id` lleva `[DatabaseGenerated(None)]` para que EF **no** lo autogenere: se asigna el `sub`.
- `CurrentUser.Id` es `long?` (parsea con `long.TryParse`); `RequiredId` lanza si falta.

**Por qué**: el código antiguo hacía `Guid.Parse(sub)` y reventaba (FormatException) → 401 en cada endpoint protegido. Se decidió guardar el `sub` tal cual como número.

## Provisioning / sincronización de usuarios

En cada token validado, `JwtBearerEventsHandler.TokenValidated`:
1. Lee `sub` (id), `name`/`preferred_username`/`email` (con fallback) y el claim OIDC `picture`.
2. Envía un `SyncUserCommand`.

`SyncUserCommandHandler` hace un **upsert idempotente**:
- Si el usuario no existe → lo crea.
- Si existe → actualiza nombre/apellidos/`ProfilePictureUrl` **solo si cambiaron** (escribe en DB únicamente cuando hay cambios).

**Por qué upsert con "escribe solo si cambió"**: `TokenValidated` se ejecuta en **cada request**; sin ese guardado condicional estaríamos escribiendo en cada petición. Y debe ser resiliente: si el provisioning falla, se loguea pero **no** tumba la autenticación.

> Nota: `CreateUserCommand` (endpoint manual `POST /users`) sigue siendo create-only; el sync del login usa `SyncUserCommand`.

## Foto de perfil

`User.ProfilePictureUrl` se rellena desde el claim `picture` del token. Si Zitadel no mete la info de perfil en el access token (ver arriba), llega `null` — el código es correcto, pero no hay nada que sincronizar hasta activar esa opción.
