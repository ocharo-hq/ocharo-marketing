-- 002-data-protection-keys — the inverse: drops the key ring. Every session cookie it encrypted stops opening, so each
-- member signs in again once.

DROP TABLE IF EXISTS {{schema}}.data_protection_keys;
