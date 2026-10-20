-- Optional reference script. The API currently creates the schema via EF Core EnsureCreated.
-- For production, use reviewed EF Core migrations instead.
IF DB_ID(N'FactoryDocs') IS NULL
    CREATE DATABASE FactoryDocs;
GO
USE FactoryDocs;
GO
-- Table definition equivalent to the current MVP EF Core entity.
IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documents (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Documents PRIMARY KEY,
        DocumentNumber NVARCHAR(60) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Description NVARCHAR(2000) NULL,
        Status NVARCHAR(30) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL
    );
    CREATE UNIQUE INDEX UX_Documents_DocumentNumber ON dbo.Documents(DocumentNumber);
END;
GO
