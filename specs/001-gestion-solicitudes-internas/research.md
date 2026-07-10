# Research: Sistema de Gestión de Solicitudes Internas

**Branch**: `001-gestion-solicitudes-internas` | **Date**: 2026-07-09 | **Phase**: 0

---

## 1. Stack Tecnológico — Decisiones y Justificación

### 1.1 Backend: .NET 8 Web API

- **Decision**: .NET 8 LTS con ASP.NET Core Web API (Minimal API + Controllers según complejidad)
- **Rationale**: LTS hasta Nov 2026; soporte nativo para JWT middleware, EF Core 8, health checks, y OpenAPI. Excelente rendimiento (Kestrel) para APIs REST internas.
- **Patterns recomendados**:
  - **Clean Architecture** (Application / Domain / Infrastructure / API): separa lógica de negocio de infraestructura; facilita testing unitario sin DB.
  - **CQRS ligero** con MediatR: Commands (escritura) y Queries (lectura) separados — crítico para auditoría inmutable y separación de concerns entre flujos de aprobación.
  - **Repository + Unit of Work** sobre EF Core para aislar persistencia.
  - **Result Pattern** (`OneOf` o `FluentResults`) en lugar de excepciones para flujos de validación de negocio.
- **Alternatives considered**: Node.js/Express (rechazado — stack definido por usuario), FastAPI (rechazado — stack definido).

### 1.2 Frontend: React + TypeScript

- **Decision**: React 18 con TypeScript 5, Vite como bundler.
- **Rationale**: TypeScript garantiza type-safety en contratos de API; React 18 con Suspense simplifica estados de carga — crítico para historial con filtros (SC-005). Vite ofrece HMR rápido para desarrollo.
- **Patterns recomendados**:
  - **Feature-based folder structure**: `features/solicitudes/`, `features/supervisor/`, `features/admin/` — alinea directamente con las User Stories.
  - **React Query (TanStack Query)**: manejo de estado de servidor, caché, polling para notificaciones in-app.
  - **React Hook Form + Zod**: validación de formularios con schema compartido con el backend (contratos de API).
  - **Axios con interceptors**: adjuntar JWT automáticamente; manejar 401 → redirect a login.
- **Alternatives considered**: Angular (rechazado — stack definido), Next.js (rechazado — innecesario para SPA interna sin SSR requerido).

### 1.3 Base de Datos: SQL Server 2022

- **Decision**: SQL Server 2022 con EF Core 8 Code-First migrations.
- **Rationale**: Soporte nativo de `ROWVERSION` para optimistic concurrency (FR-018 — "Tomar solicitud" first-wins); `JSON` columns para campos dinámicos de tipos de solicitud; Temporal Tables para auditoría adicional de nivel DB.
- **Patterns recomendados**:
  - **`ROWVERSION` / `Timestamp` en entidad `Solicitud`**: EF Core lo mapea automáticamente; `DbUpdateConcurrencyException` en "Tomar solicitud" implementa FR-018 sin código adicional.
  - **Soft delete** (`IsActive`, `DeletedAt`) en lugar de DELETE físico — crítico para FR-006 (no eliminar tipos con solicitudes) y FR-008 (inmutabilidad de auditoría).
  - **`EventoAuditoria` como tabla append-only**: definir constraint a nivel DB (`INSERT` only, sin `UPDATE`/`DELETE` vía rol de DB) + inmutabilidad en EF Core (entidad sin setters públicos).
  - **Índices**: `(Estado, SupervisorId)` en `Solicitud` para queries de supervisor; `(EmpleadoId, FechaCreacion)` para historial de empleado; `(TipoSolicitudId, Estado)` para admin.
- **JSON columns para campos dinámicos**: `Solicitud.CamposDinamicos` como `nvarchar(max)` serializado con System.Text.Json — simple, sin esquema adicional para v1; campos de tipos son texto/número/fecha/lista (per Assumptions).

### 1.4 Autenticación: JWT

- **Decision**: JWT Bearer tokens con `Microsoft.AspNetCore.Authentication.JwtBearer`.
- **Rationale**: Stateless, compatible con React SPA; roles embebidos en claims (`Empleado`, `Supervisor`, `Administrador`) para autorización declarativa con `[Authorize(Roles = "...")]`.
- **Patterns recomendados**:
  - **Access token corto (15 min) + Refresh token (7 días)** en HttpOnly cookie — buena práctica de seguridad para apps internas.
  - **Claims**: `sub` (userId), `role`, `departmentId`, `supervisorId` — necesarios para filtrado por rol (FR-007) sin queries adicionales.
  - **`ICurrentUserService`**: abstracción sobre `IHttpContextAccessor` para inyectar identidad en Application layer sin acoplar a infraestructura.
- **Assumption**: el sistema de autenticación externo (directorio corporativo) entrega tokens o credenciales que el backend valida y reemite como JWT internos (per Assumptions del spec).

### 1.5 ORM: Entity Framework Core 8

- **Decision**: EF Core 8 con Code-First, Fluent API para configuración, migrations automáticas en startup (dev) o scripts (prod).
- **Patterns recomendados**:
  - **Interceptores de EF Core** para auditoría automática: `SaveChangesInterceptor` que detecta cambios de estado en `Solicitud` y escribe `EventoAuditoria` sin lógica manual en cada servicio.
  - **`HasQueryFilter`** para soft delete global — evita filtros manuales por entity.
  - **Configuraciones separadas** en `EntityTypeConfiguration<T>` por entidad — no en `OnModelCreating`.
  - **Owned types** para `CamposDinamicos` si se decide normalizar campos en el futuro.

### 1.6 CI/CD: GitHub Actions

- **Decision**: GitHub Actions con workflows separados por etapa.
- **Rationale**: Integrado con el repositorio; soporte para .NET y Node.js sin configuración adicional.
- **Workflow recomendado**:
  ```
  PR → build + test (unit + integration) → lint (ESLint + dotnet format)
  main merge → build + test + Docker build + deploy staging
  tag/release → deploy production
  ```
- **Actions claves**: `actions/setup-dotnet@v4`, `actions/setup-node@v4`, `azure/sql-action` (si SQL Server en Azure) o `docker-compose` para test DB.

### 1.7 Semantic Kernel (validación posterior)

- **Decision**: Integrar SK como servicio de validación post-implementación — no en el núcleo del feature v1.
- **Rationale**: SK puede usarse para validar lenguaje natural en comentarios de rechazo (FR-004), detectar solicitudes duplicadas semánticas, o generar resúmenes de auditoría para administradores.
- **Approach**: Plugin separado en `Infrastructure/AI/` — no acoplado a dominio. Activar solo si `SemanticKernel:Enabled = true` en config.
- **Integration point**: Post-submit hook en `EnviarSolicitudCommandHandler` para análisis asíncrono (no bloquea el flujo principal).

---

## 2. Patrones de Dominio — Decisiones Clave

### 2.1 Máquina de estados de Solicitud

```
Borrador ──(Enviar)──► Enviada ──(Tomar)──► En Revisión ──(Aprobar)──► Aprobada
                          │                      │
                       (Cancelar)             (Rechazar)
                          ▼                      ▼
                       Cancelada             Rechazada
```

- **Transiciones válidas**: definidas como tabla o método en `Solicitud.PuedeTransicionarA(nuevoEstado, actorRol)`.
- **Borrador → Enviada**: solo el empleado dueño puede enviar; borradores expiran a los 3 días (job de limpieza).
- **Enviada → En Revisión**: solo supervisores del área del empleado; concurrencia por `ROWVERSION`.
- **En Revisión → Aprobada/Rechazada**: solo el supervisor que tomó la solicitud (o reasignación por admin).
- **Cancelada**: solo desde "Enviada"; solo el empleado dueño.

### 2.2 Modelo de Reasignación Dual

| Nivel | Actor | Efecto | Audit |
|-------|-------|--------|-------|
| Organizacional (empleado) | Admin | Cambia `Usuario.SupervisorId` | Sí |
| Solicitud específica | Admin | Cambia `Solicitud.SupervisorAsignadoId` | Sí |
| En bloque (pre-desactivación) | Admin | N solicitudes → M supervisores | Sí, por solicitud |

### 2.3 Borrador con Autoguardado

- **Frontend**: debounce de 2s en formulario → `PATCH /api/solicitudes/borrador` (upsert).
- **Backend**: el handler verifica que el borrador pertenezca al `CurrentUser`; guarda `UltimaModificacion`.
- **Expiración**: `BackgroundService` (Hosted Service) que corre diariamente → elimina borradores con `FechaCreacion < hoy - 3 días`.

### 2.4 Notificaciones In-App

- **Tabla `Notificacion`**: `DestinatarioId`, `Tipo`, `Contenido`, `Leida`, `FechaGeneracion`, `SolicitudId`.
- **Generación**: `DomainEvent` → `INotificationService` → insert en `Notificacion`.
- **Polling en frontend**: React Query con `refetchInterval: 30000` para badge de notificaciones no leídas.
- **Sin WebSockets en v1**: polling es suficiente para un sistema interno; WebSockets como mejora futura.

---

## 3. Seguridad

- **Autorización por recurso**: middleware de policies `SolicitudOwnerPolicy`, `SupervisorAreaPolicy` — no solo por rol, sino por relación con el recurso.
- **Audit log inmutable**: el rol de DB que usa la app NO tiene `UPDATE`/`DELETE` sobre `EventosAuditoria`.
- **SQL Injection**: EF Core con parametrización automática; prohibir `FromSqlRaw` sin parámetros.
- **CORS**: solo el origen del frontend en producción.
- **Rate limiting**: `Microsoft.AspNetCore.RateLimiting` — 60 req/min por usuario para endpoints de envío de solicitudes.

---

## 4. Resolución de Unknowns

| Unknown | Decision | Rationale |
|---------|----------|-----------|
| ¿Estructura de proyecto? | Clean Architecture (4 capas) | Escala bien con 18 FRs; facilita testing; compatible con SK en Infrastructure |
| ¿Cómo implementar concurrencia (FR-018)? | `ROWVERSION` + EF Core optimistic concurrency | Nativo en SQL Server 2022; sin locks; EF maneja `DbUpdateConcurrencyException` |
| ¿Cómo manejar campos dinámicos de tipos de solicitud? | JSON column (`nvarchar(max)`) | Suficiente para tipos texto/número/fecha/lista de v1; evita complejidad de EAV |
| ¿Cómo implementar auditoría inmutable? | EF Core Interceptor + DB role sin UPDATE/DELETE | Auditoría automática sin código repetido; garantía a nivel DB |
| ¿Cómo manejar expiración de borradores? | .NET Hosted Service (BackgroundService) | Nativo en ASP.NET Core; sin dependencia de scheduler externo |
| ¿Estructura de carpetas frontend? | Feature-based | Alineada con User Stories; escalable; permite lazy loading por feature |
| ¿Autenticación externa vs. interna? | JWT interno reemitido por backend | Per Assumptions del spec; backend es el issuer y valida credenciales corporativas |
