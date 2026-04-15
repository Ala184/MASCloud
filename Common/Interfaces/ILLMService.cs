using Microsoft.ServiceFabric.Services.Remoting;
using Common.Models;
using Common.Models.Document;

namespace Common.Interfaces
{
    public interface ILLMService : IService
    {
        Task<LLMResult> GenerateInterpretation(
            string systemPrompt,
            string userPrompt,
            List<DocumentSection> sections);
    }
}
