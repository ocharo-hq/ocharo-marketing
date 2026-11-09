-- 001-marketing — Marketing's records, moved from the SQLite store it kept before with the shape EF Core maps.
--
-- Each table keeps one aggregate as a JSON document (`json`) beside the columns the desk filters, orders and claims by.
-- Keys are text the application makes, compared byte by byte (COLLATE "C") as SQLite compared them. Instants are UTC
-- `timestamptz`; EF maps the `long` Unix milliseconds of the entities onto them. The EF model declares no relation
-- between these tables, so none keys into another; only a job's requester keys into Identity's published accounts and
-- organizations, which the `identity` set (level 1) applies first. An earlier release's SQLite store reaches them once,
-- through the `import-sqlite` verb, never through this step.

CREATE TABLE IF NOT EXISTS {{schema}}.products (
    id   TEXT COLLATE "C" NOT NULL,
    json JSONB            NOT NULL,

    CONSTRAINT pk_products PRIMARY KEY (id)
);

-- The brand, settings, inquiries with their activity, and the operations documents, each under its own id prefix.
CREATE TABLE IF NOT EXISTS {{schema}}.metadata (
    id   TEXT COLLATE "C" NOT NULL,
    json JSONB            NOT NULL,

    CONSTRAINT pk_metadata PRIMARY KEY (id)
);

-- `blob_path` names the local blob of an asset stored before WoW2 Files (empty for one stored in Files); `file_id` the
-- Files file holding its bytes, unset until the boot backfill fills it.
CREATE TABLE IF NOT EXISTS {{schema}}.assets (
    id        TEXT COLLATE "C" NOT NULL,
    json      JSONB            NOT NULL,
    blob_path TEXT             NOT NULL,
    file_id   TEXT,

    CONSTRAINT pk_assets PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS {{schema}}.drafts (
    id                TEXT COLLATE "C" NOT NULL,
    revision          INTEGER          NOT NULL,
    approved_revision INTEGER,
    json              JSONB            NOT NULL,

    CONSTRAINT pk_drafts PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS {{schema}}.draft_revisions (
    id       TEXT COLLATE "C" NOT NULL,
    draft_id TEXT COLLATE "C" NOT NULL,
    revision INTEGER          NOT NULL,
    json     JSONB            NOT NULL,

    CONSTRAINT pk_draft_revisions PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_draft_revisions_draft_id_revision
    ON {{schema}}.draft_revisions (draft_id, revision);

-- The WoW2 Files file holding an approved revision's export ZIP, and the SHA-256 a read checks it against.
CREATE TABLE IF NOT EXISTS {{schema}}.export_files (
    draft_id TEXT COLLATE "C" NOT NULL,
    revision INTEGER          NOT NULL,
    sha256   TEXT             NOT NULL,
    file_id  TEXT             NOT NULL,

    CONSTRAINT pk_export_files PRIMARY KEY (draft_id, revision)
);

CREATE TABLE IF NOT EXISTS {{schema}}.schedules (
    id             TEXT COLLATE "C" NOT NULL,
    draft_id       TEXT COLLATE "C" NOT NULL,
    draft_revision INTEGER          NOT NULL,
    scheduled_at   TIMESTAMPTZ      NOT NULL,
    status         TEXT             NOT NULL,
    json           JSONB            NOT NULL,

    CONSTRAINT pk_schedules PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_schedules_status_scheduled_at ON {{schema}}.schedules (status, scheduled_at);

-- `requested_by_subject` and `requested_in_organization` are the Identity account (`sub`) and organization of the
-- person who asked; null for a job from before they were recorded, or one no person asked for. Identity never deletes
-- either row, so RESTRICT never blocks.
CREATE TABLE IF NOT EXISTS {{schema}}.generation_jobs (
    id                        TEXT COLLATE "C" NOT NULL,
    kind                      TEXT             NOT NULL,
    status                    TEXT             NOT NULL,
    payload_json              JSONB            NOT NULL,
    json                      JSONB            NOT NULL,
    created_at                TIMESTAMPTZ      NOT NULL,
    lease_expires_at          TIMESTAMPTZ,
    claim_token               TEXT,
    attempts                  INTEGER          NOT NULL,
    checkpoint_json           JSONB,
    requested_by_subject      UUID,
    requested_in_organization UUID,

    CONSTRAINT pk_generation_jobs PRIMARY KEY (id),
    CONSTRAINT fk_generation_jobs_requested_by_subject FOREIGN KEY (requested_by_subject)
        REFERENCES {{schema:identity}}.identity_users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_generation_jobs_requested_in_organization FOREIGN KEY (requested_in_organization)
        REFERENCES {{schema:identity}}.identity_organizations (id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_generation_jobs_status_created_at ON {{schema}}.generation_jobs (status, created_at);
CREATE INDEX IF NOT EXISTS ix_generation_jobs_requested_by_subject
    ON {{schema}}.generation_jobs (requested_by_subject);
CREATE INDEX IF NOT EXISTS ix_generation_jobs_requested_in_organization
    ON {{schema}}.generation_jobs (requested_in_organization);

CREATE TABLE IF NOT EXISTS {{schema}}.delivery_connections (
    id                  TEXT COLLATE "C" NOT NULL,
    revision            INTEGER          NOT NULL,
    verification_status TEXT             NOT NULL,
    safe_message        TEXT             NOT NULL,
    configuration_stamp TEXT             NOT NULL,
    json                JSONB            NOT NULL,

    CONSTRAINT pk_delivery_connections PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS {{schema}}.publication_operations (
    id                  TEXT COLLATE "C" NOT NULL,
    schedule_id         TEXT COLLATE "C" NOT NULL,
    provider            TEXT             NOT NULL,
    connection_id       TEXT COLLATE "C" NOT NULL,
    connection_revision INTEGER          NOT NULL,
    configuration_stamp TEXT             NOT NULL,
    status              TEXT             NOT NULL,
    due_at              TIMESTAMPTZ      NOT NULL,
    claim_token         TEXT,
    lease_expires_at    TIMESTAMPTZ,
    dispatch_started_at TIMESTAMPTZ,
    attempts            INTEGER          NOT NULL,
    json                JSONB            NOT NULL,

    CONSTRAINT pk_publication_operations PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_publication_operations_schedule_id
    ON {{schema}}.publication_operations (schedule_id);
CREATE INDEX IF NOT EXISTS ix_publication_operations_status_due_at
    ON {{schema}}.publication_operations (status, due_at);

CREATE TABLE IF NOT EXISTS {{schema}}.metric_observations (
    id          TEXT COLLATE "C" NOT NULL,
    schedule_id TEXT COLLATE "C" NOT NULL,
    name        TEXT             NOT NULL,
    recorded_at TIMESTAMPTZ      NOT NULL,
    json        JSONB            NOT NULL,

    CONSTRAINT pk_metric_observations PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_metric_observations_schedule_id_recorded_at
    ON {{schema}}.metric_observations (schedule_id, recorded_at);

CREATE TABLE IF NOT EXISTS {{schema}}.metric_read_attempts (
    id           TEXT COLLATE "C" NOT NULL,
    purpose      TEXT             NOT NULL,
    schedule_id  TEXT COLLATE "C" NOT NULL,
    attempted_at TIMESTAMPTZ      NOT NULL,
    json         JSONB            NOT NULL,

    CONSTRAINT pk_metric_read_attempts PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_metric_read_attempts_attempted_at ON {{schema}}.metric_read_attempts (attempted_at);

CREATE TABLE IF NOT EXISTS {{schema}}.source_analyses (
    id                  TEXT COLLATE "C" NOT NULL,
    revision            INTEGER          NOT NULL,
    status              TEXT             NOT NULL,
    provider            TEXT             NOT NULL,
    model               TEXT,
    locale              TEXT             NOT NULL,
    prompt              TEXT             NOT NULL,
    sources_json        JSONB            NOT NULL,
    request_fingerprint TEXT             NOT NULL,
    suggested_text      TEXT,
    reviewed_text       TEXT,
    message             TEXT             NOT NULL,
    created_at          TIMESTAMPTZ      NOT NULL,
    updated_at          TIMESTAMPTZ      NOT NULL,
    reviewed_at         TIMESTAMPTZ,

    CONSTRAINT pk_source_analyses PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_source_analyses_status ON {{schema}}.source_analyses (status);
