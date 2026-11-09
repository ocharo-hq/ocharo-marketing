-- 001-blog — Editorial's records, moved from the SQLite store it kept before with the shape EF Core maps.
--
-- Each story, revision, topic and the site settings keep their document as `json` beside the columns Editorial finds,
-- orders and checks revisions by. Story and revision ids are the application's uuids; every other key is text the
-- application makes, compared byte by byte (COLLATE "C") as SQLite compared it. Instants are UTC `timestamptz`.
-- An image receipt keys into Identity's published organizations and accounts (set `identity`, level 1); Identity
-- never deletes either row, so RESTRICT never blocks. An earlier release's SQLite store reaches these tables once,
-- through the `import-sqlite` verb, never through this step.

CREATE TABLE IF NOT EXISTS {{schema}}.topics (
    key        TEXT COLLATE "C" NOT NULL,
    sort_order INTEGER          NOT NULL,
    json       JSONB            NOT NULL,

    CONSTRAINT pk_topics PRIMARY KEY (key)
);

-- One row, keyed by the settings id the application names.
CREATE TABLE IF NOT EXISTS {{schema}}.site_settings (
    id       TEXT COLLATE "C" NOT NULL,
    revision INTEGER          NOT NULL,
    json     JSONB            NOT NULL,

    CONSTRAINT pk_site_settings PRIMARY KEY (id)
);

-- The current story; `revision` is the optimistic concurrency token every save checks.
CREATE TABLE IF NOT EXISTS {{schema}}.articles (
    id           UUID             NOT NULL,
    slug         TEXT COLLATE "C" NOT NULL,
    topic        TEXT COLLATE "C" NOT NULL,
    status       TEXT COLLATE "C" NOT NULL,
    publish_at   TIMESTAMPTZ,
    published_at TIMESTAMPTZ,
    revision     INTEGER          NOT NULL,
    created_at   TIMESTAMPTZ      NOT NULL,
    updated_at   TIMESTAMPTZ      NOT NULL,
    json         JSONB            NOT NULL,

    CONSTRAINT pk_articles PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_articles_slug ON {{schema}}.articles (slug);
CREATE INDEX IF NOT EXISTS ix_articles_status_publish_at ON {{schema}}.articles (status, publish_at);

-- An immutable snapshot of each saved revision. As in the SQLite store, it carries no foreign key to its story.
CREATE TABLE IF NOT EXISTS {{schema}}.article_revisions (
    id         UUID        NOT NULL,
    article_id UUID        NOT NULL,
    revision   INTEGER     NOT NULL,
    saved_at   TIMESTAMPTZ NOT NULL,
    summary    TEXT        NOT NULL,
    json       JSONB       NOT NULL,

    CONSTRAINT pk_article_revisions PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_article_revisions_article_id_revision
    ON {{schema}}.article_revisions (article_id, revision);

-- The content-addressed media index. `file_id` names the WoW2 Files file holding the bytes, unset until the boot
-- backfill fills it; `blob_path` the local blob of a file stored before Files, empty for one stored in Files.
CREATE TABLE IF NOT EXISTS {{schema}}.media_blobs (
    sha256       TEXT COLLATE "C" NOT NULL,
    file_name    TEXT             NOT NULL,
    content_type TEXT             NOT NULL,
    byte_length  BIGINT           NOT NULL,
    blob_path    TEXT             NOT NULL,
    preview_path TEXT,
    width        INTEGER,
    height       INTEGER,
    stored_at    TIMESTAMPTZ      NOT NULL,
    file_id      TEXT,

    CONSTRAINT pk_media_blobs PRIMARY KEY (sha256)
);

-- Immutable image-edit receipts, apart from the deduplicated bytes they name; a trigger refuses every update and
-- delete. `organization_id` and `actor` are the Identity organization and person the editor's token named.
CREATE TABLE IF NOT EXISTS {{schema}}.media_edits (
    id              TEXT COLLATE "C" NOT NULL,
    source_sha256   TEXT COLLATE "C" NOT NULL,
    sha256          TEXT COLLATE "C" NOT NULL,
    recipe_json     JSONB            NOT NULL,
    organization_id UUID             NOT NULL,
    actor           UUID             NOT NULL,
    model           TEXT,
    created_at      TIMESTAMPTZ      NOT NULL,

    CONSTRAINT pk_media_edits PRIMARY KEY (id),
    CONSTRAINT fk_media_edits_source_sha256 FOREIGN KEY (source_sha256) REFERENCES {{schema}}.media_blobs (sha256),
    CONSTRAINT fk_media_edits_sha256 FOREIGN KEY (sha256) REFERENCES {{schema}}.media_blobs (sha256),
    CONSTRAINT fk_media_edits_organization_id FOREIGN KEY (organization_id)
        REFERENCES {{schema:identity}}.identity_organizations (id) ON DELETE RESTRICT,
    CONSTRAINT fk_media_edits_actor FOREIGN KEY (actor)
        REFERENCES {{schema:identity}}.identity_users (id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_media_edits_sha256_created_at ON {{schema}}.media_edits (sha256, created_at DESC);

CREATE OR REPLACE FUNCTION {{schema}}.media_edits_immutable() RETURNS trigger
    LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'Image receipts are immutable';
END;
$$;

CREATE OR REPLACE TRIGGER media_edits_immutable
    BEFORE UPDATE OR DELETE ON {{schema}}.media_edits
    FOR EACH ROW EXECUTE FUNCTION {{schema}}.media_edits_immutable();
