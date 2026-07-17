# Checklist: Core State Machine & Concurrency Requirements

**Purpose**: Validate the quality, clarity, and completeness of requirements related to the request lifecycle and concurrency.
**Target Audience**: Peer Reviewer
**Created**: 2026-07-16

## Requirement Completeness
- [ ] CHK001 - Are the allowed state transitions explicitly defined for all 6 states (Borrador, Enviada, En Revisión, Aprobada, Rechazada, Cancelada)? [Completeness, Spec §FR-003]
- [ ] CHK002 - Are the required fields and validation rules completely defined before a state can transition from Borrador to Enviada? [Completeness, Spec §US1]

## Requirement Clarity
- [ ] CHK003 - Is "primero en llegar gana" defined with specific technical boundaries (e.g., database optimistic concurrency version matching)? [Clarity, Spec §FR-018]
- [ ] CHK004 - Is the 3-day expiration rule for drafts clear about timezones and exactly when the timer starts? [Clarity, Spec §FR-017]
- [ ] CHK005 - Is the requirement for an "explicit button click" for 'Tomar solicitud' sufficiently clear to prevent automated assignments? [Clarity, Spec §FR-004]

## Requirement Consistency
- [ ] CHK006 - Does the employee cancellation rule (only in "Enviada" state) conflict with any automated background tasks? [Consistency, Spec §FR-011]
- [ ] CHK007 - Do the admin mass-reassignment rules align consistently with the "En Revisión" locking mechanism for supervisors? [Consistency, Spec §FR-015]

## Scenario Coverage & Edge Cases
- [ ] CHK008 - Are requirements defined for the race condition where an employee attempts to cancel a request exactly when a supervisor clicks 'Tomar solicitud'? [Edge Case, Gap]
- [ ] CHK009 - Does the spec define system behavior if the 3-day draft expiration triggers while the user is actively editing the form? [Edge Case, Gap]
- [ ] CHK010 - Are recovery requirements specified if the audit log insertion fails during a critical state transition? [Coverage, Exception Flow]
- [ ] CHK011 - Is the behavior defined for when a supervisor attempts to take a request but is deactivated by an Admin at the exact same moment? [Edge Case, Gap]

## Acceptance Criteria Quality
- [ ] CHK012 - Can the optimistic concurrency behavior be objectively verified under load in an automated test environment? [Measurability, Spec §US2]
- [ ] CHK013 - Are the audit log immutability constraints testable via the application layer? [Measurability, Spec §FR-013]
