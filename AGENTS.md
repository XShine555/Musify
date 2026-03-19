# Contexto del proyecto para agentes de IA

## Resumen rápido
- Proyecto backend en `.NET 10`.
- Estructura principal en capas: `Domain`, `Application`, `Infrastructure`.
- Patrón arquitectónico: `Clean Architecture` con enfoque `CQRS`.

## Arquitectura
- **Domain (`Domain`)**: entidades de negocio puras.
- **Application (`Application`)**:
  - Casos de uso (comandos/queries y handlers).
  - Contratos (`Application.Contracts`).
  - Configuración de aplicación (`Application.Configuration`).
  - Mensajería orientada a eventos a través de `IEventBus`.
- **Infrastructure (`Infrastructure`)**:
  - Persistencia y acceso a datos.
  - Integraciones externas (Keycloak, almacenamiento, procesamiento de imágenes).
  - Implementación de mensajería y consumidores.

## CQRS / Mediación
- Se usa **DispatchR** (paquete `DispatchR.Mediator`) como variante de MediatR para orquestar comandos y consultas.
- Los casos de uso están organizados por feature con handlers dedicados (`Commands`, `Queries`).

## Mensajería y workers
- Se usa **MassTransit** para publicación/consumo de eventos.
- Broker actual: **RabbitMQ** (`MassTransit.RabbitMQ`).
- Hay modo de registro para workers con consumidores en `Infrastructure.Messaging.Consumers`.
- Actualmente existen consumidores como:
  - `ResizePictureConsumer`
  - `RemoveFileConsumer`

## Persistencia e integraciones
- `Entity Framework Core` + proveedor `Npgsql` (PostgreSQL).
- Integración de identidad con `Keycloak`.
- Almacenamiento con `AWS S3`.

## Notas para agentes
- Mantener separación de responsabilidades por capa.
- Evitar lógica de infraestructura dentro de `Application` y `Domain`.
- Agregar nuevos casos de uso siguiendo el patrón CQRS existente (comando/query + handler).
- Para nuevas integraciones asíncronas, reutilizar el pipeline de `MassTransit` y la abstracción `IEventBus`.
