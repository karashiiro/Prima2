using System.Text.RegularExpressions;

namespace Prima.Stable
{
    public static class FFLogs
    {
        private static readonly Regex IsLogLinkRegex = new(@"fflogs\.com\/reports\/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex LogLinkToIdRegex = new(@"[a-zA-Z0-9]{10,}", RegexOptions.Compiled);

        public static bool IsLogLink(string text)
            => IsLogLinkRegex.Match(text).Success;

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