using Common.Interfaces;
using Common.Models.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;

namespace WebClient.Controllers
{
    public class QueryController : Controller
    {
        private const string QueryServiceUri = "fabric:/AIRegPolInterpreter/QueryService";

        // ── GET: /Query/Ask ──
        // New query form
        public IActionResult Ask()
        {
            return View();
        }

        // ── POST: /Query/Ask ──
        // Make query and show response
        [HttpPost]
        public async Task<IActionResult> Ask(string questionText, DateTime? contextDate, string? contextInfo)
        {
            var proxyFactory = new ServiceProxyFactory(c =>
                new FabricTransportServiceRemotingClientFactory(
                    remotingSettings: new FabricTransportRemotingSettings
                    {
                        OperationTimeout = TimeSpan.FromMinutes(15)
                    }));
            var queryService = proxyFactory.CreateServiceProxy<IQueryService>(
                new Uri(QueryServiceUri));

            var request = new QueryRequest
            {
                QuestionText = questionText,
                ContextDate = contextDate,
                ContextInfo = contextInfo ?? ""
            };

            var response = await queryService.AskQuestion(request);

            ViewBag.Response = response;
            ViewBag.Question = questionText;

            return View("Result");
        }
    }
}
