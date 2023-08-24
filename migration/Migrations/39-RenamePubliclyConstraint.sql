USE [arrangement-db];

ALTER TABLE [Events]
DROP CONSTRAINT DF__Events__IsPublic__1AD3FDA4;

ALTER TABLE [Events]
ADD CONSTRAINT DF__Events_IsPublic_Default_False DEFAULT 0 FOR [IsPubliclyAvailable];