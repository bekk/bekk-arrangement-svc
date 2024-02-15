USE [arrangement-db];

ALTER TABLE ParticipantQuestions
ADD Required BIT NOT NULL DEFAULT 0;
