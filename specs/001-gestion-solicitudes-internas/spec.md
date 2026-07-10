# Feature Specification: Sistema de Gestión de Solicitudes Internas

**Feature Branch**: `001-gestion-solicitudes-internas`

**Created**: 2026-07-09

**Status**: Draft

**Input**: User description: "Crear un Sistema de Gestión de Solicitudes internas que permita a empleados registrar solicitudes, supervisores aprobar o rechazar solicitudes, administradores configurar tipos de solicitud y todos los usuarios consultar el estado e historial. El sistema debe mantener trazabilidad, auditoría, roles y notificaciones básicas."

## Clarifications

### Session 2026-07-09

- Q: ¿Quién activa el estado "En Revisión" y bajo qué condición? → A: El estado cambia a "En Revisión" manualmente cuando el supervisor hace clic en un botón explícito de "Tomar solicitud". Esta acción queda registrada en la bitácora de auditoría.
- Q: ¿Qué ocurre si el supervisor asignado a un empleado es dado de baja del sistema mientras tiene solicitudes pendientes de revisión? → A: El sistema bloquea la desactivación del supervisor mientras tenga solicitudes activas (en estado "Enviada" o "En Revisión"). El administrador debe reasignar esas solicitudes en bloque —seleccionando una o varias— a uno o más supervisores (existentes o nuevos) antes de que la desactivación pueda completarse.
- Q: Cuando un administrador reasigna un supervisor, ¿a qué nivel aplica la reasignación? → A: Ambos niveles de forma independiente: (1) a nivel de empleado, cambiar el supervisor organizacional afecta todas las solicitudes futuras de ese empleado pero no las ya existentes; (2) a nivel de solicitud, reasignar una o varias solicitudes específicas a otro supervisor no altera la relación organizacional del empleado.
- Q: ¿El sistema debe permitir guardar un borrador sin enviar? → A: Sí, mediante autoguardado: el sistema guarda automáticamente el progreso del formulario (estado "Borrador"); el empleado debe enviar explícitamente. El borrador no notifica al supervisor. Si no se envía en 3 días calendario, el borrador expira y se elimina automáticamente.
- Q: ¿Qué ocurre si dos supervisores intentan tomar la misma solicitud simultáneamente? → A: Primero en llegar gana: el primer supervisor que hace clic en "Tomar solicitud" la bloquea y transiciona a "En Revisión"; el segundo recibe un mensaje informativo de que la solicitud ya fue tomada y la solicitud desaparece de su lista de pendientes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Empleado Registra una Solicitud (Priority: P1)

Un empleado autenticado accede al sistema, selecciona el tipo de solicitud que necesita (por ejemplo, permiso de ausencia, solicitud de material, reembolso de gastos), completa el formulario correspondiente y lo envía para revisión de su supervisor.

**Why this priority**: Es el punto de entrada principal del sistema. Sin la capacidad de registrar solicitudes, ningún otro flujo tiene sentido. Entrega valor inmediato al empleado y es el núcleo de la propuesta.

**Independent Test**: Puede probarse de forma aislada iniciando sesión como empleado, creando una solicitud de cualquier tipo habilitado, y verificando que aparece en el listado de solicitudes pendientes con el estado "Enviada".

**Acceptance Scenarios**:

1. **Given** un empleado autenticado con tipos de solicitud activos disponibles, **When** completa el formulario de solicitud y lo envía, **Then** el sistema guarda la solicitud con estado "Enviada", genera un identificador único, registra la fecha/hora de creación, y notifica al supervisor asignado.

2. **Given** un empleado completa un formulario de solicitud con campos obligatorios vacíos, **When** intenta enviar la solicitud, **Then** el sistema resalta los campos faltantes, muestra mensajes de validación descriptivos, y no procesa el envío.

3. **Given** un empleado autenticado, **When** accede al historial de solicitudes, **Then** visualiza todas sus solicitudes previas con estado, fechas y comentarios del supervisor.

4. **Given** un empleado inicia el llenado de un formulario de solicitud, **When** abandona el formulario sin enviar, **Then** el sistema guarda automáticamente el progreso como "Borrador", el cual es visible solo para el empleado y puede retomarse o enviarse en cualquier momento. Si el borrador no es enviado dentro de 3 días calendario desde su creación, el sistema lo elimina automáticamente.

---

### User Story 2 - Supervisor Aprueba o Rechaza una Solicitud (Priority: P1)

Un supervisor recibe una notificación sobre una solicitud pendiente de revisión. Accede al sistema, consulta los detalles de la solicitud del empleado, puede hacer clic en "Tomar solicitud" para transicionarla a "En Revisión", puede agregar comentarios y decide aprobar o rechazar la solicitud. La decisión queda registrada con trazabilidad completa.

**Why this priority**: Junto al registro de solicitudes, la aprobación/rechazo es el flujo central del sistema. Sin esta capacidad, el ciclo de vida de las solicitudes no puede completarse.

**Independent Test**: Puede probarse iniciando sesión como supervisor, con al menos una solicitud en estado "Enviada" asignada a su área, y verificando que: (1) puede cambiar el estado a "En Revisión" mediante "Tomar solicitud", y (2) puede cambiar el estado a "Aprobada" o "Rechazada" con un comentario obligatorio en caso de rechazo.

**Acceptance Scenarios**:

1. **Given** un supervisor con solicitudes pendientes asignadas a su área, **When** selecciona "Tomar solicitud" en una solicitud en estado "Enviada", **Then** el estado cambia a "En Revisión", se registra quién tomó la solicitud y cuándo en la bitácora de auditoría, y la solicitud ya no aparece como pendiente para otros supervisores.

2. **Given** un supervisor con una solicitud en estado "En Revisión", **When** selecciona "Aprobar" y confirma, **Then** el estado cambia a "Aprobada", se registra quién aprobó y cuándo, y el empleado recibe una notificación.

3. **Given** un supervisor intenta rechazar una solicitud sin ingresar un motivo, **When** intenta confirmar el rechazo, **Then** el sistema requiere un comentario de rechazo antes de proceder y no cambia el estado.

4. **Given** un supervisor aprueba o rechaza una solicitud, **When** se completa la acción, **Then** el evento queda registrado en la bitácora de auditoría con usuario, fecha, hora y acción realizada.

---

### User Story 3 - Administrador Configura Tipos de Solicitud (Priority: P2)

Un administrador del sistema puede crear, editar, activar y desactivar los tipos de solicitud disponibles. Cada tipo puede tener campos personalizados, nombre, descripción y configuración de flujo de aprobación.

**Why this priority**: Los tipos de solicitud son la columna vertebral de la configuración del sistema. Sin ellos, los empleados no pueden registrar solicitudes, pero pueden crearse una vez al inicio y reutilizarse indefinidamente.

**Independent Test**: Puede probarse iniciando sesión como administrador, creando un nuevo tipo de solicitud con nombre, descripción y al menos un campo, activándolo, e iniciando sesión como empleado para verificar que el nuevo tipo aparece disponible en el formulario de registro.

**Acceptance Scenarios**:

1. **Given** un administrador autenticado, **When** crea un nuevo tipo de solicitud con nombre, descripción y campos requeridos y lo activa, **Then** el tipo aparece disponible para los empleados al registrar nuevas solicitudes.

2. **Given** un administrador desactiva un tipo de solicitud existente, **When** un empleado intenta registrar una nueva solicitud, **Then** el tipo desactivado no aparece en las opciones disponibles, aunque las solicitudes históricas de ese tipo permanecen accesibles.

3. **Given** existen solicitudes activas con un tipo determinado, **When** un administrador intenta eliminar ese tipo, **Then** el sistema impide la eliminación y muestra un aviso indicando que el tipo tiene solicitudes asociadas.

---

### User Story 4 - Todos los Usuarios Consultan Estado e Historial (Priority: P2)

Cualquier usuario autenticado puede consultar el estado actual y el historial completo de las solicitudes a las que tiene acceso según su rol: empleados ven sus propias solicitudes; supervisores ven las de su área; administradores ven todas.

**Why this priority**: La consulta de estado e historial es transversal y complementa los flujos P1. Permite trazabilidad a todos los actores sin ser el flujo de mayor impacto en sí mismo.

**Independent Test**: Puede probarse iniciando sesión con cada tipo de rol y verificando que la vista de historial muestra exactamente las solicitudes a las que ese rol tiene acceso, con filtros funcionales por estado y fecha.

**Acceptance Scenarios**:

1. **Given** un empleado autenticado, **When** accede a su historial de solicitudes, **Then** visualiza únicamente sus propias solicitudes con estado, fechas de creación/modificación, y comentarios del supervisor.

2. **Given** un supervisor autenticado, **When** accede al panel de solicitudes, **Then** visualiza todas las solicitudes de los empleados bajo su supervisión, pudiendo filtrar por estado, tipo y rango de fechas.

3. **Given** un administrador autenticado, **When** accede al módulo de auditoría, **Then** visualiza el historial completo de todas las solicitudes de todos los empleados, incluyendo eventos de cambio de estado con usuario responsable, fecha y hora.

---

### User Story 5 - Notificaciones Básicas (Priority: P3)

El sistema envía notificaciones a los usuarios relevantes ante eventos clave del ciclo de vida de una solicitud: nueva solicitud enviada (al supervisor), solicitud aprobada/rechazada (al empleado), y recordatorios de solicitudes pendientes de revisión (al supervisor).

**Why this priority**: Las notificaciones mejoran la experiencia y agilidad del proceso, pero el sistema es funcional sin ellas. Pueden añadirse después de que los flujos principales estén operativos.

**Independent Test**: Puede probarse enviando una solicitud y verificando que el supervisor correspondiente recibe una notificación, luego aprobando y verificando que el empleado recibe la notificación de resolución.

**Acceptance Scenarios**:

1. **Given** un empleado envía una solicitud, **When** el envío es exitoso, **Then** el supervisor asignado recibe una notificación con el nombre del empleado, tipo de solicitud y enlace directo al detalle.

2. **Given** un supervisor aprueba o rechaza una solicitud, **When** la acción se confirma, **Then** el empleado que registró la solicitud recibe una notificación indicando el resultado y, en caso de rechazo, el motivo ingresado por el supervisor.

---

### Edge Cases

- ~~¿Qué ocurre si el supervisor asignado a un empleado es dado de baja del sistema mientras tiene solicitudes pendientes de revisión?~~ *(Resuelto: ver Clarifications — el sistema bloquea la desactivación y el administrador reasigna en bloque.)*
- ¿Cómo maneja el sistema solicitudes enviadas fuera del horario laboral configurado (si aplica)?
- ¿Qué sucede si un empleado intenta modificar una solicitud que ya fue enviada y está en proceso de revisión?
- ~~¿Cómo se comporta el sistema si dos supervisores intentan actuar sobre la misma solicitud simultáneamente?~~ *(Resuelto: ver Clarifications — primero en llegar gana; el segundo recibe un mensaje informativo y la solicitud desaparece de su lista.)*
- ¿Qué ocurre si un tipo de solicitud es modificado después de que existen solicitudes activas de ese tipo?
- ¿Puede un administrador reasignar una solicitud pendiente a otro supervisor?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE permitir a usuarios autenticados registrar solicitudes seleccionando un tipo de solicitud activo y completando los campos requeridos.
- **FR-002**: El sistema DEBE asignar automáticamente un identificador único y registrar la fecha y hora de creación al guardar cada solicitud.
- **FR-003**: El sistema DEBE mantener un ciclo de vida de solicitud con estados: Borrador, Enviada, En Revisión, Aprobada, Rechazada y Cancelada.
- **FR-004**: Los supervisores DEBEN poder tomar una solicitud en estado "Enviada" mediante una acción explícita de "Tomar solicitud", lo que transiciona el estado a "En Revisión" y queda registrado en la bitácora de auditoría. Solo el supervisor que tomó la solicitud puede aprobarla o rechazarla, salvo reasignación por parte de un administrador. El campo de comentario es obligatorio al rechazar.
- **FR-005**: Los administradores DEBEN poder crear, editar, activar y desactivar tipos de solicitud con campos configurables.
- **FR-006**: El sistema DEBE impedir eliminar tipos de solicitud que tengan solicitudes asociadas (activas o históricas).
- **FR-007**: El sistema DEBE mostrar a cada usuario únicamente las solicitudes a las que tiene acceso según su rol (empleado: propias; supervisor: de su área; administrador: todas).
- **FR-008**: El sistema DEBE registrar en una bitácora de auditoría cada cambio de estado de una solicitud, incluyendo: usuario responsable, fecha, hora, estado anterior y estado nuevo.
- **FR-009**: El sistema DEBE enviar notificaciones al supervisor cuando un empleado envíe una nueva solicitud.
- **FR-010**: El sistema DEBE enviar notificaciones al empleado cuando su solicitud sea aprobada o rechazada, incluyendo el motivo en caso de rechazo.
- **FR-011**: Los empleados NO DEBEN poder modificar una solicitud después de haberla enviado; solo podrán cancelarla si aún está en estado "Enviada".
- **FR-012**: El sistema DEBE permitir filtrar solicitudes por estado, tipo, fecha de creación y usuario (según el rol del consultante).
- **FR-013**: El sistema DEBE garantizar que los datos de auditoría sean inmutables: ningún usuario puede editar o eliminar registros de la bitácora.
- **FR-014**: El sistema DEBE soportar al menos tres roles: Empleado, Supervisor y Administrador, con permisos diferenciados para cada acción.
- **FR-015**: El sistema DEBE soportar reasignación de supervisor en dos niveles independientes:
  - **Nivel organizacional (empleado)**: Un administrador puede cambiar el supervisor asignado a un empleado. Este cambio aplica únicamente a las solicitudes futuras de ese empleado; las solicitudes ya existentes retienen el supervisor que les fue asignado en el momento de su creación o última reasignación.
  - **Nivel de solicitud**: Un administrador puede reasignar una o varias solicitudes específicas (en bloque) a cualquier supervisor, sin alterar la relación organizacional del empleado. Toda reasignación queda registrada en la bitácora de auditoría.
- **FR-016**: El sistema DEBE impedir la desactivación de un supervisor que tenga solicitudes activas en estado "Enviada" o "En Revisión". El intento de desactivación DEBE mostrar la lista de solicitudes pendientes y ofrecer una interfaz de reasignación en bloque al administrador. Solo tras completar la reasignación de todas las solicitudes activas podrá procederse con la desactivación.
- **FR-017**: El sistema DEBE autoguardar el progreso del formulario de solicitud en estado "Borrador" mientras el empleado lo completa. El borrador es visible únicamente para el empleado que lo creó. Si el borrador no es enviado en 3 días calendario desde su creación, el sistema lo elimina automáticamente. Los borradores no generan notificaciones al supervisor.
- **FR-018**: La acción "Tomar solicitud" DEBE implementar control de concurrencia optimista (primero en llegar gana): si dos supervisores intentan tomar la misma solicitud simultáneamente, el sistema garantiza que solo uno tenga éxito. El supervisor que llegue segundo DEBE recibir un mensaje informativo indicando que la solicitud ya fue tomada por otro supervisor, y dicha solicitud DEBE eliminarse de su lista de pendientes de forma inmediata.

### Key Entities *(include if feature involves data)*

- **Solicitud**: Unidad central del sistema. Representa una petición formal de un empleado. Tiene identificador único, tipo, estado, campos dinámicos según el tipo, fechas de ciclo de vida, comentarios y relaciones con empleado y supervisor.
- **Tipo de Solicitud**: Categoría configurable de solicitud (ej. permiso, reembolso, material). Define nombre, descripción, campos requeridos y estado activo/inactivo.
- **Usuario**: Persona autenticada en el sistema. Pertenece a uno o más roles (Empleado, Supervisor, Administrador). Tiene área o departamento asignado. Cada empleado tiene un supervisor organizacional asignado (relación a nivel de empleado), que puede ser distinto del supervisor asignado a una solicitud específica (relación a nivel de solicitud).
- **Rol**: Define el conjunto de permisos y el alcance de visibilidad de un usuario dentro del sistema.
- **Evento de Auditoría**: Registro inmutable de cada cambio de estado en una solicitud. Contiene usuario, acción, estado anterior, estado nuevo, fecha y hora.
- **Notificación**: Mensaje generado automáticamente ante eventos del ciclo de vida de una solicitud. Tiene destinatario, contenido, estado de lectura y referencia a la solicitud.
- **Área / Departamento**: Agrupación organizacional que relaciona empleados con supervisores.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un empleado puede registrar y enviar una solicitud desde cero en menos de 3 minutos, incluyendo la selección del tipo y el llenado de campos.
- **SC-002**: El supervisor recibe la notificación de una nueva solicitud en menos de 2 minutos después de que el empleado la envíe.
- **SC-003**: El 100% de los cambios de estado de las solicitudes queda registrado en la bitácora de auditoría sin excepción, y estos registros no pueden ser alterados.
- **SC-004**: Los usuarios solo pueden visualizar y actuar sobre las solicitudes que les corresponden según su rol, con una tasa de acceso no autorizado del 0%.
- **SC-005**: El historial de solicitudes con filtros aplicados se carga y muestra en menos de 5 segundos para conjuntos de hasta 500 solicitudes.
- **SC-006**: El 90% de los supervisores puede aprobar o rechazar una solicitud sin asistencia técnica, en su primera sesión con el sistema.
- **SC-007**: Los administradores pueden crear un nuevo tipo de solicitud activo y disponible para los empleados en menos de 5 minutos.
- **SC-008**: El sistema mantiene la integridad de los datos históricos: las solicitudes y su bitácora permanecen accesibles aunque el tipo de solicitud o el usuario asociado sean desactivados.

## Assumptions

- Se asume que el sistema contará con un mecanismo de autenticación propio o integrado con el directorio corporativo existente; el diseño de autenticación en sí está fuera del alcance de esta especificación.
- Se asume que las notificaciones se entregarán como notificaciones dentro del propio sistema (in-app); la integración con correo electrónico o mensajería externa es considerada una mejora futura.
- Se asume que cada empleado tiene un único supervisor directo asignado, aunque un supervisor puede supervisar a múltiples empleados.
- Se asume que un usuario puede tener un solo rol activo a la vez (Empleado, Supervisor o Administrador); la gestión de roles múltiples o jerarquías complejas queda fuera del alcance de v1.
- Se asume que los campos de los tipos de solicitud son de tipo texto, número, fecha o selección de lista; campos avanzados (archivos adjuntos, firmas digitales) son mejoras futuras.
- Se asume que el sistema operará para una sola organización (single-tenant); la arquitectura multi-tenant es fuera del alcance de esta especificación.
- Se asume que los datos históricos de solicitudes deben conservarse indefinidamente; no se define política de eliminación en esta versión.
