# MecaManage Frontend Interface Progress

Last update: 2026-04-11

## Backend Features Inventory (API)

### Auth (`/api/auth`)
- `POST /register` - Create user account (`firstName`, `lastName`, `email`, `password`, `phone`, `role`, `tenantId`)
- `POST /login` - Authenticate and return JWT + refresh token
- `POST /refresh` - Rotate access token from refresh token

### Users (`/api/users`) [Authorize]
- `GET /` - List users for current tenant

### Tenants (`/api/tenants`) [Authorize, SuperAdmin]
- `GET /` - List all tenants
- `POST /` - Create tenant (`name`, `slug`, `email`, `phone`)

### Garages (`/api/garages`) [Authorize]
- `GET /` - List tenant garages
- `POST /` - Create garage (`tenantId`, `name`, `address`, `city`, `phone`, `latitude`, `longitude`) [SuperAdmin, AdminEntreprise]

### Vehicles (`/api/vehicles`) [Authorize]
- `GET /` - List vehicles for connected client
- `POST /` - Create vehicle (`clientId`, `brand`, `model`, `year`, `licensePlate`, `fuelType`, `mileage`, `vin`)

### Interventions (`/api/interventions`) [Authorize]
- `GET /` - List interventions for current tenant
- `POST /` - Create intervention (`vehicleId`, `garageId`, `description`, `urgencyLevel`, `appointmentDate`)
- `PUT /{id}/status` - Update status (`newStatus`, `notes`) [SuperAdmin, AdminEntreprise, ChefAtelier]
- `PUT /{id}/assign` - Assign mechanic (`mecanicienId`) [SuperAdmin, AdminEntreprise, ChefAtelier]
- `POST /{id}/diagnose` - Request AI diagnosis [SuperAdmin, AdminEntreprise, ChefAtelier]

---

## Frontend Contracts and Services

### Models (`frontend/src/app/core/models`)
- [x] `auth.models.ts`
- [x] `user.models.ts`
- [x] `tenant.models.ts`
- [x] `garage.models.ts`
- [x] `vehicle.models.ts`
- [x] `intervention.models.ts`

### API Services (`frontend/src/app/core/services`)
- [x] `users.service.ts`
- [x] `tenants.service.ts`
- [x] `garages.service.ts`
- [x] `vehicles.service.ts`
- [x] `interventions.service.ts`
- [x] `auth.service.ts` (already in `frontend/src/app/auth`)

### Auth Linking
- [x] Login page connected to backend (`AuthService.login`)
- [x] Signup page connected to backend (`AuthService.register`)
- [x] Token persistence helper (`setSession`, `getAccessToken`, `clearSession`)
- [x] HTTP JWT interceptor registered in app config
- [ ] Refresh token auto-flow on `401`
- [ ] Route guards by role and auth state

---

## Interface/UI Progress (Tailwind style consistency)

- [x] Login page (Tailwind, dark mode)
- [x] Signup page (Tailwind, dark mode)
- [ ] Users list page wired to `UsersService`
- [ ] Tenants admin page wired to `TenantsService`
- [ ] Garages page wired to `GaragesService`
- [ ] Vehicles page wired to `VehiclesService`
- [ ] Interventions page wired to `InterventionsService`

---

## Next Suggested Sprint Slice

1. Build role-aware route guards (`Auth`, `Role`) and secure dashboard routes.
2. Wire one page per domain (list + create) using existing Tailwind card/table style.
3. Add global API error handler/toast for unauthorized and validation errors.
4. Add refresh-token interceptor strategy to keep sessions alive.

