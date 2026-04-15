using System.Diagnostics;
using System.Fabric;
using System.Text.Json;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Runtime;
using Common.Interfaces;
using Common.Models;
using Common.Models.Document;
using Common.Models.Query;

namespace QueryService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class QueryService : StatelessService, IQueryService
    {
        private const string DocumentServiceUri = "fabric:/AIRegPolInterpreter/DocumentService";
        private const string LLMServiceUri = "fabric:/AIRegPolInterpreter/LLMService";
        private const string AuditServiceUri = "fabric:/AIRegPolInterpreter/AuditService";

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        public QueryService(StatelessServiceContext context) : base(context) { }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        public async Task<QueryResponse> AskQuestion(QueryRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var documentService = ServiceProxy.Create<IDocumentService>(
                    new Uri(DocumentServiceUri));

                var sections = await documentService.SearchSections(request.QuestionText, request.ContextDate);

                // Dohvati nazive dokumenata za prikaz izvora
                var allDocuments = await documentService.GetAllDocuments();

                if (sections == null || sections.Count == 0)
                {
                    stopwatch.Stop();
                    var emptyResponse = new QueryResponse
                    {
                        Explanation = "Na osnovu dostupnih dokumenata, nije moguće dati odgovor na postavljeno pitanje.",
                        Citations = new List<Citation>(),
                        ConfidenceLevel = 0.0,
                        Warnings = new List<string>
                        {
                            "Nije pronađen nijedan relevantni dio dokumenta za dati upit i kontekst."
                        },
                        ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
                    };

                    _ = LogQueryAsync(request, emptyResponse);
                    return emptyResponse;
                }

                string systemPrompt = BuildSystemPrompt();
                string userPrompt = BuildUserPrompt(request, sections);

                var proxyFactory = new ServiceProxyFactory(c =>
                    new FabricTransportServiceRemotingClientFactory(
                        remotingSettings: new FabricTransportRemotingSettings
                        {
                            OperationTimeout = TimeSpan.FromMinutes(15)
                        }));
                var llmService = proxyFactory.CreateServiceProxy<ILLMService>(
                    new Uri(LLMServiceUri));

                var llmResult = await llmService.GenerateInterpretation(
                    systemPrompt, userPrompt, sections);

                // Mapiraj DocumentVersionId → Document za nazive
                var versionDocMap = new Dictionary<int, string>();
                foreach (var doc in allDocuments)
                {
                    var versions = await documentService.GetVersions(doc.Id);
                    foreach (var v in versions)
                    {
                        versionDocMap[v.Id] = doc.Title;
                    }
                }

                var citations = sections
                    .Where(s => llmResult.ReferencedSectionIds.Contains(s.SectionIdentifier))
                    .Select(s => new Citation
                    {
                        SectionIdentifier = s.SectionIdentifier,
                        Content = s.Content,
                        DocumentTitle = versionDocMap.GetValueOrDefault(s.DocumentVersionId, "Nepoznat dokument"),
                        VersionNumber = 0
                    })
                    .ToList();

                // Dodaj nazive izvora u odgovor
                var sourceDocNames = citations
                    .Select(c => c.DocumentTitle)
                    .Distinct()
                    .ToList();
                string sourceText = string.Join(", ", sourceDocNames);

                stopwatch.Stop();

                var response = new QueryResponse
                {
                    Explanation = llmResult.InterpretationText + "\n\nIzvor: " + sourceText,
                    Citations = citations,
                    ConfidenceLevel = llmResult.ConfidenceLevel,
                    Warnings = new List<string>(),
                    ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
                };

                if (llmResult.InsufficientInformation)
                {
                    response.Warnings.Add("LLM je označio da nema dovoljno informacija za potpun odgovor.");
                }

                _ = LogQueryAsync(request, response);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new QueryResponse
                {
                    Explanation = "Došlo je do greške prilikom obrade upita. Pokušajte ponovo.",
                    Citations = new List<Citation>(),
                    ConfidenceLevel = 0.0,
                    Warnings = new List<string> { $"Greška: {ex.Message}" },
                    ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
        }

        private static string BuildSystemPrompt()
        {
            return @"Ti si AI pravni asistent specijalizovan za tumačenje zakona, pravilnika i internih politika.
                    PRAVILA:
                    1. Odgovaraj ISKLJUČIVO na osnovu prilošenih dijelova dokumenata.
                    2. Za svaki dio odgovora OBAVEZNO navedi referencu u formatu [SectionIdentifier].
                    3. Ako nemaš dovoljno informacija za potpun odgovor, EKSPLICITNO to naglasi.
                    4. Ne donosi pravne odluke š samo tumači tekst propisa.
                    5. Odgovor formuliši jasno i razumljivo za krajnjeg korisnika.
                    6. Na SAMOM KRAJU odgovora, u zasebnom redu, napiši:
                       POUZDANOST: X.X (broj od 0.0 do 1.0)
                    7. U zasebnom redu napiši:
                       REFERENCE: SectionId1, SectionId2, ...";
        }

        private static string BuildUserPrompt(QueryRequest request, List<DocumentSection> sections)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("DOKUMENTI:");
            sb.AppendLine("?????????????????????????????");
            foreach (var section in sections)
            {
                sb.AppendLine($"[{section.SectionIdentifier}]: {section.Content}");
                sb.AppendLine();
            }
            sb.AppendLine("?????????????????????????????");
            sb.AppendLine();
            sb.AppendLine($"PITANJE: {request.QuestionText}");

            if (request.ContextDate.HasValue)
                sb.AppendLine($"KONTEKST DATUM: {request.ContextDate.Value:yyyy-MM-dd}");

            if (!string.IsNullOrEmpty(request.ContextInfo))
                sb.AppendLine($"KONTEKST INFO: {request.ContextInfo}");

            return sb.ToString();
        }

        private async Task LogQueryAsync(QueryRequest request, QueryResponse response)
        {
            try
            {
                var auditService = ServiceProxy.Create<IAuditService>(
                    new Uri(AuditServiceUri));

                var log = new QueryLog
                {
                    QuestionText = request.QuestionText,
                    ContextDate = request.ContextDate,
                    ContextInfo = request.ContextInfo,
                    ResponseText = response.Explanation,
                    ConfidenceLevel = response.ConfidenceLevel,
                    ReferencedSections = JsonSerializer.Serialize(
                        response.Citations.Select(c => c.SectionIdentifier).ToList()),
                    CreatedAt = DateTime.UtcNow,
                    ProcessingTimeMs = response.ProcessingTimeMs
                };

                await auditService.LogQuery(log);
            }
            catch
            {
                //
            }
        }
    }
}
