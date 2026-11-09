-- 002-data-protection-keys — the ASP.NET Core Data Protection key ring that encrypts the sign-in session cookie.
--
-- The host's key repository appends one row per key or revocation element and reads every row back; it never updates or
-- deletes one, so the table is an append-only log keyed by an identity. Every replica and every restart reads the same
-- ring, so a deploy keeps members signed in. `import-sqlite` leaves this table alone: the SQLite era kept its keys in
-- files.

CREATE TABLE IF NOT EXISTS {{schema}}.data_protection_keys (
    id            BIGINT GENERATED ALWAYS AS IDENTITY,
    friendly_name TEXT        NOT NULL,
    xml           TEXT        NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_data_protection_keys PRIMARY KEY (id)
);
