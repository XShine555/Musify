# Entorno de desarrollo

Cómo levantar todo en local y probarlo. El detalle completo del deploy (y el de
producción) está en [deploy/README.md](../deploy/README.md).

## Arranque

```sh
docker compose -f deploy/compose.yml -f deploy/compose.dev.yml up -d
```

Ese único comando levanta la infraestructura y corre los jobs *one-shot* de
bootstrap: genera las claves RS256 de stream-ticket en `deploy/keys/`, aplica
las migraciones EF Core y provisiona Zitadel (proyecto + apps OIDC), dejando
los client ids en `deploy/.env`. Detalle completo, incluido cómo arrancar las
apps en Docker con `--profile apps`, en [deploy/README.md](../deploy/README.md).

## Aplicaciones

```powershell
dotnet run --project backend/Hosts/Musify.Api               # :5111
dotnet run --project backend/Hosts/Musify.StreamingGateway  # :8081
dotnet run --project backend/Hosts/Musify.Worker            # background
npm --prefix web-player run dev                       # :5173
```

## Mapa de puertos

| Servicio | Puerto |
|---|---|
| Musify.Api | 5111 (`/scalar/v1`, `/openapi/v1.json`) |
| Musify.StreamingGateway | 8081 (`/health`, `/media/...`) |
| web-player | 5173 (`npm run dev`) o 3000 (contenedor) |
| SeaweedFS S3 / filer / master | 8333 / 8888 / 9333 |
| Zitadel | 8080 |
| PostgreSQL | 59000 (→ 5432 en el contenedor) |
| RabbitMQ AMQP / management | 5672 / 15672 |
| Jaeger UI / OTLP | 16686 / 4317 |
| pgAdmin (`-Tools`) | 5050 |

DB: `musify_db`, usuario `postgres`/`postgres`. Bucket S3: `webapi-storage`
(credenciales `admin_access_key`/`admin_secret_key`). Todos los valores salen de
`deploy/.env`.

## Migraciones

El servicio `migrate` de `deploy/compose.yml` las aplica en cada `docker
compose up -d` (no-op si el esquema ya está al día). A mano, tras cambios de
esquema, contra la base local:

```powershell
dotnet ef database update --project backend/Core/Musify.Infrastructure --startup-project backend/Core/Musify.Infrastructure --context Database
```

`Musify.Infrastructure` es a la vez el proyecto de migraciones y el de arranque:
tiene el `IDesignTimeDbContextFactory` y el paquete `EFCore.Design`. La cadena
de conexión se lee de sus user-secrets, o de la variable de entorno
`Database__ConnectionString` si no hay user-secrets (así es como la usa el
contenedor `migrate`, ver `backend/Musify.Migrator.Dockerfile`).

## Stream-tickets (claves RS256)

La API firma los stream-tickets con la clave privada y el gateway los valida con
la pública. El servicio `keys-init` de `deploy/compose.yml` las genera en
`deploy/keys/` si no existen; a mano:

```powershell
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out deploy/keys/stream_private.pem
openssl rsa -in deploy/keys/stream_private.pem -pubout -out deploy/keys/stream_public.pem
```

- API: `StreamTicket:PrivateKeyPath`.
- Gateway: `StreamTicket:PublicKeyPath`.

## Tests

```sh
dotnet test backend/Musify.slnx
```

`backend/Tests/` (carpeta `/Tests/` en el `.slnx`) tiene cinco proyectos xUnit:

| Proyecto | Cubre | Docker |
|---|---|---|
| `Musify.Domain.Tests` | Lógica de los value objects (`TrackAudio`/`TrackPictures`: `IsProcessed`, `IsFailed`, `IsInProgress`). Las entidades son en su mayoría anémicas — sin comportamiento propio, nada más que testear ahí. | No |
| `Musify.Application.Tests` | Los ~45 handlers y servicios de `Musify.Application` (Albums, Tracks, PlayLists, Users, Mixes). | No |
| `Musify.Infrastructure.Tests` | `Database`/migraciones, `StorageService`, `StreamTicketService`, `AuditableEntityInterceptor`, `PictureService`, `SingleFlightCache`, `PlayListPresetSeeder`. | **Sí** |
| `Musify.Api.Tests` | Endpoints HTTP reales (routing, auth, `ValidationFilter`, mapeo `ErrorOr`→HTTP) vía `WebApplicationFactory`, más `ErrorOrHttpExtensions`/`CurrentUser`/validators FluentValidation en aislado. | **Sí** |
| `Musify.StreamingGateway.Tests` | `TicketValidator` (RS256) y `TicketValidationMiddleware` (traversal, límites de prefijo, extracción de ticket). | No |

`Musify.Worker` no tiene proyecto de test: su `Program.cs` es puro *wiring* de
DI/host (Hangfire, MassTransit, jobs) — la lógica real que ejecuta vive en
`Musify.Infrastructure`, ya cubierta ahí.

### Application.Tests: por qué SQLite en memoria y no mocks

`IDatabase` expone `DbSet<T>` directamente, así que los handlers arman LINQ
real (`Where`, `Include`, `Select`...) contra él — no se puede mockear eso con
NSubstitute, no hay `IQueryable` detrás de un mock. En su lugar,
`TestSupport/TestDatabase.cs` es un `DbContext` real sobre SQLite en memoria
(una conexión `:memory:` nueva por test, con foreign keys reales — a diferencia
del proveedor InMemory de EF Core, SQLite además soporta transacciones, que dos
handlers usan). `TestSupport/TestEntities.cs` y `TestConfigurations.cs`
construyen entidades y configuración con valores por defecto sensatos.
`IStorageService`, `IEventBus`, etc. sí se mockean con NSubstitute, al ser
interfaces normales.

### Infrastructure.Tests y Api.Tests: Testcontainers

Estos dos sí necesitan Docker corriendo. Levantan un Postgres real
(`Testcontainers.PostgreSql`) y, en `Infrastructure.Tests`, también un
SeaweedFS real (mismo `chrislusf/seaweedfs:latest` e imagen/comando que
`deploy/compose.yml`, no MinIO) — así `StorageService` se prueba contra el
mismo backend S3-compatible que usa producción, no contra AWS S3. Los
contenedores se levantan una vez por corrida (`ICollectionFixture`), con
migraciones EF Core reales aplicadas contra el Postgres efímero.

`Api.Tests` usa `WebApplicationFactory<Program>` contra ese mismo Postgres —
routing, auth y validación reales — pero sustituye por fakes los servicios que
hablan por red (`IStorageService`, `IEventBus`, `IStreamTicketService`) y el
esquema de autenticación JWT real por un handler
de prueba (ver `TestSupport/FakeAuthenticationHandler.cs`) que autentica según
un header, sin necesitar un Zitadel real.

Escribir estos tests contra el backend real encontró y arregló dos bugs reales
(no simulados): `StorageService.RemoveFolderAsync` reventaba con
`NullReferenceException` contra SeaweedFS (su respuesta `DeleteObjects` omite
la lista `<Error>` cuando no hay errores, a diferencia de AWS S3), y
`AuditableEntityInterceptor` nunca actualizaba `UpdatedAt` en una actualización
normal (carga → modifica → guarda) porque corría antes de que EF detectara los
cambios pendientes.

## Config (AppSettings)

Convención: `AppSettings.json` es la plantilla con valores vacíos o neutros;
`AppSettings.Development.json` tiene los valores reales de dev; en Docker todo
se sobreescribe con variables de entorno (`Seccion__Clave`) desde
`deploy/compose.yml`. Para correr `Musify.Api` desde el IDE, los valores de
`Authentication` van en user-secrets (no en el repo) — cópialos de
`deploy/.env` una vez que `zitadel-init` los haya generado (ver
[deploy/README.md](../deploy/README.md)).

Secciones clave: `Authentication` (Zitadel), `Database`, `MassTransit`,
`InfrastructureStorage` (S3), `ApplicationStorage` (bucket), `Track`, `PlayList`,
`UploadIntent`, `AudioTranscoder`, `Workers`, `StreamGateway` / `StreamTicket`.

## Requisitos del host

- .NET 10 SDK y Node 22.
- **ffmpeg** en el PATH (transcode de audio del Worker).
- Docker Desktop.
