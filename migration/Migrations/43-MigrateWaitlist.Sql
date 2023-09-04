USE [arrangement-db];

UPDATE P
SET IsWaitlisted = 1
    FROM Participants AS P
INNER JOIN (
    SELECT E.Id AS EventId, PN.Email
    FROM Events AS E
    LEFT JOIN (
        SELECT EventId, Email, ROW_NUMBER() OVER (PARTITION BY EventId ORDER BY RegistrationTime) AS RegistrationSpot
        FROM Participants
    ) AS PN ON E.Id = PN.EventId
    WHERE PN.RegistrationSpot > E.MaxParticipants
) AS WaitlistedParticipants ON P.EventId = WaitlistedParticipants.EventId AND P.Email = WaitlistedParticipants.Email;