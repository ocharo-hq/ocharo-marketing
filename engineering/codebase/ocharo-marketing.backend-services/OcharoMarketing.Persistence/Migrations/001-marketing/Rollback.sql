-- 001-marketing — the inverse: drops every table 001 created, with its rows. The schema itself, its migration history
-- and its owner marker belong to the migrator and stay.

DROP TABLE IF EXISTS {{schema}}.source_analyses;
DROP TABLE IF EXISTS {{schema}}.metric_read_attempts;
DROP TABLE IF EXISTS {{schema}}.metric_observations;
DROP TABLE IF EXISTS {{schema}}.publication_operations;
DROP TABLE IF EXISTS {{schema}}.delivery_connections;
DROP TABLE IF EXISTS {{schema}}.generation_jobs;
DROP TABLE IF EXISTS {{schema}}.schedules;
DROP TABLE IF EXISTS {{schema}}.export_files;
DROP TABLE IF EXISTS {{schema}}.draft_revisions;
DROP TABLE IF EXISTS {{schema}}.drafts;
DROP TABLE IF EXISTS {{schema}}.assets;
DROP TABLE IF EXISTS {{schema}}.metadata;
DROP TABLE IF EXISTS {{schema}}.products;
