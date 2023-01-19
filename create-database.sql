IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'arrangement-db')
BEGIN
  CREATE DATABASE [arrangement-db];
END;
GO
