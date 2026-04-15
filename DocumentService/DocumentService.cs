using System.Fabric;
using Microsoft.Data.SqlClient;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Common.Enums;
using Common.Helpers;
using Common.Interfaces;
using Common.Models.Document;

namespace DocumentService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class DocumentService : StatefulService, IDocumentService
    {
        private SqlHelper _sqlHelper = null!;

        public DocumentService(StatefulServiceContext context) : base(context)
        {
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var configPackage = Context.CodePackageActivationContext
                .GetConfigurationPackageObject("Config");
            var connectionString = configPackage.Settings.Sections["DatabaseSettings"]
                .Parameters["ConnectionString"].Value;

            _sqlHelper = new SqlHelper(connectionString);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        public async Task<int> AddDocument(Document doc)
        {
            string sql = @"
                INSERT INTO Documents (Title, DocumentType, CreatedAt, CreatedBy, ValidUntil)
                VALUES (@Title, @DocumentType, @CreatedAt, @CreatedBy, @ValidUntil)";

            int newId = await _sqlHelper.ExecuteScalarInsertAsync(sql,
                new SqlParameter("@Title", doc.Title),
                new SqlParameter("@DocumentType", doc.DocumentType.ToString()),
                new SqlParameter("@CreatedAt", DateTime.UtcNow),
                new SqlParameter("@CreatedBy", doc.CreatedBy),
                new SqlParameter("@ValidUntil", (object?)doc.ValidUntil ?? DBNull.Value));

            return newId;
        }

        public async Task<int> AddVersion(int documentId, DocumentVersion version)
        {
            // Close old version
            string closeSql = @"
                UPDATE DocumentVersions
                SET ValidTo = @Now
                WHERE DocumentId = @DocumentId AND ValidTo IS NULL";

            await _sqlHelper.ExecuteNonQueryAsync(closeSql,
                new SqlParameter("@Now", DateTime.UtcNow),
                new SqlParameter("@DocumentId", documentId));

            int nextVersion = version.VersionNumber;

            // Add new version
            string insertSql = @"
                INSERT INTO DocumentVersions
                    (DocumentId, VersionNumber, FullText, ValidFrom, ValidTo, ChangeDescription, CreatedAt)
                VALUES
                    (@DocumentId, @VersionNumber, @FullText, @ValidFrom, NULL, @ChangeDescription, @CreatedAt)";

            int newId = await _sqlHelper.ExecuteScalarInsertAsync(insertSql,
                new SqlParameter("@DocumentId", documentId),
                new SqlParameter("@VersionNumber", nextVersion),
                new SqlParameter("@FullText", version.FullText),
                new SqlParameter("@ValidFrom", version.ValidFrom),
                new SqlParameter("@ChangeDescription", version.ChangeDescription ?? ""),
                new SqlParameter("@CreatedAt", DateTime.UtcNow));

            return newId;
        }

        public async Task<List<Document>> GetAllDocuments()
        {
            var documents = new List<Document>();
            string sql = "SELECT Id, Title, DocumentType, CreatedAt, CreatedBy, ValidUntil FROM Documents ORDER BY CreatedAt DESC";

            var (connection, reader) = await _sqlHelper.ExecuteReaderAsync(sql);
            using (connection)
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    documents.Add(new Document
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        DocumentType = Enum.Parse<DocumentType>(reader.GetString(2)),
                        CreatedAt = reader.GetDateTime(3),
                        CreatedBy = reader.GetString(4),
                        ValidUntil = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
                    });
                }
            }
            return documents;
        }

        public async Task<List<DocumentVersion>> GetVersions(int documentId)
        {
            var versions = new List<DocumentVersion>();
            string sql = @"
                SELECT Id, DocumentId, VersionNumber, FullText, ValidFrom, ValidTo, ChangeDescription, CreatedAt
                FROM DocumentVersions
                WHERE DocumentId = @DocumentId
                ORDER BY VersionNumber DESC";

            var (connection, reader) = await _sqlHelper.ExecuteReaderAsync(sql,
                new SqlParameter("@DocumentId", documentId));
            using (connection)
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    versions.Add(MapVersionFromReader(reader));
                }
            }
            return versions;
        }

        public async Task<DocumentVersion?> GetVersionByDate(int documentId, DateTime date)
        {
            string sql = @"
                SELECT TOP 1 Id, DocumentId, VersionNumber, FullText, ValidFrom, ValidTo, ChangeDescription, CreatedAt
                FROM DocumentVersions
                WHERE DocumentId = @DocumentId
                  AND ValidFrom <= @Date
                  AND (ValidTo IS NULL OR ValidTo >= @Date)
                ORDER BY VersionNumber DESC";

            var (connection, reader) = await _sqlHelper.ExecuteReaderAsync(sql,
                new SqlParameter("@DocumentId", documentId),
                new SqlParameter("@Date", date));
            using (connection)
            using (reader)
            {
                if (await reader.ReadAsync())
                    return MapVersionFromReader(reader);
            }
            return null;
        }

        public async Task<List<DocumentSection>> GetSectionsForVersion(int versionId)
        {
            var sections = new List<DocumentSection>();
            string sql = @"
                SELECT Id, DocumentVersionId, SectionIdentifier, SectionType,
                       ParentSectionId, OrderIndex, Content
                FROM DocumentSections
                WHERE DocumentVersionId = @VersionId
                ORDER BY OrderIndex";

            var (connection, reader) = await _sqlHelper.ExecuteReaderAsync(sql,
                new SqlParameter("@VersionId", versionId));
            using (connection)
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    sections.Add(MapSectionFromReader(reader));
                }
            }
            return sections;
        }

        public async Task<List<DocumentSection>> SearchSections(string keywords, DateTime? contextDate)
        {
            var sections = new List<DocumentSection>();
            DateTime effectiveDate = contextDate ?? DateTime.UtcNow;

            var words = keywords.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var likeConditions = new List<string>();
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Date", effectiveDate)
            };

            for (int i = 0; i < words.Length; i++)
            {
                likeConditions.Add($"ds.Content LIKE @Word{i}");
                parameters.Add(new SqlParameter($"@Word{i}", $"%{words[i]}%"));
            }

            string whereKeywords = likeConditions.Count > 0
                ? "AND (" + string.Join(" OR ", likeConditions) + ")"
                : "";

            string sql = $@"
                SELECT ds.Id, ds.DocumentVersionId, ds.SectionIdentifier, ds.SectionType,
                       ds.ParentSectionId, ds.OrderIndex, ds.Content
                FROM DocumentSections ds
                INNER JOIN DocumentVersions dv ON ds.DocumentVersionId = dv.Id
                WHERE dv.ValidFrom <= @Date
                  AND (dv.ValidTo IS NULL OR dv.ValidTo >= @Date)
                  {whereKeywords}
                ORDER BY ds.OrderIndex";

            var (connection, reader) = await _sqlHelper.ExecuteReaderAsync(sql, parameters.ToArray());
            using (connection)
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    sections.Add(MapSectionFromReader(reader));
                }
            }
            return sections;
        }

        public async Task<bool> AddSections(int versionId, List<DocumentSection> sections)
        {
            foreach (var section in sections)
            {
                string sql = @"
                    INSERT INTO DocumentSections
                        (DocumentVersionId, SectionIdentifier, SectionType, ParentSectionId, OrderIndex, Content)
                    VALUES
                        (@VersionId, @SectionId, @SectionType, @ParentId, @OrderIndex, @Content)";

                await _sqlHelper.ExecuteNonQueryAsync(sql,
                    new SqlParameter("@VersionId", versionId),
                    new SqlParameter("@SectionId", section.SectionIdentifier),
                    new SqlParameter("@SectionType", section.SectionType.ToString()),
                    new SqlParameter("@ParentId", (object?)section.ParentSectionId ?? DBNull.Value),
                    new SqlParameter("@OrderIndex", section.OrderIndex),
                    new SqlParameter("@Content", section.Content));
            }
            return true;
        }

        public async Task<bool> AutoParseSections(int versionId)
        {
            string sql = "SELECT FullText FROM DocumentVersions WHERE Id = @Id";
            var (connection, reader) = await _sqlHelper.ExecuteReaderAsync(sql,
                new SqlParameter("@Id", versionId));

            string fullText;
            using (connection)
            using (reader)
            {
                if (!await reader.ReadAsync()) return false;
                fullText = reader.GetString(0);
            }

            var sections = DocumentParser.Parse(fullText, versionId);

            return await AddSections(versionId, sections);
        }

        private static DocumentVersion MapVersionFromReader(SqlDataReader reader)
        {
            return new DocumentVersion
            {
                Id = reader.GetInt32(0),
                DocumentId = reader.GetInt32(1),
                VersionNumber = reader.GetInt32(2),
                FullText = reader.GetString(3),
                ValidFrom = reader.GetDateTime(4),
                ValidTo = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                ChangeDescription = reader.IsDBNull(6) ? "" : reader.GetString(6),
                CreatedAt = reader.GetDateTime(7)
            };
        }

        private static DocumentSection MapSectionFromReader(SqlDataReader reader)
        {
            return new DocumentSection
            {
                Id = reader.GetInt32(0),
                DocumentVersionId = reader.GetInt32(1),
                SectionIdentifier = reader.GetString(2),
                SectionType = Enum.Parse<SectionType>(reader.GetString(3)),
                ParentSectionId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                OrderIndex = reader.GetInt32(5),
                Content = reader.GetString(6)
            };
        }
    }
}
