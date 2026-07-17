# Quickstart & Validation Guide: Sistema de Gestión de Solicitudes Internas

**Branch**: `001-gestion-solicitudes-internas` | **Date**: 2026-07-09

---

## Prerrequisitos

| Herramienta | Versión mínima | Verificación |
|-------------|----------------|--------------|
| .NET SDK | 8.0 | `dotnet --version` |
| Node.js | 20 LTS | `node --version` |
| SQL Server | 2022 (local o Docker) | `sqlcmd -Q "SELECT @@VERSION"` |
| Git | 2.40+ | `git --version` |

**SQL Server local con Docker** (alternativa rápida):
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Dev@12345!" \
  -p 1433:1433 --name sqlserver-dev -d mcr.microsoft.com/mssql/server:2022-latest
```

---

## Setup Inicial

### 1. Clonar y configurar

```bash
git clone <repo-url>
git checkout 001-gestion-solicitudes-internas
```

### 2. Backend — configuración

```bash
cd backend/src/API
cp appsettings.Development.json.example appsettings.Development.json
```

Editar `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=GestionSolicitudes;User Id=sa;Password=Dev@12345!;TrustServerCertificate=True"
  },
  "Jwt": {
    "SecretKey": "dev-secret-key-minimum-32-characters!!",
    "Issuer": "gestion-solicitudes-api",
    "ExpiresInMinutes": 15
  }
}
```

### 3. Ejecutar migraciones y seed

```bash
cd backend
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

El seed inicial crea:
- 3 roles: `Empleado`, `Supervisor`, `Administrador`
- 1 área: `General`
- Usuarios de prueba (ver tabla abajo)

### 4. Iniciar backend

```bash
cd backend
dotnet run --project src/API
# Escucha en: https://localhost:5001 y http://localhost:5000
# Swagger UI: https://localhost:5001/swagger
```

### 5. Iniciar frontend

```bash
cd frontend
npm install
npm run dev
# Disponible en: http://localhost:5173
```

---

## Usuarios de Prueba (Seed)

| Email | Password | Rol | Área | Supervisor |
|-------|----------|-----|------|------------|
| `admin@test.com` | `Admin@123!` | Administrador | General | — |
| `supervisor1@test.com` | `Sup@123!` | Supervisor | General | — |
| `supervisor2@test.com` | `Sup@123!` | Supervisor | General | — |
| `empleado1@test.com` | `Emp@123!` | Empleado | General | supervisor1 |
| `empleado2@test.com` | `Emp@123!` | Empleado | General | supervisor1 |
| `empleado3@test.com` | `Emp@123!` | Empleado | General | supervisor2 |

---

## Escenarios de Validación

### Escenario 1 — Flujo completo P1: Empleado registra y Supervisor aprueba

**Valida**: FR-001, FR-002, FR-003, FR-004, FR-009, FR-010, SC-001, SC-002

```
1. Login como admin@test.com
2. Crear tipo de solicitud "Permiso de Ausencia" con campo "Motivo" (texto, requerido)
3. Activar el tipo → verificar respuesta 200 con activo: true

4. Login como empleado1@test.com
5. POST /api/v1/solicitudes → { tipoSolicitudId: "<id>" } → Estado: Borrador (capturar rowVersion)
6. PATCH /api/v1/solicitudes/{id}/borrador (con If-Match: "<rowVersion>") → { camposDinamicos: { "Motivo": "Cita médica" } }
7. POST /api/v1/solicitudes/{id}/enviar (con If-Match: "<rowVersion>") → Estado: Enviada
   ✓ Verificar que supervisorAsignadoId = supervisor1

8. Login como supervisor1@test.com
9. GET /api/v1/notificaciones → verificar notificación "NuevaSolicitud" (SC-002: < 2 min)
10. POST /api/v1/solicitudes/{id}/tomar (con If-Match: "<rowVersion>") → Estado: EnRevision
11. POST /api/v1/solicitudes/{id}/aprobar → Estado: Aprobada

12. Login como empleado1@test.com
13. GET /api/v1/notificaciones → verificar notificación "SolicitudAprobada"
14. GET /api/v1/solicitudes/{id}/auditoria → verificar 4 eventos: Creada, Enviada, Tomada, Aprobada
    ✓ Cada evento tiene usuario, fechaHora, estadoAnterior, estadoNuevo
```

**Criterio de éxito**: todos los pasos retornan 200/201 sin errores; auditoría contiene exactamente 4 eventos inmutables.

---

### Escenario 2 — Rechazo con comentario obligatorio

**Valida**: FR-004, SC-006

```
1. Crear solicitud en estado EnRevision (como en Escenario 1, pasos 4-10)
2. Login como supervisor1@test.com
3. POST /api/v1/solicitudes/{id}/rechazar → body vacío
   ✓ Esperar 422 Unprocessable Entity

4. POST /api/v1/solicitudes/{id}/rechazar → { "comentario": "Insuficiente documentación" }
   ✓ Esperar 200, estado: Rechazada, comentarioSupervisor presente

5. Login como empleado1@test.com
6. GET /api/v1/notificaciones → notificación "SolicitudRechazada" con motivo incluido
```

---

### Escenario 3 — Concurrencia: dos supervisores intentan tomar la misma solicitud (FR-018)

**Valida**: FR-018, Clarification Q5

```
1. Crear solicitud en estado Enviada bajo supervisor1 y supervisor2 del mismo área
   (Ajustar seed para que ambos supervisores pertenezcan al área del empleado1)

2. Login supervisor1: GET /api/v1/solicitudes/{id} → capturar rowVersion "v1"
3. Login supervisor2: GET /api/v1/solicitudes/{id} → capturar rowVersion "v1" (igual)

4. supervisor1: POST /api/v1/solicitudes/{id}/tomar con If-Match: "v1"
   ✓ Esperar 200, estado: EnRevision

5. supervisor2: POST /api/v1/solicitudes/{id}/tomar con If-Match: "v1"
   ✓ Esperar 409, mensaje: "La solicitud ya fue tomada por otro supervisor"
   ✓ La solicitud ya no aparece en GET /api/v1/solicitudes de supervisor2 (pendientes)
```

---

### Escenario 4 — Borrador con autoguardado y expiración (FR-017)

**Valida**: FR-017, Clarification Q4

```
1. Login empleado1@test.com
2. POST /api/v1/solicitudes → Estado: Borrador, anotar fechaCreacion y rowVersion
3. PATCH /api/v1/solicitudes/{id}/borrador (con If-Match: "<rowVersion>") → guardar datos parciales → 200 OK

4. Simular expiración: UPDATE Solicitudes SET FechaCreacion = DATEADD(day, -4, GETUTCDATE())
   WHERE Id = '{id}' (directo en DB para test)

5. Ejecutar el job de limpieza manualmente:
   POST /api/v1/admin/jobs/limpiar-borradores (endpoint interno, solo dev)

6. GET /api/v1/solicitudes/{id}
   ✓ Esperar 404 — borrador eliminado
```

---

### Escenario 5 — Bloqueo de desactivación de supervisor con reasignación en bloque (FR-016)

**Valida**: FR-016, FR-015, Clarification Q2

```
1. Login admin@test.com
2. Crear solicitud en estado EnRevision asignada a supervisor1

3. POST /api/v1/usuarios/{supervisor1Id}/desactivar
   ✓ Esperar 409 con lista de solicitudes pendientes (la solicitud en EnRevision)

4. POST /api/v1/solicitudes/reasignar → { "solicitudIds": ["{id}"], "nuevoSupervisorId": "{supervisor2Id}" }
   ✓ Esperar 200, reasignadas: 1

5. POST /api/v1/usuarios/{supervisor1Id}/desactivar
   ✓ Esperar 204 — desactivación exitosa

6. GET /api/v1/solicitudes/{id}
   ✓ supervisorAsignado.id = supervisor2Id
   ✓ GET /api/v1/solicitudes/{id}/auditoria → evento "Reasignada" registrado
```

---

### Escenario 6 — Visibilidad por rol (FR-007, SC-004)

**Valida**: FR-007, SC-004

```
1. Login empleado1@test.com
2. GET /api/v1/solicitudes
   ✓ Solo solicitudes del empleado1 (ninguna de empleado2 o empleado3)

3. Login supervisor1@test.com
4. GET /api/v1/solicitudes
   ✓ Solo solicitudes de empleado1 y empleado2 (sus empleados)
   ✗ NO debe ver solicitudes de empleado3 (pertenece a supervisor2)

5. Login admin@test.com
6. GET /api/v1/solicitudes
   ✓ Todas las solicitudes del sistema

7. Intentar acceso cruzado:
   empleado1: GET /api/v1/solicitudes/{id_de_empleado2}
   ✓ Esperar 404 (no exponer que existe)
```

---

### Escenario 7 — Performance: historial con filtros (SC-005)

**Valida**: SC-005

```
Prerrequisito: seed de 500+ solicitudes con estados y fechas variadas (script en /backend/tests/performance/seed-500.sql)

1. Login admin@test.com
2. GET /api/v1/solicitudes?page=1&pageSize=20&estado=Aprobada
3. Medir tiempo de respuesta con curl o Postman
   ✓ Debe ser < 5 segundos para 500 solicitudes (SC-005)
   ✓ Verificar en respuesta: total, page, pageSize correctos
```

---

## Verificación de CI/CD

```bash
# Ejecutar suite de tests
cd backend
dotnet test --configuration Release --no-build

# Verificar cobertura mínima
dotnet test --collect:"XPlat Code Coverage"

# Frontend lint
cd frontend
npm run lint
npm run type-check
```

**Pipeline GitHub Actions**: se dispara automáticamente en cada PR contra `main`. Ver `.github/workflows/ci.yml` para detalle de stages.

---

## Referencias

- [Contratos de API](./contracts/api.md)
- [Modelo de Datos](./data-model.md)
- [Investigación Técnica](./research.md)
- [Especificación](./spec.md)
