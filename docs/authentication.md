# Authentication and identity

## Provider: Zitadel (OIDC/OAuth2)

Login and token issuance are handled by **Zitadel**, not the API. The API is a **resource server**: it only **validates** the access token (JWT) it receives in `Authorization: Bearer`.

- Configured under the `Authentication` section (`AuthenticationConfiguration`): metadata address, issuer, audience, client id, authorize/token endpoints, scopes.
- `JwtBearerOptionsSetup` sets up validation (issuer, audience, keys via OIDC metadata).
- In dev, `RequireHttpsMetadata = false`.

### Key requirement in the Zitadel app
- **Auth Token Type = JWT** (otherwise Zitadel issues an **opaque** token with no dots, which `JwtBearer` can't validate → 401 on everything protected).
- **User Info inside Token** enabled if you want the access token to carry profile claims (`name`, `email`, `picture`). By default the access token is minimal (`sub`, `aud`, `iss`, `exp`…), with no profile info.

## `sub` is numeric → `User.Id` is `long`

Zitadel's `sub` is a 64-bit integer encoded as a string (e.g. `371953080444977155`), **not a GUID**. Because of that:
- `User.Id` and every user-related FK (`PlayList.UserId`, `UserHasTrack.UserId`, `UploadIntent.UserId`) are **`long`**.
- `User.Id` has `[DatabaseGenerated(None)]` so EF doesn't autogenerate it: it's assigned from the `sub`.
- `CurrentUser.Id` is `long?` (parsed with `long.TryParse`); `RequiredId` throws if it's missing.

**Why**: the original code did `Guid.Parse(sub)`, which threw a `FormatException` → 401 on every protected endpoint. The fix was to store the `sub` as-is, as a number.

## User provisioning / sync

On every validated token, `JwtBearerEventsHandler.TokenValidated`:
1. Reads `sub` (id), `name`/`preferred_username`/`email` (with fallback), and the OIDC `picture` claim.
2. Sends a `SyncUserCommand`.

`SyncUserCommandHandler` does an **idempotent upsert**:
- If the user doesn't exist yet → creates it.
- If it exists → updates the name and `ProfilePictureUrl` **only if they changed** (a DB write only happens when something is different).

**Why "write only if changed"**: `TokenValidated` runs on **every request**; without that guard we'd be writing to the DB on every single call. It also needs to be resilient: if provisioning fails, it's logged but authentication **doesn't** fail because of it.

> Note: `CreateUserCommand` (the manual `POST /users` endpoint) is still create-only; login sync uses `SyncUserCommand`.

## Profile picture

`User.ProfilePictureUrl` is filled from the token's `picture` claim. If Zitadel doesn't include profile info in the access token (see above), it arrives as `null`. The code is correct, there's just nothing to sync until that option is turned on.
