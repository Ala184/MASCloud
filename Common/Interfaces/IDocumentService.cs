using Microsoft.ServiceFabric.Services.Remoting;
using Common.Models.Document;
using Document = Common.Models.Document.Document;

namespace Common.Interfaces
{
    public interface IDocumentService : IService
    {
        Task<int> AddDocument(Document doc);

        Task<int> AddVersion(int documentId, DocumentVersion version);

        Task<List<Document>> GetAllDocuments();

        Task<List<DocumentVersion>> GetVersions(int documentId);

        Task<DocumentVersion?> GetVersionByDate(int documentId, DateTime date);

        Task<List<DocumentSection>> GetSectionsForVersion(int versionId);

        Task<List<DocumentSection>> SearchSections(string keywords, DateTime? contextDate);

        Task<bool> AddSections(int versionId, List<DocumentSection> sections);

        Task<bool> AutoParseSections(int versionId);
    }
}
