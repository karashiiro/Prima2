using System.Text.RegularExpressions;

namespace Prima.Stable
{
    public static class FFLogs
    {
        public static readonly Regex LogLinkToIdRegex = new Regex(@"[a-zA-Z0-9]{10,}", RegexOptions.Compiled);
        public static readonly Regex IsLogLink = new Regex(@"fflogs\.com\/reports\/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string BuildLogRequest(string logId)
        {
            return $@"{{
    reportData {{
        report(code: ""{logId}"") {{
            fights {{
                kill
                name
                friendlyPlayers
            }}
            masterData {{
                actors {{
                    id
                    name
                    server
                }}
            }}
        }}
    }}
}}";
        }
    }
}