-- Create NotebookApp database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'NotebookApp')
BEGIN
    CREATE DATABASE NotebookApp;
END
GO

USE NotebookApp;
GO

-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 't_user')
BEGIN
    CREATE TABLE t_user (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        Name NVARCHAR(255) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_Users_Email ON t_user(Email);
END
GO

-- Create Notes table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 't_note')
BEGIN
    CREATE TABLE t_note (
        Id NVARCHAR(50) PRIMARY KEY,
        Title NVARCHAR(500) NOT NULL,
        Content NVARCHAR(MAX),
        UserId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Notes_Users FOREIGN KEY (UserId) REFERENCES t_user(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Notes_UserId ON t_note(UserId);
    CREATE INDEX IX_Notes_CreatedAt ON t_note(CreatedAt);
END
GO


select * from t_user