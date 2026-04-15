IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PolicyInterpreterDb')
BEGIN
    CREATE DATABASE PolicyInterpreterDb;
END
GO

USE PolicyInterpreterDb;
GO