USE PolicyInterpreterDb;
GO

-- Test dokument: Zakon o radu (pojednostavljen)
INSERT INTO Documents (Title, DocumentType, CreatedBy)
VALUES ('Zakon o radu', 'Zakon', 'Administrator');

DECLARE @DocId INT = SCOPE_IDENTITY();

INSERT INTO DocumentVersions (DocumentId, VersionNumber, FullText, ValidFrom, ChangeDescription)
VALUES (@DocId, 1,
'Član 1.
(1) Ovim zakonom uređuju se prava, obaveze i odgovornosti iz radnog odnosa.
(2) Ovaj zakon primjenjuje se na sve zaposlene i poslodavce.

Član 2.
(1) Zaposleni ima pravo na godišnji odmor u trajanju od najmanje 20 radnih dana.
(2) Zaposleni stiče pravo na godišnji odmor nakon šest mjeseci neprekidnog rada.

Član 3.
(1) Radno vrijeme zaposlenog je 40 sati sedmično.
(2) Zaposleni ima pravo na odmor u toku radnog dana od najmanje 30 minuta.
a) Odmor se koristi nakon četiri sata rada.
b) Odmor se ne može koristiti na početku ili kraju radnog vremena.',
'2024-01-01', 'Inicijalna verzija');

DECLARE @VersionId INT = SCOPE_IDENTITY();

-- Sekcije
INSERT INTO DocumentSections (DocumentVersionId, SectionIdentifier, SectionType, ParentSectionId, OrderIndex, Content)
VALUES
(@VersionId, 'Član 1', 'Clan', NULL, 1,
 'Ovim zakonom uređuju se prava, obaveze i odgovornosti iz radnog odnosa.'),
(@VersionId, 'Član 1, Stav 1', 'Stav', NULL, 2,
 'Ovim zakonom uređuju se prava, obaveze i odgovornosti iz radnog odnosa.'),
(@VersionId, 'Član 1, Stav 2', 'Stav', NULL, 3,
 'Ovaj zakon primjenjuje se na sve zaposlene i poslodavce.'),
(@VersionId, 'Član 2', 'Clan', NULL, 4,
 'Godišnji odmor zaposlenih.'),
(@VersionId, 'Član 2, Stav 1', 'Stav', NULL, 5,
 'Zaposleni ima pravo na godišnji odmor u trajanju od najmanje 20 radnih dana.'),
(@VersionId, 'Član 2, Stav 2', 'Stav', NULL, 6,
 'Zaposleni stiče pravo na godišnji odmor nakon šest mjeseci neprekidnog rada.'),
(@VersionId, 'Član 3', 'Clan', NULL, 7,
 'Radno vrijeme.'),
(@VersionId, 'Član 3, Stav 1', 'Stav', NULL, 8,
 'Radno vrijeme zaposlenog je 40 sati sedmično.'),
(@VersionId, 'Član 3, Stav 2', 'Stav', NULL, 9,
 'Zaposleni ima pravo na odmor u toku radnog dana od najmanje 30 minuta.'),
(@VersionId, 'Član 3, Stav 2, Tačka a)', 'Tacka', NULL, 10,
 'Odmor se koristi nakon četiri sata rada.'),
(@VersionId, 'Član 3, Stav 2, Tačka b)', 'Tacka', NULL, 11,
 'Odmor se ne može koristiti na početku ili kraju radnog vremena.');
GO

PRINT 'Test podaci uspješno umetnuti.';
GO