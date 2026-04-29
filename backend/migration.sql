CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE TABLE "Tenants" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Slug" text NOT NULL,
        "Email" text NOT NULL,
        "Phone" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE TABLE "Garages" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Address" character varying(500) NOT NULL,
        "City" character varying(100) NOT NULL,
        "Phone" text NOT NULL,
        "Latitude" double precision,
        "Longitude" double precision,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Garages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Garages_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "FirstName" character varying(100) NOT NULL,
        "LastName" character varying(100) NOT NULL,
        "Email" character varying(200) NOT NULL,
        "PasswordHash" text NOT NULL,
        "Phone" text NOT NULL,
        "Role" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "RefreshToken" text,
        "RefreshTokenExpiry" timestamp with time zone,
        "AssignedGarageId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Users_Garages_AssignedGarageId" FOREIGN KEY ("AssignedGarageId") REFERENCES "Garages" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_Users_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE TABLE "Vehicles" (
        "Id" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "Brand" text NOT NULL,
        "Model" text NOT NULL,
        "Year" integer NOT NULL,
        "LicensePlate" text NOT NULL,
        "FuelType" text NOT NULL,
        "Mileage" integer NOT NULL,
        "VIN" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_Vehicles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Vehicles_Users_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE TABLE "InterventionRequests" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "VehicleId" uuid NOT NULL,
        "GarageId" uuid NOT NULL,
        "AssignedMecanicienId" uuid,
        "Description" text NOT NULL,
        "Status" text NOT NULL,
        "UrgencyLevel" text NOT NULL,
        "AppointmentDate" timestamp with time zone,
        "DiagnosisResult" text,
        "Notes" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_InterventionRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InterventionRequests_Garages_GarageId" FOREIGN KEY ("GarageId") REFERENCES "Garages" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_InterventionRequests_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_InterventionRequests_Users_AssignedMecanicienId" FOREIGN KEY ("AssignedMecanicienId") REFERENCES "Users" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_InterventionRequests_Users_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_InterventionRequests_Vehicles_VehicleId" FOREIGN KEY ("VehicleId") REFERENCES "Vehicles" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE TABLE "AIDiagnoses" (
        "Id" uuid NOT NULL,
        "InterventionRequestId" uuid NOT NULL,
        "Diagnosis" text NOT NULL,
        "ConfidenceScore" real NOT NULL,
        "RecommendedWorkshop" text NOT NULL,
        "UrgencyLevel" text NOT NULL,
        "EstimatedCostRange" text NOT NULL,
        "RecommendedActions" text NOT NULL,
        "RagSourcesUsed" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "IsDeleted" boolean NOT NULL,
        CONSTRAINT "PK_AIDiagnoses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AIDiagnoses_InterventionRequests_InterventionRequestId" FOREIGN KEY ("InterventionRequestId") REFERENCES "InterventionRequests" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_AIDiagnoses_InterventionRequestId" ON "AIDiagnoses" ("InterventionRequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE INDEX "IX_Garages_TenantId" ON "Garages" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE INDEX "IX_InterventionRequests_AssignedMecanicienId" ON "InterventionRequests" ("AssignedMecanicienId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE INDEX "IX_InterventionRequests_ClientId" ON "InterventionRequests" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE INDEX "IX_InterventionRequests_GarageId" ON "InterventionRequests" ("GarageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE INDEX "IX_InterventionRequests_TenantId" ON "InterventionRequests" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE INDEX "IX_InterventionRequests_VehicleId" ON "InterventionRequests" ("VehicleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE INDEX "IX_Users_AssignedGarageId" ON "Users" ("AssignedGarageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE INDEX "IX_Users_TenantId" ON "Users" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    CREATE INDEX "IX_Vehicles_ClientId" ON "Vehicles" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260307032104_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260307032104_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

