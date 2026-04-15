USE PolicyInterpreterDb;
GO

-- Tabela: Documents
CREATE TABLE Documents (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Title           NVARCHAR(500)   NOT NULL,
    DocumentType    NVARCHAR(100)   NOT NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       NVARCHAR(200)   NOT NULL
);
GO

-- Tabela: DocumentVersions
CREATE TABLE DocumentVersions (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    DocumentId          INT             NOT NULL REFERENCES Documents(Id),
    VersionNumber       INT             NOT NULL,
    FullText            NVARCHAR(MAX)   NOT NULL,
    ValidFrom           DATETIME2       NOT NULL,
    ValidTo             DATETIME2       NULL,
    ChangeDescription   NVARCHAR(1000)  NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_DocVersion UNIQUE (DocumentId, VersionNumber)
);
GO

-- Tabela: DocumentSections
CREATE TABLE DocumentSections (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    DocumentVersionId   INT             NOT NULL REFERENCES DocumentVersions(Id),
    SectionIdentifier   NVARCHAR(100)   NOT NULL,
    SectionType         NVARCHAR(50)    NOT NULL,
    ParentSectionId     INT             NULL REFERENCES DocumentSections(Id),
    OrderIndex          INT             NOT NULL,
    Content             NVARCHAR(MAX)   NOT NULL
);
GO

-- Tabela: QueryLogs
CREATE TABLE QueryLogs (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    QuestionText        NVARCHAR(MAX)   NOT NULL,
    ContextDate         DATETIME2       NULL,
    ContextInfo         NVARCHAR(500)   NULL,
    ResponseText        NVARCHAR(MAX)   NOT NULL,
    ConfidenceLevel     FLOAT           NOT NULL,
    ReferencedSections  NVARCHAR(MAX)   NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    ProcessingTimeMs    INT             NOT NULL
);
GO