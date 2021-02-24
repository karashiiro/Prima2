namespace Prima.Stable.Resources
{
    public static class FFLogs
    {
        public static string BuildMembersRequest(string logId, int fightId)
        {
            return $@"
            {{
                reportData: {{
                    report(code: ""{logId}"") {{
                        fights(fightIDs: [{fightId}]) {{
                            encounterID,
                            kill,
                            endTime,
                            startTime,
                            name,
                            friendlyPlayers
                        }},
                        masterData {{
                            actors {{
                                name,
                                server
                            }}
                        }}
                    }}
                }}
            }}";
        }
    }
}