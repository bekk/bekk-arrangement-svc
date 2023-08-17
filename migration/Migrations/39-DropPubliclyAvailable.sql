USE [arrangement-db]

ALTER TABLE [Events]
DROP CONSTRAINT [DF__Events__IsPublic__59FA5E80];

ALTER TABLE [Events]
DROP COLUMN IsPubliclyAvailable;

