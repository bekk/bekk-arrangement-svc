USE [arrangement-db];

ALTER TABLE [Events]
ADD IsCancelled BIT NOT NULL DEFAULT 0;