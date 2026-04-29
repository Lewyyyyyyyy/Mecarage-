# MecaManage Operational Workflow (Admin-First)

Last update: 2026-04-11
Scope: current backend/API implementation + immediate frontend wiring priorities.

## 1) Roles and Responsibilities

- `SuperAdmin`: platform-level governance (multi-tenant setup, global supervision).
- `AdminEntreprise`: manages one tenant/company (garages, team setup, operations oversight).
- `ChefAtelier`: workshop execution control (assign jobs, update statuses, trigger AI diagnosis).
- `Mecanicien`: executes interventions and reports progress.
- `Client`: manages personal vehicle requests and follows repair lifecycle.

## 2) End-to-End Workflow (Starting with Admins)

### Phase A - Platform Bootstrap (`SuperAdmin`)

1. Create company workspace (tenant).
   - Endpoint: `POST /api/tenants`
   - Output: `tenantId`
2. Create first company admin account for that tenant.
   - Endpoint: `POST /api/auth/register`
   - Payload role: `AdminEntreprise`
3. Share onboarding info to tenant admin (credentials/process).

### Phase B - Tenant Setup (`AdminEntreprise`)

1. Login and initialize org session.
   - Endpoint: `POST /api/auth/login`
2. Create one or more physical garages for the tenant.
   - Endpoint: `POST /api/garages`
3. Create internal users (chef atelier, mecaniciens, optionally clients if needed by process).
   - Endpoint: `POST /api/auth/register`
   - Roles used: `ChefAtelier`, `Mecanicien`, `Client`
4. Validate tenant resources.
   - Endpoints: `GET /api/users`, `GET /api/garages`

### Phase C - Daily Workshop Operations (`ChefAtelier` + `AdminEntreprise`)

1. Monitor all tenant interventions.
   - Endpoint: `GET /api/interventions`
2. Assign interventions to mechanics.
   - Endpoint: `PUT /api/interventions/{id}/assign`
3. Update intervention state according to workshop progress.
   - Endpoint: `PUT /api/interventions/{id}/status`
4. Trigger AI diagnosis when needed for technical support.
   - Endpoint: `POST /api/interventions/{id}/diagnose`

### Phase D - Mechanic Execution (`Mecanicien`)

1. Login and open assigned workload view.
2. Execute repairs according to assignment and status process.
3. Coordinate with workshop lead for status updates and re-assignment as needed.

Note: with current API surface, assignment/status actions are restricted to `SuperAdmin`, `AdminEntreprise`, `ChefAtelier`.

### Phase E - Client Journey (`Client`)

1. Register/login to personal account.
   - Endpoints: `POST /api/auth/register`, `POST /api/auth/login`
2. Register vehicle profile.
   - Endpoint: `POST /api/vehicles`
3. Create intervention request (problem description + urgency + appointment).
   - Endpoint: `POST /api/interventions`
4. Track intervention lifecycle.
   - Endpoint: `GET /api/interventions`

## 3) Typical Business Lifecycle (Single Repair)

1. Client creates vehicle (`/vehicles`) and intervention request (`/interventions`).
2. Workshop lead reviews queue (`GET /interventions`).
3. Lead assigns mechanic (`PUT /interventions/{id}/assign`).
4. Lead updates status over time (`PUT /interventions/{id}/status`).
5. Lead may trigger AI diagnostic support (`POST /interventions/{id}/diagnose`).
6. Intervention closes when status reaches terminal state (`Termine`, `Refuse`, or `Annule`).

## 4) Current Frontend-to-Backend Mapping

Already connected in frontend:
- Auth pages (`login`, `signup`) -> `/api/auth/*`
- Typed service layer for domains:
  - `users.service.ts`
  - `tenants.service.ts`
  - `garages.service.ts`
  - `vehicles.service.ts`
  - `interventions.service.ts`
- JWT attach interceptor for protected endpoints.

In progress:
- Route guards by auth/role.
- Refresh token auto-flow on `401`.
- Domain pages wired to list/create actions.

## 5) Integration Lanes (Planned/Extended)

- Notifications (email/SMS): trigger on assignment, status changes, and reminders.
- AI module: diagnose endpoint already available for privileged roles.
- n8n workflows: orchestrate reminders, approval chains, and reporting automations.

## 6) Governance Notes

- Tenant isolation is the core security boundary: each company sees only its own data.
- Role-based authorization is enforced at API level for critical operations.
- All sensitive operations should remain auditable through centralized logs.

