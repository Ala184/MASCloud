using Common.Enums;
using Common.Interfaces;
using Common.Models.Document;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace WebClient.Controllers
{
    public class DocumentController : Controller
    {
        private const string DocumentServiceUri = "fabric:/AIRegPolInterpreter/DocumentService";

        private IDocumentService GetDocumentService()
        {
            return ServiceProxy.Create<IDocumentService>(
                new Uri(DocumentServiceUri));
        }

        // ── GET: /Document ──
        // Lista of all documents
        public async Task<IActionResult> Index()
        {
            var service = GetDocumentService();
            var documents = await service.GetAllDocuments();
            ViewBag.Documents = documents;
            return View();
        }

        // ── GET: /Document/Upload ──
        // New document form
        public IActionResult Upload()
        {
            return View();
        }

        // ── POST: /Document/Upload ──
        // Create new document (Inital verzion and auto-parse of sections)
        [HttpPost]
        public async Task<IActionResult> Upload(string title, string documentType,
            string createdBy, string fullText, DateTime validFrom, DateTime? validUntil)
        {
            var service = GetDocumentService();

            var doc = new Document
            {
                Title = title,
                DocumentType = Enum.Parse<DocumentType>(documentType),
                CreatedBy = createdBy,
                ValidUntil = validUntil
            };
            int docId = await service.AddDocument(doc);

            var version = new DocumentVersion
            {
                DocumentId = docId,
                VersionNumber = 1,
                FullText = fullText,
                ValidFrom = validFrom,
                ChangeDescription = "Inicijalna verzija"
            };
            int versionId = await service.AddVersion(docId, version);

            await service.AutoParseSections(versionId);

            return RedirectToAction("Details", new { id = docId });
        }

        // ── GET: /Document/Details/id ──
        // Show specific version of document (Ptional ?versionId=X for older versions)
        public async Task<IActionResult> Details(int id, int? versionId)
        {
            var service = GetDocumentService();
            var versions = await service.GetVersions(id);
            ViewBag.DocumentId = id;
            ViewBag.Versions = versions;

            if (versions.Count > 0)
            {
                // If versionId exists show that specific version, else show newest
                var selectedVersion = versionId.HasValue
                    ? versions.FirstOrDefault(v => v.Id == versionId.Value) ?? versions.First()
                    : versions.First();

                var sections = await service.GetSectionsForVersion(selectedVersion.Id);
                ViewBag.Sections = sections;
                ViewBag.CurrentVersion = selectedVersion;
            }

            return View();
        }

        // ── GET: /Document/AddVersion/id ──
        // Show add new version form
        public IActionResult AddVersion(int id)
        {
            ViewBag.DocumentId = id;
            return View();
        }

        // ── POST: /Document/AddVersion ──
        [HttpPost]
        public async Task<IActionResult> AddVersion(int documentId, string fullText,
            DateTime validFrom, string changeDescription)
        {
            var service = GetDocumentService();

            // Odredi sljedeći VersionNumber
            var existingVersions = await service.GetVersions(documentId);
            int nextNumber = existingVersions.Count > 0
                ? existingVersions.Max(v => v.VersionNumber) + 1
                : 1;

            var version = new DocumentVersion
            {
                DocumentId = documentId,
                VersionNumber = nextNumber,
                FullText = fullText,
                ValidFrom = validFrom,
                ChangeDescription = changeDescription
            };

            int versionId = await service.AddVersion(documentId, version);
            await service.AutoParseSections(versionId);

            return RedirectToAction("Details", new { id = documentId });
        }
    }
}
