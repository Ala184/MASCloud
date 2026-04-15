using Common.Models.Query;
using Microsoft.ServiceFabric.Services.Remoting;

namespace Common.Interfaces
{
    public interface IAuditService : IService
    {
        Task LogQuery(QueryLog log);

        Task<List<QueryLog>> GetQueryHistory(int page, int pageSize);

        Task<int> GetTotalQueryCount();
    }
}
