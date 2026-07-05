# Entorno de desarrollo

Cómo levantar todo en local y probarlo.

## Dependencias (Docker)

```powershell
# SeaweedFS (almacenamiento)
docker compose -f deploy\seaweedfs\docker-compose.yml up -d

# PostgreSQL + pgAdmin
docker compose -f "C:\Users\XShin\OneDrive\Desktop\postgresql\docker-compose.yml" up -d

# Zitadel (auth)
docker compose -f "C:\Users\XShin\OneDrive\Pictures\zitadel-compose\docker-compose.yml" up -d

# RabbitMQ (contenedor suelto)
docker start rabbitmq
```

> Nota: usa `docker compose up` (no `docker start`) para Postgres/Zitadel: si sus redes se borraron, compose las recrea.

## Aplicaciones (.NET)

```powershell
dotnet run --project Musify.WebApi\WebApi                    # :5111
dotnet run --project Musify.StreamingGateway\StreamingGateway # :8081
dotnet run --project Musify.Worker\Worker                     # background
```

## Mapa de puertos

| Servicio | Puerto |
|---|---|
| WebApi | 5111 (`/scalar/v1`, `/openapi/v1.json`) |
| StreamingGateway | 8081 (`/health`, `/media/...`) |
| SeaweedFS S3 / filer / master | 8333 / 8888 / 9333 |
| Zitadel | 8080 |
| PostgreSQL | 59000 (→ 5432 en el contenedor) |
| RabbitMQ AMQP / management | 5672 / 15672 |
| pgAdmin | 5050 |

DB: `musify_db`, usuario `postgres`/`postgres`. Bucket S3: `webapi-storage` (credenciales `admin_access_key`/`admin_secret_key`).

## Migraciones

No hay migración automática al arrancar. Para aplicar cambios de esquema:

```powershell
cd MusifyBackend\Infrastructure
dotnet ef database update --context Database
```

La cadena de conexión para `dotnet ef` se lee de `DesignSettings.json` / user-secrets del proyecto Infrastructure.

## Stream-tickets (claves RS256)

La WebApi firma los stream-tickets con una clave privada y el gateway valida con la pública:

```powershell
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out stream_private.pem
openssl rsa -in stream_private.pem -pubout -out stream_public.pem
```

- WebApi: `StreamTicket:PrivateKeyPath` → `stream_private.pem`.
- Gateway: `StreamTicket:PublicKeyPath` → `stream_public.pem`.

## Consola de pruebas: `player.html`

Un único archivo HTML para probar todo sin front:

1. Sírvelo o ábrelo directo (el CORS está abierto en Development).
2. Pega tu **access token** de Zitadel y la API base (`http://localhost:5111`).
3. Permite: crear/actualizar/eliminar playlists, listar canciones, añadir/quitar canciones de una playlist, ver las canciones de una playlist y **reproducir** (pide el ticket y abre el `.m4a` con `?t=<ticket>`).

> Nota: `player.html` todavía usa dash.js; tras el cambio a `.m4a` debe actualizarse para reproducir el fichero directo (elemento `<audio>` por HTTP range).

Para obtener un token: login OAuth2 en Scalar (`/scalar/v1`) o cópialo de DevTools.

## Config (AppSettings)

Convención: `AppSettings.json` = plantilla con valores vacíos/por defecto; `AppSettings.Development.json` = valores reales de dev. Secretos sensibles (claves, connection strings) van idealmente en user-secrets.

Secciones clave: `Authentication` (Zitadel), `Database`, `MassTransit`, `InfrastructureStorage` (S3), `ApplicationStorage` (bucket), `Track`, `PlayList`, `UploadIntent`, `AudioTranscoder`, `Workers`, `StreamGateway`/`StreamTicket`.

## Requisitos del host

- .NET 10 SDK.
- **ffmpeg** en el PATH (transcode de audio del Worker).
- Docker Desktop.
- Directorio temporal de trabajo del Worker (`Workers:Routes:TemporaryFilesDirectory`, p. ej. `D:\tempsFilesDev`).
