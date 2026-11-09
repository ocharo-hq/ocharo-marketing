-- The SQLite store as the retired EF migrations left it at their third level (InitialMarketing, DeliveryPipeline,
-- GenerationJobRequester): the DDL EF Core's SQLite provider ran, kept as an import-sqlite test fixture.
CREATE TABLE "Products" ("Id" TEXT NOT NULL PRIMARY KEY, "Json" TEXT NOT NULL);
CREATE TABLE "Metadata" ("Id" TEXT NOT NULL PRIMARY KEY, "Json" TEXT NOT NULL);
CREATE TABLE "Assets" ("Id" TEXT NOT NULL PRIMARY KEY, "Json" TEXT NOT NULL, "BlobPath" TEXT NOT NULL);
CREATE TABLE "Drafts" ("Id" TEXT NOT NULL PRIMARY KEY, "Revision" INTEGER NOT NULL, "ApprovedRevision" INTEGER NULL,
    "Json" TEXT NOT NULL);
CREATE TABLE "DraftRevisions" ("Id" TEXT NOT NULL PRIMARY KEY, "DraftId" TEXT NOT NULL, "Revision" INTEGER NOT NULL,
    "Json" TEXT NOT NULL);
CREATE TABLE "Schedules" ("Id" TEXT NOT NULL PRIMARY KEY, "DraftId" TEXT NOT NULL, "DraftRevision" INTEGER NOT NULL,
    "ScheduledAt" INTEGER NOT NULL, "Status" TEXT NOT NULL, "Json" TEXT NOT NULL);
CREATE TABLE "GenerationJobs" ("Id" TEXT NOT NULL PRIMARY KEY, "Kind" TEXT NOT NULL, "Status" TEXT NOT NULL,
    "PayloadJson" TEXT NOT NULL, "Json" TEXT NOT NULL, "CreatedAt" INTEGER NOT NULL, "LeaseExpiresAt" INTEGER NULL,
    "ClaimToken" TEXT NULL, "Attempts" INTEGER NOT NULL, "CheckpointJson" TEXT NULL, "RequestedBySubject" TEXT NULL,
    "RequestedInOrganization" TEXT NULL);
CREATE UNIQUE INDEX "IX_DraftRevisions_DraftId_Revision" ON "DraftRevisions" ("DraftId", "Revision");
CREATE INDEX "IX_Schedules_Status_ScheduledAt" ON "Schedules" ("Status", "ScheduledAt");
CREATE INDEX "IX_GenerationJobs_Status_CreatedAt" ON "GenerationJobs" ("Status", "CreatedAt");
CREATE TABLE "DeliveryConnections" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_DeliveryConnections" PRIMARY KEY,
    "Revision" INTEGER NOT NULL,
    "VerificationStatus" TEXT NOT NULL,
    "SafeMessage" TEXT NOT NULL,
    "ConfigurationStamp" TEXT NOT NULL,
    "Json" TEXT NOT NULL
);
CREATE TABLE "MetricObservations" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_MetricObservations" PRIMARY KEY,
    "ScheduleId" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "RecordedAt" INTEGER NOT NULL,
    "Json" TEXT NOT NULL
);
CREATE TABLE "MetricReadAttempts" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_MetricReadAttempts" PRIMARY KEY,
    "Purpose" TEXT NOT NULL,
    "ScheduleId" TEXT NOT NULL,
    "AttemptedAt" INTEGER NOT NULL,
    "Json" TEXT NOT NULL
);
CREATE TABLE "PublicationOperations" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_PublicationOperations" PRIMARY KEY,
    "ScheduleId" TEXT NOT NULL,
    "Provider" TEXT NOT NULL,
    "ConnectionId" TEXT NOT NULL,
    "ConnectionRevision" INTEGER NOT NULL,
    "ConfigurationStamp" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "DueAt" INTEGER NOT NULL,
    "ClaimToken" TEXT NULL,
    "LeaseExpiresAt" INTEGER NULL,
    "DispatchStartedAt" INTEGER NULL,
    "Attempts" INTEGER NOT NULL,
    "Json" TEXT NOT NULL
);
CREATE TABLE "SourceAnalyses" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SourceAnalyses" PRIMARY KEY,
    "Revision" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "Provider" TEXT NOT NULL,
    "Model" TEXT NULL,
    "Locale" TEXT NOT NULL,
    "Prompt" TEXT NOT NULL,
    "SourcesJson" TEXT NOT NULL,
    "RequestFingerprint" TEXT NOT NULL,
    "SuggestedText" TEXT NULL,
    "ReviewedText" TEXT NULL,
    "Message" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "ReviewedAt" TEXT NULL
);
CREATE INDEX "IX_MetricObservations_ScheduleId_RecordedAt" ON "MetricObservations" ("ScheduleId", "RecordedAt");
CREATE INDEX "IX_MetricReadAttempts_AttemptedAt" ON "MetricReadAttempts" ("AttemptedAt");
CREATE UNIQUE INDEX "IX_PublicationOperations_ScheduleId" ON "PublicationOperations" ("ScheduleId");
CREATE INDEX "IX_PublicationOperations_Status_DueAt" ON "PublicationOperations" ("Status", "DueAt");
CREATE INDEX "IX_SourceAnalyses_Status" ON "SourceAnalyses" ("Status");
CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);
CREATE TABLE "__EFMigrationsLock" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK___EFMigrationsLock" PRIMARY KEY,
    "Timestamp" TEXT NOT NULL
);
INSERT INTO "__EFMigrationsHistory" VALUES ('202609270001_InitialMarketing', '10.0.3');
INSERT INTO "__EFMigrationsHistory" VALUES ('20260927000200_DeliveryPipeline', '10.0.3');
INSERT INTO "__EFMigrationsHistory" VALUES ('20261105072649_GenerationJobRequester', '10.0.14');
