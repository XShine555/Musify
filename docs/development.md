# Entorno de desarrollo

Cómo levantar todo en local y probarlo. El detalle completo del deploy (y el de
producción) está en [deploy/README.md](../deploy/README.md).

## Arranque

```powershell
./deploy/up.ps1            # infraestructura; las apps se ejecutan desde el IDE
./deploy/up.ps1 -Apps      # todo en Docker (api, worker, gateway, web player)
```

`up.ps1` hace el bootstrap completo: crea `deploy/.env`, genera las claves RS256
de stream-ticket en `deploy/keys/`, levanta el stack, aplica las migraciones y
provisiona Zitadel (proyecto + apps OIDC), dejando los client ids en
`deploy/.env`, en los user-secrets de `Musify.Api` y en `web-player/.env`.

Otros modificadores: `-Tools` (pgAdmin), `-SkipMigrations`, `-Down`, `-Destroy`.

## Aplicaciones

```powershell
dotnet run --project backend/Musify.Api               # :5111
dotnet run --project backend/Musify.StreamingGateway  # :8081
dotnet run --project backend/Musify.Worker            # background
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

No hay migración automática al arrancar; `up.ps1` las aplica en el bootstrap.
A mano, tras cambios de esquema:

```powershell
dotnet ef database update --project backend/Musify.Infrastructure --startup-project backend/Musify.Infrastructure --context Database
```

`Musify.Infrastructure` es a la vez el proyecto de migraciones y el de arranque:
tiene el `IDesignTimeDbContextFactory` y el paquete `EFCore.Design`. La cadena de
conexión se lee de sus user-secrets (`up.ps1` la deja puesta).

## Stream-tickets (claves RS256)

La API firma los stream-tickets con la clave privada y el gateway los valida con
la pública. `up.ps1` las genera en `deploy/keys/`; a mano:

```powershell
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out deploy/keys/stream_private.pem
openssl rsa -in deploy/keys/stream_private.pem -pubout -out deploy/keys/stream_public.pem
```

- API: `StreamTicket:PrivateKeyPath`.
- Gateway: `StreamTicket:PublicKeyPath`.

## Config (AppSettings)

Convención: `AppSettings.json` es la plantilla con valores vacíos o neutros;
`AppSettings.Development.json` tiene los valores reales de dev; en Docker todo
se sobreescribe con variables de entorno (`Seccion__Clave`) desde
`deploy/compose.yml`. Los valores de `Authentication` los deja el provisioning
en user-secrets, no en el repo.

Secciones clave: `Authentication` (Zitadel), `Database`, `MassTransit`,
`InfrastructureStorage` (S3), `ApplicationStorage` (bucket), `Track`, `PlayList`,
`UploadIntent`, `AudioTranscoder`, `YtDlp`, `Workers`, `StreamGateway` /
`StreamTicket`.

## Requisitos del host

- .NET 10 SDK y Node 22.
- **ffmpeg** y **yt-dlp** en el PATH (transcode y descarga de audio del Worker).
- Docker Desktop.
