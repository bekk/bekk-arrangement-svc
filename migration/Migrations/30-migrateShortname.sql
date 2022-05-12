USE [arrangement-db];

CREATE UNIQUE NONCLUSTERED INDEX UQ_Event_Shortname
    ON Events(Shortname)
    WHERE Shortname IS NOT NULL;

UPDATE
    [Events]
SET
    [Events].Shortname = OldShortname.Shortname
    FROM
    [Events] E
        INNER JOIN
    [Shortnames] OldShortname
ON
    OldShortname.EventId = E.Id;

DROP TABLE [Shortnames];