USE [arrangement-db];

UPDATE [Events]
SET IsPubliclyAvailable = 1
WHERE EventType='Faglig';

UPDATE [Events]
SET IsPubliclyAvailable = 0
WHERE EventType='Sosialt';