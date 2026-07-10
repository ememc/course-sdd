# Data Model: Sistema de Gestión de Solicitudes Internas

**Branch**: `001-gestion-solicitudes-internas` | **Date**: 2026-07-09

---

## Diagrama de Entidades

```
┌─────────────────┐       ┌──────────────────────┐       ┌────────────────────┐
│     Usuario     │       │      Solicitud        │       │  TipoSolicitud     │
├─────────────────┤       ├──────────────────────┤       ├────────────────────┤
│ Id (PK)         │──┐    │ Id (PK)              │──┐    │ Id (PK)            │
│ Nombre          │  │    │ TipoSolicitudId (FK) │──┼───►│ Nombre             │
│ Email           │  │    │ EmpleadoId (FK)      │◄─┘    │ Descripcion        │
│ RolId (FK)      │  └───►│ SupervisorAsignadoId │       │ CamposDefinicion   │
│ SupervisorId FK │        │ Estado               │       │ (JSON)             │
│ AreaId (FK)     │        │ CamposDinamicos(JSON)│       │ Activo             │
│ Activo          │        │ FechaCreacion        │       │ FechaCreacion      │
│ FechaCreacion   │        │ FechaEnvio           │       │ CreadoPor (FK)     │
│ FechaDesact.    │        │ FechaResolucion      │       └────────────────────┘
└─────────────────┘        │ ComentarioSupervisor │
        │                  │ RowVersion           │
        │              ┌───┴──────────────────────┘
        │              │
        │    ┌─────────▼──────────┐       ┌────────────────────┐
        │    │  EventoAuditoria   │       │   Notificacion     │
        │    ├────────────────────┤       ├────────────────────┤
        │    │ Id (PK)            │       │ Id (PK)            │
        │    │ SolicitudId (FK)   │       │ DestinatarioId(FK) │
        │    │ UsuarioId (FK)     │       │ SolicitudId (FK)   │
        │    │ EstadoAnterior     │       │ Tipo               │
        │    │ EstadoNuevo        │       │ Contenido          │
        │    │ Accion             │       │ Leida              │
        │    │ FechaHora          │       │ FechaGeneracion    │
        │    │ Metadata (JSON)    │       └────────────────────┘
        │    └────────────────────┘
        │
        ▼
┌─────────────────┐       ┌──────────────────────┐
│      Rol        │       │    Area              │
├─────────────────┤       ├──────────────────────┤
│ Id (PK)         │       │ Id (PK)              │
│ Nombre          │       │ Nombre               │
│ Descripcion     │       │ Descripcion          │
└─────────────────┘       └──────────────────────┘
```

---

## Entidades Detalladas

### `Usuario`

| Campo | Tipo | Restricciones | Notas |
|-------|------|--------------|-------|
| `Id` | `Guid` | PK, NOT NULL | NEWSEQUENTIALID() para índices eficientes |
| `Nombre` | `nvarchar(200)` | NOT NULL | Nombre completo |
| `Email` | `nvarchar(320)` | NOT NULL, UNIQUE | Identificador de login |
| `PasswordHash` | `nvarchar(512)` | NOT NULL | Bcrypt; NULL si auth externa |
| `RolId` | `Guid` | FK → Rol, NOT NULL | Un solo rol activo por usuario (per Assumptions) |
| `SupervisorId` | `Guid?` | FK → Usuario (self-ref), NULL | NULL para supervisores y admins |
| `AreaId` | `Guid` | FK → Area, NOT NULL | Departamento organizacional |
| `Activo` | `bit` | NOT NULL, DEFAULT 1 | Soft delete |
| `FechaCreacion` | `datetime2` | NOT NULL | UTC |
| `FechaDesactivacion` | `datetime2?` | NULL | Fecha en que fue desactivado |

**Reglas de negocio**:
- Un supervisor no puede ser desactivado si tiene solicitudes en estado `Enviada` o `En Revisión` (FR-016).
- `SupervisorId` solo aplica a usuarios con rol `Empleado`.

**Índices**:
- `IX_Usuario_Email` (UNIQUE)
- `IX_Usuario_RolId_Activo`
- `IX_Usuario_SupervisorId` (para listar empleados de un supervisor)

---

### `TipoSolicitud`

| Campo | Tipo | Restricciones | Notas |
|-------|------|--------------|-------|
| `Id` | `Guid` | PK, NOT NULL | |
| `Nombre` | `nvarchar(200)` | NOT NULL, UNIQUE | |
| `Descripcion` | `nvarchar(1000)` | NULL | |
| `CamposDefinicion` | `nvarchar(max)` | NOT NULL | JSON: array de `{ nombre, tipo, requerido, opciones? }` |
| `Activo` | `bit` | NOT NULL, DEFAULT 1 | |
| `FechaCreacion` | `datetime2` | NOT NULL | UTC |
| `CreadoPorId` | `Guid` | FK → Usuario | Administrador que lo creó |

**Tipos de campo soportados en `CamposDefinicion`**: `texto`, `numero`, `fecha`, `lista`

**Reglas de negocio**:
- No puede eliminarse si tiene solicitudes asociadas (activas o históricas) — FR-006.
- Solo administradores pueden crear, editar, activar/desactivar — FR-005.

**Índices**:
- `IX_TipoSolicitud_Activo`

---

### `Solicitud`

| Campo | Tipo | Restricciones | Notas |
|-------|------|--------------|-------|
| `Id` | `Guid` | PK, NOT NULL | |
| `TipoSolicitudId` | `Guid` | FK → TipoSolicitud, NOT NULL | |
| `EmpleadoId` | `Guid` | FK → Usuario, NOT NULL | Empleado que registró la solicitud |
| `SupervisorAsignadoId` | `Guid?` | FK → Usuario, NULL | Supervisor asignado a esta solicitud específica (puede diferir del supervisor organizacional del empleado) |
| `Estado` | `nvarchar(20)` | NOT NULL | Enum: `Borrador`, `Enviada`, `EnRevision`, `Aprobada`, `Rechazada`, `Cancelada` |
| `CamposDinamicos` | `nvarchar(max)` | NOT NULL | JSON: `{ [campoNombre]: valor }` — validado contra `TipoSolicitud.CamposDefinicion` |
| `ComentarioSupervisor` | `nvarchar(2000)` | NULL | Obligatorio cuando Estado = `Rechazada` |
| `FechaCreacion` | `datetime2` | NOT NULL | UTC; asignada al crear el borrador |
| `FechaEnvio` | `datetime2?` | NULL | Asignada al transicionar a `Enviada` |
| `FechaResolucion` | `datetime2?` | NULL | Asignada al transicionar a `Aprobada`, `Rechazada` o `Cancelada` |
| `UltimaModificacion` | `datetime2` | NOT NULL | Actualizada en cada PATCH de borrador |
| `RowVersion` | `rowversion` | NOT NULL | Control de concurrencia optimista (FR-018) |

**Transiciones de estado válidas**:

| Estado Actual | Estado Nuevo | Actor permitido | Condición |
|--------------|-------------|----------------|-----------|
| `Borrador` | `Enviada` | Empleado dueño | Campos requeridos completos |
| `Enviada` | `EnRevision` | Supervisor del área | Concurrencia: ROWVERSION; primero gana |
| `Enviada` | `Cancelada` | Empleado dueño | Solo desde `Enviada` (FR-011) |
| `EnRevision` | `Aprobada` | Supervisor que tomó la solicitud | |
| `EnRevision` | `Rechazada` | Supervisor que tomó la solicitud | ComentarioSupervisor obligatorio |

**Reglas de negocio**:
- Borradores con `FechaCreacion < GETUTCDATE() - 3 días` son eliminados por el job de limpieza.
- `SupervisorAsignadoId` se establece automáticamente como el supervisor organizacional del empleado al crear la solicitud; puede reasignarse por admin.
- Un empleado no puede modificar una solicitud en estado ≠ `Borrador`; solo puede cancelarla desde `Enviada`.

**Índices**:
- `IX_Solicitud_EmpleadoId_FechaCreacion`
- `IX_Solicitud_SupervisorAsignadoId_Estado` (crítico para lista de pendientes del supervisor)
- `IX_Solicitud_Estado_TipoSolicitudId`
- `IX_Solicitud_FechaCreacion_Estado` (para limpieza de borradores expirados)

---

### `EventoAuditoria`

| Campo | Tipo | Restricciones | Notas |
|-------|------|--------------|-------|
| `Id` | `Guid` | PK, NOT NULL | |
| `SolicitudId` | `Guid` | FK → Solicitud, NOT NULL | |
| `UsuarioId` | `Guid` | FK → Usuario, NOT NULL | Quien realizó la acción |
| `EstadoAnterior` | `nvarchar(20)` | NULL | NULL para el primer evento (creación) |
| `EstadoNuevo` | `nvarchar(20)` | NOT NULL | |
| `Accion` | `nvarchar(100)` | NOT NULL | e.g., `Creada`, `Enviada`, `Tomada`, `Aprobada`, `Rechazada`, `Cancelada`, `Reasignada` |
| `FechaHora` | `datetime2` | NOT NULL | UTC; asignada por servidor, no por cliente |
| `Metadata` | `nvarchar(max)` | NULL | JSON: contexto adicional (e.g., `{ "supervisorAnterior": "...", "supervisorNuevo": "..." }`) |

**Invariantes** (garantizados a nivel aplicación + DB):
- Ningún `UPDATE` ni `DELETE` permitido (EF Core: entidad sin setters tras creación; DB: permisos de rol).
- `FechaHora` asignada por el servidor (UTC); nunca por el cliente.
- Generado automáticamente por `AuditSaveChangesInterceptor` en EF Core.

**Índices**:
- `IX_EventoAuditoria_SolicitudId_FechaHora` (consultas de historial)
- `IX_EventoAuditoria_UsuarioId_FechaHora`

---

### `Notificacion`

| Campo | Tipo | Restricciones | Notas |
|-------|------|--------------|-------|
| `Id` | `Guid` | PK, NOT NULL | |
| `DestinatarioId` | `Guid` | FK → Usuario, NOT NULL | |
| `SolicitudId` | `Guid` | FK → Solicitud, NOT NULL | |
| `Tipo` | `nvarchar(50)` | NOT NULL | Enum: `NuevaSolicitud`, `SolicitudAprobada`, `SolicitudRechazada` |
| `Contenido` | `nvarchar(500)` | NOT NULL | Mensaje legible |
| `Leida` | `bit` | NOT NULL, DEFAULT 0 | |
| `FechaGeneracion` | `datetime2` | NOT NULL | UTC |

**Índices**:
- `IX_Notificacion_DestinatarioId_Leida_FechaGeneracion` (badge + lista de no leídas)

---

### `Rol`

| Campo | Tipo | Restricciones |
|-------|------|--------------|
| `Id` | `Guid` | PK |
| `Nombre` | `nvarchar(50)` | NOT NULL, UNIQUE |
| `Descripcion` | `nvarchar(200)` | NULL |

**Valores seed**: `Empleado`, `Supervisor`, `Administrador`

---

### `Area`

| Campo | Tipo | Restricciones |
|-------|------|--------------|
| `Id` | `Guid` | PK |
| `Nombre` | `nvarchar(200)` | NOT NULL, UNIQUE |
| `Descripcion` | `nvarchar(500)` | NULL |

---

## Matriz de Permisos por Rol

| Operación | Empleado | Supervisor | Administrador |
|-----------|----------|------------|---------------|
| Crear solicitud (borrador) | ✅ propio | ❌ | ❌ |
| Enviar solicitud | ✅ propio | ❌ | ❌ |
| Cancelar solicitud | ✅ propio (solo Enviada) | ❌ | ❌ |
| Tomar solicitud | ❌ | ✅ de su área | ❌ |
| Aprobar/Rechazar | ❌ | ✅ tomadas por él | ❌ |
| Ver historial propio | ✅ | ✅ | ✅ |
| Ver solicitudes de área | ❌ | ✅ | ✅ |
| Ver todas las solicitudes | ❌ | ❌ | ✅ |
| Configurar tipos de solicitud | ❌ | ❌ | ✅ |
| Reasignar supervisor (org) | ❌ | ❌ | ✅ |
| Reasignar solicitud | ❌ | ❌ | ✅ |
| Ver auditoría completa | ❌ | ❌ | ✅ |

---

## Estrategia de Migración

1. **Seed inicial**: `Rol` (3 registros) + `Area` (datos de prueba) + Usuario admin por defecto.
2. **Índices**: aplicados en la misma migración que crea cada tabla.
3. **Permisos de DB**: script post-migración que revoca `UPDATE`/`DELETE` sobre `EventosAuditoria` al rol de aplicación.
4. **Evolución de campos dinámicos**: validación solo a nivel aplicación en v1; migración futura podría normalizar a tabla `CampoValor` si se requieren consultas cross-solicitud sobre campos dinámicos.
