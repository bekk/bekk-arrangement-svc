USE [arrangement-db];

ALTER TABLE [Events]
ALTER COLUMN [Office] nvarchar(255)

UPDATE [Events] SET Office='Oslo,Trondheim';

EXEC sp_rename 'Events.Office', 'Offices';

ALTER TABLE [Events]
ALTER COLUMN [Offices] nvarchar(255) not null;
