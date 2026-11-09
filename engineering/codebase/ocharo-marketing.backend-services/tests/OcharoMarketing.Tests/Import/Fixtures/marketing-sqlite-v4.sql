-- The retired EF migration AssetFiles, the store's last level before PostgreSQL: run after marketing-sqlite-v3.sql.
ALTER TABLE "Assets" ADD "FileId" TEXT NULL;
CREATE TABLE "ExportFiles" (
    "DraftId" TEXT NOT NULL,
    "Revision" INTEGER NOT NULL,
    "Sha256" TEXT NOT NULL,
    "FileId" TEXT NOT NULL,
    CONSTRAINT "PK_ExportFiles" PRIMARY KEY ("DraftId", "Revision")
);
INSERT INTO "__EFMigrationsHistory" VALUES ('20261109080003_AssetFiles', '10.0.14');
