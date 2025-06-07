using System.Threading.Tasks;

namespace Prima.Game.FFXIV.FFLogs
{
    public interface IFFLogsClient
    {
        Task Initialize();
        Task<T> MakeGraphQLRequest<T>(string query);
    }
}