using System.Fabric;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Common.Interfaces;
using Common.Models;
using Common.Models.Document;

namespace LLMService
{
    internal sealed class LLMService : StatefulService, ILLMService
    {
        private HttpClient _httpClient = null!;
        private string _apiKey = string.Empty;
        private string _modelName = string.Empty;
        private readonly SemaphoreSlim _rateLimiter = new(5, 5);

        private const string CacheDictionaryName = "LLMResponseCache";

        public LLMService(StatefulServiceContext context) : base(context) { }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var configPackage = Context.CodePackageActivationContext
                .GetConfigurationPackageObject("Config");
            var settings = configPackage.Settings.Sections["GeminiSettings"];

            _apiKey = settings.Parameters["ApiKey"].Value;
            _modelName = settings.Parameters["ModelName"].Value;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
                Timeout = TimeSpan.FromMinutes(10)
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        public async Task<LLMResult> GenerateInterpretation(string systemPrompt, string userPrompt, List<DocumentSection> sections)
        {
            string cacheKey = ComputeCacheKey(userPrompt, sections);
            var cache = await StateManager.GetOrAddAsync<IReliableDictionary<string, LLMResult>>(CacheDictionaryName);

            using (var tx = StateManager.CreateTransaction())
            {
                var cached = await cache.TryGetValueAsync(tx, cacheKey);
                if (cached.HasValue)
                {
                    return cached.Value;
                }
            }

            await _rateLimiter.WaitAsync();
            try
            {
                string llmOutput = await CallGeminiApiAsync(systemPrompt, userPrompt);

                var result = ResponseParser.Parse(llmOutput);

                using (var tx = StateManager.CreateTransaction())
                {
                    await cache.TryAddAsync(tx, cacheKey, result);
                    await tx.CommitAsync();
                }

                return result;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        private async Task<string> CallGeminiApiAsync(string systemPrompt, string userPrompt)
        {
            string url = $"v1beta/models/{_modelName}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 8192
                }
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Gemini API error {response.StatusCode}: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            var text = root
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? string.Empty;
        }

        private static string ComputeCacheKey(string userPrompt, List<DocumentSection> sections)
        {
            var sb = new StringBuilder();
            sb.Append(userPrompt);
            foreach (var s in sections.OrderBy(x => x.Id))
            {
                sb.Append(s.Id);
                sb.Append(s.Content);
            }

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToBase64String(hash);
        }
    }
}
