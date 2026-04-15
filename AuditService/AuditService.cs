using System.Fabric;
using Microsoft.Data.SqlClient;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Common.Helpers;
using Common.Interfaces;
using Common.Models.Query;

namespace AuditService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class AuditService : StatefulService, IAuditService
    {
        private SqlHelper _sqlHelper = null!;
        private const string QueueName = "AuditLogQueue";

        public AuditService(StatefulServiceContext context) : base(context) { }

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

            var queue = await StateManager.GetOrAddAsync<IReliableQueue<QueryLog>>(QueueName);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var tx = StateManager.CreateTransaction();
                    var item = await queue.TryDequeueAsync(tx, TimeSpan.FromSeconds(4), cancellationToken);

                    if (item.HasValue)
                    {
                        await InsertQueryLogToSql(item.Value);
                        await tx.CommitAsync();
                    }
                    else
                    {
                        // Queue empty, wait 1 second
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
        }

        public async Task LogQuery(QueryLog log)
        {
            var queue = await StateManager.GetOrAddAsync<IReliableQueue<QueryLog>>(QueueName);

            using var tx = StateManager.CreateTransaction();
            await queue.EnqueueAsync(tx, log);
            await tx.CommitAsync();
        }

        public async Task<List<QueryLog>> GetQueryHistory(int page, int pageSize)
        {
            var logs = new List<QueryLog>();
            int offset = (page - 1) * pageSize;

            string sql = @"
                SELECT Id, QuestionText, ContextDate, ContextInfo,
                       ResponseText, ConfidenceLevel, ReferencedSections,
                       CreatedAt, ProcessingTimeMs
                FROM QueryLogs
                ORDER BY CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var (connection, reader) = await _sqlHelper.ExecuteReaderAsync(sql,
                new SqlParameter("@Offset", offset),
                new SqlParameter("@PageSize", pageSize));

            using (connection)
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    logs.Add(new QueryLog
                    {
                        Id = reader.GetInt32(0),
                        QuestionText = reader.GetString(1),
                        ContextDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                        ContextInfo = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        ResponseText = reader.GetString(4),
                        ConfidenceLevel = reader.GetDouble(5),
                        ReferencedSections = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        CreatedAt = reader.GetDateTime(7),
                        ProcessingTimeMs = reader.GetInt32(8)
                    });
                }
            }
            return logs;
        }

        public async Task<int> GetTotalQueryCount()
        {
            string sql = "SELECT COUNT(*) FROM QueryLogs";

            using var connection = _sqlHelper.CreateConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task InsertQueryLogToSql(QueryLog log)
        {
            string sql = @"
                INSERT INTO QueryLogs
                    (QuestionText, ContextDate, ContextInfo, ResponseText,
                     ConfidenceLevel, ReferencedSections, CreatedAt, ProcessingTimeMs)
                VALUES
                    (@QuestionText, @ContextDate, @ContextInfo, @ResponseText,
                     @ConfidenceLevel, @ReferencedSections, @CreatedAt, @ProcessingTimeMs)";

            await _sqlHelper.ExecuteNonQueryAsync(sql,
                new SqlParameter("@QuestionText", log.QuestionText),
                new SqlParameter("@ContextDate", (object?)log.ContextDate ?? DBNull.Value),
                new SqlParameter("@ContextInfo", (object?)log.ContextInfo ?? DBNull.Value),
                new SqlParameter("@ResponseText", log.ResponseText),
                new SqlParameter("@ConfidenceLevel", log.ConfidenceLevel),
                new SqlParameter("@ReferencedSections", (object?)log.ReferencedSections ?? DBNull.Value),
                new SqlParameter("@CreatedAt", log.CreatedAt),
                new SqlParameter("@ProcessingTimeMs", log.ProcessingTimeMs));
        }
    }
}
