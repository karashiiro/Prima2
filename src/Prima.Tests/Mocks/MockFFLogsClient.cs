using System.Collections.Generic;
using System.Threading.Tasks;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Models.FFLogs;

namespace Prima.Tests.Mocks
{
    public class MockFFLogsClient : IFFLogsClient
    {
        private LogInfo _responseToReturn;

        public void SetupLog(LogInfo logInfo)
        {
            _responseToReturn = logInfo;
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public Task<T> MakeGraphQLRequest<T>(string query)
        {
            if (_responseToReturn != null && typeof(T) == typeof(LogInfo))
            {
                return Task.FromResult((T)(object)_responseToReturn);
            }

            return Task.FromResult(default(T));
        }
    }

    public class MockBatchActorMapper : IBatchActorMapper
    {
        private Dictionary<int, DiscordXIVUser> _userMapping = new();

        public void SetupUsers(Dictionary<int, DiscordXIVUser> userMapping)
        {
            _userMapping = userMapping;
        }

        public Task<Dictionary<int, DiscordXIVUser>> GetUsersFromActors(
            IEnumerable<LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor> actors)
        {
            return Task.FromResult(_userMapping);
        }
    }
}