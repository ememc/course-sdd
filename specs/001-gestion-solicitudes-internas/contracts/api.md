# API Contracts: Sistema de Gestión de Solicitudes Internas

**Format**: REST/JSON over HTTPS
**Auth**: JWT Bearer — todos los endpoints requieren `Authorization: Bearer <token>` salvo `/auth/*`
**Base URL**: `/api/v1`
**Date**: 2026-07-09

---

## Convenciones

- IDs: `Guid` (UUID v4), representado como string en JSON
- Fechas: ISO 8601 UTC (`2026-07-09T22:00:00Z`)
- Paginación: `?page=1&pageSize=20` → respuesta con `{ data, total, page, pageSize }`
- Errores: `{ "type": "...", "title": "...", "status": 400, "errors": { "campo": ["mensaje"] } }` (RFC 7807)
- Concurrencia: header `If-Match: "<rowVersion>"` para operaciones que lo requieren

---

## Auth

### `POST /api/v1/auth/login`

**Request**:
```json
{
  "email": "string",
  "password": "string"
}
```

**Response 200**:
```json
{
  "accessToken": "string (JWT)",
  "expiresIn": 900,
  "user": {
    "id": "guid",
    "nombre": "string",
    "email": "string",
    "rol": "Empleado | Supervisor | Administrador",
    "areaId": "guid",
    "areaNombre": "string"
  }
}
```

> Refresh token entregado como `HttpOnly` cookie `refresh_token`.

### `POST /api/v1/auth/refresh`

**Request**: cookie `refresh_token` (automático)

**Response 200**: igual a `/auth/login`

### `POST /api/v1/auth/logout`

**Response 204**: invalida refresh token

---

## Solicitudes

### `GET /api/v1/solicitudes`

Lista solicitudes según rol del usuario autenticado.

**Query params**: `estado`, `tipoSolicitudId`, `fechaDesde`, `fechaHasta`, `empleadoId` (solo admin/supervisor), `page`, `pageSize`

**Response 200**:
```json
{
  "data": [
    {
      "id": "guid",
      "tipo": { "id": "guid", "nombre": "string" },
      "empleado": { "id": "guid", "nombre": "string" },
      "supervisorAsignado": { "id": "guid", "nombre": "string" },
      "estado": "Borrador | Enviada | EnRevision | Aprobada | Rechazada | Cancelada",
      "fechaCreacion": "datetime",
      "fechaEnvio": "datetime | null",
      "fechaResolucion": "datetime | null",
      "rowVersion": "string (base64)"
    }
  ],
  "total": 0,
  "page": 1,
  "pageSize": 20
}
```

---

### `POST /api/v1/solicitudes`

Crea un borrador. Solo rol `Empleado`.

**Request**:
```json
{
  "tipoSolicitudId": "guid"
}
```

**Response 201**:
```json
{
  "id": "guid",
  "estado": "Borrador",
  "fechaCreacion": "datetime",
  "rowVersion": "string (base64)"
}
```

---

### `GET /api/v1/solicitudes/{id}`

Detalle de solicitud. Respeta filtro de visibilidad por rol.

**Response 200**:
```json
{
  "id": "guid",
  "tipo": { "id": "guid", "nombre": "string", "camposDefinicion": [] },
  "empleado": { "id": "guid", "nombre": "string", "area": "string" },
  "supervisorAsignado": { "id": "guid", "nombre": "string" },
  "estado": "string",
  "camposDinamicos": { "campoNombre": "valor" },
  "comentarioSupervisor": "string | null",
  "fechaCreacion": "datetime",
  "fechaEnvio": "datetime | null",
  "fechaResolucion": "datetime | null",
  "ultimaModificacion": "datetime",
  "rowVersion": "string (base64)"
}
```

---

### `PATCH /api/v1/solicitudes/{id}/borrador`

Autoguardado de campos dinámicos. Solo empleado dueño; solo estado `Borrador`.

**Headers**: `If-Match: "<rowVersion>"`

**Request**:
```json
{
  "camposDinamicos": { "campoNombre": "valor" }
}
```

**Response 200**:
```json
{
  "ultimaModificacion": "datetime",
  "rowVersion": "string"
}
```

---

### `POST /api/v1/solicitudes/{id}/enviar`

Transiciona `Borrador → Enviada`. Solo empleado dueño.

**Headers**: `If-Match: "<rowVersion>"`

**Response 200**:
```json
{
  "id": "guid",
  "estado": "Enviada",
  "fechaEnvio": "datetime"
}
```

**Response 409 Conflict**: validación fallida (campos requeridos vacíos).

---

### `POST /api/v1/solicitudes/{id}/tomar`

Transiciona `Enviada → EnRevision`. Solo supervisor del área del empleado.

**Headers**: `If-Match: "<rowVersion>"` — control de concurrencia optimista.

**Response 200**:
```json
{
  "id": "guid",
  "estado": "EnRevision",
  "supervisorAsignado": { "id": "guid", "nombre": "string" }
}
```

**Response 409 Conflict**:
```json
{
  "title": "La solicitud ya fue tomada por otro supervisor.",
  "status": 409,
  "supervisorActual": { "id": "guid", "nombre": "string" }
}
```

---

### `POST /api/v1/solicitudes/{id}/aprobar`

Transiciona `EnRevision → Aprobada`. Solo supervisor que tomó la solicitud.

**Request**: (body vacío)

**Response 200**:
```json
{
  "id": "guid",
  "estado": "Aprobada",
  "fechaResolucion": "datetime"
}
```

---

### `POST /api/v1/solicitudes/{id}/rechazar`

Transiciona `EnRevision → Rechazada`. Solo supervisor que tomó la solicitud.

**Request**:
```json
{
  "comentario": "string (requerido, 10–2000 chars)"
}
```

**Response 200**:
```json
{
  "id": "guid",
  "estado": "Rechazada",
  "comentarioSupervisor": "string",
  "fechaResolucion": "datetime"
}
```

**Response 422**: comentario vacío o muy corto.

---

### `POST /api/v1/solicitudes/{id}/cancelar`

Transiciona `Enviada → Cancelada`. Solo empleado dueño.

**Response 200**:
```json
{
  "id": "guid",
  "estado": "Cancelada",
  "fechaResolucion": "datetime"
}
```

---

### `GET /api/v1/solicitudes/{id}/auditoria`

Historial de eventos de auditoría de la solicitud. Solo empleado dueño, supervisor asignado o admin.

**Response 200**:
```json
{
  "data": [
    {
      "id": "guid",
      "usuario": { "id": "guid", "nombre": "string" },
      "estadoAnterior": "string | null",
      "estadoNuevo": "string",
      "accion": "string",
      "fechaHora": "datetime",
      "metadata": {}
    }
  ]
}
```

---

### `POST /api/v1/solicitudes/reasignar`

Reasignación en bloque de solicitudes. Solo administrador.

**Request**:
```json
{
  "solicitudIds": ["guid", "guid"],
  "nuevoSupervisorId": "guid"
}
```

**Response 200**:
```json
{
  "reasignadas": 2,
  "errores": []
}
```

---

## Tipos de Solicitud

### `GET /api/v1/tipos-solicitud`

**Query**: `activo=true|false` (default: true para empleados, all para admins)

**Response 200**:
```json
{
  "data": [
    {
      "id": "guid",
      "nombre": "string",
      "descripcion": "string",
      "camposDefinicion": [
        { "nombre": "string", "tipo": "texto|numero|fecha|lista", "requerido": true, "opciones": ["A","B"] }
      ],
      "activo": true
    }
  ]
}
```

---

### `POST /api/v1/tipos-solicitud`

Crea tipo de solicitud. Solo administrador.

**Request**:
```json
{
  "nombre": "string",
  "descripcion": "string",
  "camposDefinicion": [
    { "nombre": "string", "tipo": "texto|numero|fecha|lista", "requerido": true, "opciones": [] }
  ]
}
```

**Response 201**: tipo creado (incluye `id`, `activo: false` — requiere activación explícita).

---

### `PATCH /api/v1/tipos-solicitud/{id}`

Edita nombre, descripción o campos. Solo administrador. No aplica a tipos con solicitudes activas si el cambio remueve campos requeridos.

**Response 200**: tipo actualizado.

---

### `POST /api/v1/tipos-solicitud/{id}/activar`

**Response 200**: `{ "id": "guid", "activo": true }`

---

### `POST /api/v1/tipos-solicitud/{id}/desactivar`

**Response 200**: `{ "id": "guid", "activo": false }`

**Response 409**: el tipo tiene solicitudes activas (solo informativo — desactivar es permitido; el tipo no aparece para nuevas solicitudes pero las activas continúan).

---

### `DELETE /api/v1/tipos-solicitud/{id}`

**Response 409**: el tipo tiene solicitudes asociadas (activas o históricas) — FR-006.

**Response 204**: eliminado (solo si no tiene solicitudes).

---

## Usuarios (Admin)

### `GET /api/v1/usuarios`

Solo administrador. Query: `rol`, `areaId`, `activo`, `page`, `pageSize`.

**Response 200**: lista paginada con `id`, `nombre`, `email`, `rol`, `area`, `supervisor`, `activo`.

---

### `PATCH /api/v1/usuarios/{id}/supervisor`

Reasignación organizacional. Solo administrador.

**Request**:
```json
{
  "nuevoSupervisorId": "guid"
}
```

**Response 200**: usuario actualizado.

---

### `POST /api/v1/usuarios/{id}/desactivar`

Solo administrador.

**Response 409**:
```json
{
  "title": "El supervisor tiene solicitudes activas pendientes.",
  "status": 409,
  "solicitudesPendientes": [{ "id": "guid", "empleado": "string", "estado": "string" }]
}
```

**Response 204**: desactivado exitosamente.

---

## Notificaciones

### `GET /api/v1/notificaciones`

Notificaciones del usuario autenticado. Query: `leida=true|false`, `page`, `pageSize`.

**Response 200**:
```json
{
  "data": [
    {
      "id": "guid",
      "tipo": "string",
      "contenido": "string",
      "leida": false,
      "fechaGeneracion": "datetime",
      "solicitud": { "id": "guid" }
    }
  ],
  "noLeidas": 3
}
```

---

### `POST /api/v1/notificaciones/{id}/marcar-leida`

**Response 204**

---

### `POST /api/v1/notificaciones/marcar-todas-leidas`

**Response 204**

---

## Códigos de Error Comunes

| Código | Significado |
|--------|-------------|
| 400 | Validación de request (campos requeridos, formatos) |
| 401 | Token inválido o expirado |
| 403 | Rol sin permisos para la operación o recurso |
| 404 | Recurso no encontrado o fuera del alcance del usuario |
| 409 | Conflicto de negocio (concurrencia, solicitudes activas, etc.) |
| 412 | Precondition Failed — `If-Match` no coincide (rowVersion obsoleto) |
| 422 | Entidad no procesable (validación de negocio: comentario requerido, estado inválido) |
| 429 | Rate limit alcanzado (60 req/min por usuario) |
