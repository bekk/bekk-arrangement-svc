USE [arrangement-db];

ALTER TABLE [Events]
ALTER COLUMN [Office] nvarchar(255)

UPDATE [Events] SET Office=null;
EXEC sp_rename 'Events.Office', 'Offices';