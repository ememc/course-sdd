---
description: "Task list template for feature implementation"
---

# Tasks: Sistema de Gestión de Solicitudes Internas

**Input**: Design documents from `/specs/001-gestion-solicitudes-internas/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Initialize ASP.NET Core 8 Web API project in `backend/src/API/`
- [ ] T002 Initialize React 18 + TypeScript + Vite project in `frontend/`
- [ ] T003 [P] Configure Serilog and Error Handling middleware in `backend/src/API/Middleware/`
- [ ] T004 [P] Setup React Router v6 and Axios interceptors in `frontend/src/shared/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Setup EF Core 8 `AppDbContext` and migrations in `backend/src/Infrastructure/Persistence/`
- [ ] T006 [P] Create base `Usuario` and `RolUsuario` models in `backend/src/Domain/`
- [ ] T007 [P] Implement JWT Authentication and `CurrentUserService` in `backend/src/Infrastructure/Identity/`
- [ ] T008 Configure CQRS/MediatR pipeline and validation in `backend/src/Application/`
- [ ] T009 Implement `AuditSaveChangesInterceptor` for global auditing in `backend/src/Infrastructure/Persistence/Interceptors/`
- [ ] T010 [P] Implement `AuthContext` and Login UI in `frontend/src/features/auth/`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Empleado Registra una Solicitud (Priority: P1) 🎯 MVP

**Goal**: Empleado puede registrar solicitudes con borrador, completarlas y enviarlas.

**Independent Test**: Iniciar sesión como empleado, crear una solicitud de prueba, guardarla como borrador, completarla y enviarla para verificar que pase a estado "Enviada".

### Implementation for User Story 1

- [ ] T011 [P] [US1] Create `Solicitud`, `TipoSolicitud` and `EstadoSolicitud` models in `backend/src/Domain/`
- [ ] T012 [US1] Implement Create/Update Borrador Commands in `backend/src/Application/Solicitudes/Commands/`
- [ ] T013 [US1] Implement Submit Solicitud Command in `backend/src/Application/Solicitudes/Commands/`
- [ ] T014 [US1] Add endpoint `POST /api/solicitudes` in `backend/src/API/Controllers/SolicitudesController.cs`
- [ ] T015 [P] [US1] Build Solicitud Form UI with React Hook Form + Zod in `frontend/src/features/solicitudes/`
- [ ] T016 [US1] Implement auto-save (borrador) logic in frontend using TanStack Query in `frontend/src/features/solicitudes/`
- [ ] T017 [P] [US1] Implement `BorradorCleanupService` (Hosted Service) in `backend/src/Infrastructure/BackgroundServices/` to delete 3-day old drafts.

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Supervisor Aprueba o Rechaza una Solicitud (Priority: P1)

**Goal**: Supervisor puede tomar una solicitud (concurrencia optimista), aprobarla o rechazarla (con comentario obligatorio).

**Independent Test**: Iniciar sesión como supervisor, tomar una solicitud "Enviada" y rechazarla con comentario, verificando los cambios de estado en BD.

### Implementation for User Story 2

- [ ] T018 [P] [US2] Add `rowversion` for optimistic concurrency to `Solicitud` in EF Core config `backend/src/Infrastructure/Persistence/Configurations/`
- [ ] T019 [US2] Implement Tomar, Aprobar, Rechazar Commands in `backend/src/Application/Solicitudes/Commands/`
- [ ] T020 [US2] Add endpoints for state changes in `backend/src/API/Controllers/SolicitudesController.cs`
- [ ] T021 [P] [US2] Build Supervisor Dashboard UI (Pendientes) in `frontend/src/features/supervisor/`
- [ ] T022 [US2] Build Solicitud Detail View with Aprobar/Rechazar buttons in `frontend/src/features/supervisor/`
- [ ] T023 [US2] Integrate concurrency error handling in frontend (show "Ya tomada" message)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Administrador Configura Tipos de Solicitud (Priority: P2)

**Goal**: Administrador puede crear, editar, activar y desactivar tipos de solicitud y sus campos.

**Independent Test**: Iniciar sesión como administrador, crear un tipo de solicitud con campos dinámicos, y verificar que aparece para los empleados.

### Implementation for User Story 3

- [ ] T024 [P] [US3] Implement CRUD Commands/Queries for `TipoSolicitud` in `backend/src/Application/TiposSolicitud/`
- [ ] T025 [US3] Add validation to prevent deleting in-use TiposSolicitud in `backend/src/Application/TiposSolicitud/Commands/`
- [ ] T026 [US3] Add endpoints in `backend/src/API/Controllers/TiposSolicitudController.cs`
- [ ] T027 [P] [US3] Build Admin UI for Tipos de Solicitud (Dynamic form builder) in `frontend/src/features/admin/tipos-solicitud/`
- [ ] T028 [US3] Integrate TipoSolicitud dynamic fields into Employee registration form (US1 integration)

**Checkpoint**: Admin features should be fully functional and integrated with US1

---

## Phase 6: User Story 4 - Todos los Usuarios Consultan Estado e Historial (Priority: P2)

**Goal**: Consulta del historial y bitácora de auditoría con base en el rol (empleado, supervisor, admin).

**Independent Test**: Iniciar sesión con diferentes roles y verificar la visibilidad correcta del listado.

### Implementation for User Story 4

- [ ] T029 [P] [US4] Implement `ListarSolicitudes` Query with filters and role-based scoping in `backend/src/Application/Solicitudes/Queries/`
- [ ] T030 [US4] Implement `ObtenerAuditoria` Query for a specific Solicitud in `backend/src/Application/Solicitudes/Queries/`
- [ ] T031 [US4] Add GET endpoints to `backend/src/API/Controllers/SolicitudesController.cs`
- [ ] T032 [P] [US4] Build unified History/List Table UI in `frontend/src/features/solicitudes/` (adapts to roles)
- [ ] T033 [US4] Build Auditoría timeline component in `frontend/src/features/solicitudes/`

**Checkpoint**: Querying and visualization fully functional

---

## Phase 7: User Story 5 - Notificaciones Básicas (Priority: P3)

**Goal**: Notificaciones in-app basadas en eventos de dominio (nueva solicitud, aprobación, rechazo).

**Independent Test**: Crear solicitud como empleado y verificar que el supervisor ve la notificación badge.

### Implementation for User Story 5

- [ ] T034 [P] [US5] Implement `INotificationService` and `Notificacion` entity in `backend/src/Infrastructure/Notifications/`
- [ ] T035 [US5] Add Domain Event handlers for `SolicitudEnviada`, `SolicitudAprobada`, `SolicitudRechazada` in `backend/src/Application/Solicitudes/Events/`
- [ ] T036 [US5] Add endpoint `GET /api/notificaciones` in `backend/src/API/Controllers/NotificacionesController.cs`
- [ ] T037 [P] [US5] Build Notification dropdown/badge UI in `frontend/src/features/notificaciones/`
- [ ] T038 [US5] Implement 30s polling for notifications in React using TanStack Query

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T039 [P] Refactor Admin Reassignment: Commands/UI to reassign supervisor (`UsuariosController`)
- [ ] T040 Write unit tests for Command Handlers (especially optimistic concurrency and reassignments)
- [ ] T041 Write E2E Playwright tests following `quickstart.md` scenarios
- [ ] T042 Performance optimization: verify `seed-500.sql` (SC-005)
