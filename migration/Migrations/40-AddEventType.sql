USE [arrangement-db];

ALTER TABLE [Events]
ADD EventType NVARCHAR(255) NOT NULL
DEFAULT ('Sosialt');

