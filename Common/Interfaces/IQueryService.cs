using Microsoft.ServiceFabric.Services.Remoting;
using Common.Models.Query;

namespace Common.Interfaces
{
    public interface IQueryService : IService
    {
        Task<QueryResponse> AskQuestion(QueryRequest request);
    }
}
