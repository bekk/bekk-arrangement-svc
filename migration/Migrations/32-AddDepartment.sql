USE [arrangement-db];

ALTER TABLE [Participants]
    ADD
        Department NVARCHAR(64) NULL DEFAULT NULL;
