-- 001-blog — the inverse: drops every table and function 001 created, with their rows. The schema itself, its migration
-- history and its owner marker belong to the migrator and stay.

DROP TABLE IF EXISTS {{schema}}.media_edits;
DROP FUNCTION IF EXISTS {{schema}}.media_edits_immutable();
DROP TABLE IF EXISTS {{schema}}.media_blobs;
DROP TABLE IF EXISTS {{schema}}.article_revisions;
DROP TABLE IF EXISTS {{schema}}.articles;
DROP TABLE IF EXISTS {{schema}}.site_settings;
DROP TABLE IF EXISTS {{schema}}.topics;
