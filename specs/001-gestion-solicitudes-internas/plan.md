# Implementation Plan: Sistema de Gestión de Solicitudes Internas

**Branch**: `001-gestion-solicitudes-internas` | **Date**: 2026-07-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-gestion-solicitudes-internas/spec.md`

---

## Summary

Sistema web full-stack para gestión del ciclo de vida de solicitudes internas organizacionales. Permite a empleados registrar solicitudes con autoguardado de borrador, supervisores aprobarlas/rechazarlas con control de concurrencia optimista, y administradores configurar tipos de solicitud y gestionar reasignaciones. El sistema garantiza trazabilidad completa mediante auditoría inmutable y notificaciones in-app.

**Stack**: .NET 8 Web API (Clean Architecture + CQRS/MediatR) · React 18 + TypeScript · SQL Server 2022 (EF Core 8) · JWT · GitHub Actions

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 LTS (backend) · TypeScript 5 / React 18 (frontend)

**Primary Dependencies**:
- Backend: ASP.NET Core 8, MediatR, FluentValidation, EF Core 8, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.RateLimiting`, Serilog
- Frontend: React 18, Vite, TanStack Query, React Hook Form, Zod, Axios, React Router v6

**Storage**: SQL Server 2022 — Code-First con EF Core 8 migrations; `rowversion` para concurrencia; JSON columns para campos dinámicos de tipos de solicitud; Soft delete global vía `HasQueryFilter`

**Testing**:
- Backend: xUnit + Moq + FluentAssertions (unit); Testcontainers para SQL Server (integration)
- Frontend: Vitest + React Testing Library (unit); Playwright (E2E)

**Target Platform**: Web — Windows Server / Linux container (backend); navegadores modernos (Chrome, Edge, Firefox) — frontend SPA

**Project Type**: Web application (REST API + SPA)

**Performance Goals**:
- SC-001: Empleado puede registrar y enviar solicitud en < 3 minutos (UX)
- SC-002: Notificación al supervisor en < 2 minutos tras envío (polling 30s)
- SC-005: Historial con filtros carga en < 5s para hasta 500 solicitudes

**Constraints**:
- Single-tenant (v1)
- Un solo rol activo por usuario
- Notificaciones in-app únicamente (no email/SMS)
- Campos de solicitud: texto, número, fecha, lista (sin adjuntos ni firmas en v1)
- Datos históricos conservados indefinidamente (sin política de borrado)
- Borradores expiran a los 3 días calendario

**Scale/Scope**: Sistema interno organizacional; volumen de v1 asumido ≤ 500 solicitudes activas concurrentes (SC-005 como benchmark de escala)

---

## Constitution Check

> La `constitution.md` del proyecto contiene solo la plantilla vacía sin principios definidos. No hay gates configurados. Se omite esta sección sin bloqueo.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-gestion-solicitudes-internas/
├── plan.md              ← Este archivo (/speckit.plan)
├── research.md          ← Decisiones técnicas y justificaciones (Phase 0)
├── data-model.md        ← Entidades, relaciones, índices, permisos (Phase 1)
├── contracts/
│   └── api.md           ← Contratos REST completos (Phase 1)
├── quickstart.md        ← 7 escenarios de validación E2E (Phase 1)
├── checklists/
│   └── requirements.md  ← Checklist de calidad (16/16 ✅)
└── tasks.md             ← Pendiente (/speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── API/                         # ASP.NET Core Web API — controllers, middleware, DI
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── SolicitudesController.cs
│   │   │   ├── TiposSolicitudController.cs
│   │   │   ├── UsuariosController.cs
│   │   │   └── NotificacionesController.cs
│   │   ├── Middleware/              # JWT, error handling, rate limiting
│   │   └── Program.cs
│   ├── Application/                 # CQRS: Commands, Queries, Handlers, DTOs, Validators
│   │   ├── Solicitudes/
│   │   │   ├── Commands/            # Crear, Enviar, Tomar, Aprobar, Rechazar, Cancelar, Reasignar
│   │   │   └── Queries/             # ListarSolicitudes, ObtenerDetalle, ObtenerAuditoria
│   │   ├── TiposSolicitud/
│   │   ├── Usuarios/
│   │   ├── Notificaciones/
│   │   └── Common/                  # ICurrentUserService, Result pattern, PaginatedList
│   ├── Domain/                      # Entidades, enums, domain events, value objects
│   │   ├── Entities/                # Solicitud, TipoSolicitud, Usuario, EventoAuditoria, Notificacion
│   │   ├── Enums/                   # EstadoSolicitud, RolUsuario, TipoNotificacion, TipoCampo
│   │   ├── Events/                  # SolicitudEnviada, SolicitudAprobada, SolicitudRechazada, etc.
│   │   └── Interfaces/              # IRepository<T>, IUnitOfWork, INotificationService
│   └── Infrastructure/              # EF Core, JWT, Hosted Services, AI (SK)
│       ├── Persistence/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/      # IEntityTypeConfiguration<T> por entidad
│       │   ├── Interceptors/        # AuditSaveChangesInterceptor
│       │   ├── Migrations/
│       │   └── Repositories/
│       ├── Identity/                # JWT service, CurrentUserService
│       ├── Notifications/           # NotificationService (DB insert)
│       ├── BackgroundServices/      # BorradorCleanupService (Hosted Service)
│       └── AI/                      # Semantic Kernel plugin (deshabilitado por defecto)
└── tests/
    ├── unit/                        # Domain + Application sin DB
    ├── integration/                 # API + DB real (Testcontainers)
    └── performance/                 # seed-500.sql para SC-005

frontend/
├── src/
│   ├── features/
│   │   ├── auth/                    # Login, logout, token refresh
│   │   ├── solicitudes/             # Formulario, historial, detalle, borrador autoguardado
│   │   ├── supervisor/              # Panel de pendientes, tomar/aprobar/rechazar
│   │   ├── admin/
│   │   │   ├── tipos-solicitud/     # CRUD tipos y campos
│   │   │   └── usuarios/            # Gestión, reasignación, desactivación
│   │   └── notificaciones/          # Badge, lista, marcar leídas
│   ├── shared/
│   │   ├── api/                     # Axios instance + interceptors
│   │   ├── components/              # UI compartido (Button, Modal, Table, etc.)
│   │   └── hooks/                   # useCurrentUser, usePagination, useDebounce
│   └── main.tsx
└── tests/
    ├── unit/                        # Vitest + RTL
    └── e2e/                         # Playwright — escenarios del quickstart.md

.github/
└── workflows/
    ├── ci.yml                       # PR: build + test + lint
    └── deploy.yml                   # main merge + tag: staging/prod
```

**Structure Decision**: Web application (Option 2) — backend API + frontend SPA separados. Elegido por separación de responsabilidades, ciclos de deploy independientes y mejor soporte de TypeScript en el cliente.

---

## Complexity Tracking

> No hay violaciones de Constitution Check (constitution sin principios definidos en este proyecto). Sección N/A.

---

## Resumen de Artefactos Generados

| Artefacto | Ruta | Descripción |
|-----------|------|-------------|
| `research.md` | `specs/.../research.md` | Decisiones técnicas, patrones, resolución de unknowns |
| `data-model.md` | `specs/.../data-model.md` | 7 entidades, relaciones, índices, FSM, matriz de permisos |
| `contracts/api.md` | `specs/.../contracts/api.md` | 25+ endpoints REST con request/response y códigos de error |
| `quickstart.md` | `specs/.../quickstart.md` | Setup + 7 escenarios de validación E2E |
