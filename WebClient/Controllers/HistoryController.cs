using Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace WebClient.Controllers
{
    public class HistoryController : Controller
    {
        private const string AuditServiceUri = "fabric:/AIRegPolInterpreter/AuditService";

        // ── GET: /History?page=1 ──
        // Show query history
        public async Task<IActionResult> Index(int page = 1)
        {
            var auditService = ServiceProxy.Create<IAuditService>(
                new Uri(AuditServiceUri));

            int pageSize = 20;
            var logs = await auditService.GetQueryHistory(page, pageSize);
            int totalCount = await auditService.GetTotalQueryCount();

            ViewBag.Logs = logs;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return View();
        }
    }
}
