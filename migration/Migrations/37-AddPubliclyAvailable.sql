USE [arrangement-db];

ALTER TABLE [Events]
ADD IsPubliclyAvailable BIT NOT NULL
DEFAULT 0;
