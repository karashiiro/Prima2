namespace Prima.Stable.Resources
{
    public static class FFLogs
    {
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