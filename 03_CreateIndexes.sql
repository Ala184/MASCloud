USE PolicyInterpreterDb;
GO

CREATE INDEX IX_DocVersions_Validity
    ON DocumentVersions (DocumentId, ValidFrom, ValidTo);

CREATE INDEX IX_Sections_VersionType
    ON DocumentSections (DocumentVersionId, SectionType);

CREATE INDEX IX_QueryLogs_Date
    ON QueryLogs (CreatedAt DESC);
GO