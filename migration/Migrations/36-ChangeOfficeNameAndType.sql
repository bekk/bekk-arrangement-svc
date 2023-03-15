USE [arrangement-db];
EXEC sp_rename 'Events.Office', 'Offices';
ALTER TABLE [Events]
ALTER COLUMN [Offices] nvarchar(255) not null