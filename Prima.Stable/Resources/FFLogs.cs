using System.Text.RegularExpressions;

namespace Prima.Stable.Resources
{
    public static class FFLogs
    {
        public static Regex LogLinkToIdRegex = new Regex(@"[a-zA-Z0-9]{10,}", RegexOptions.Compiled);

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