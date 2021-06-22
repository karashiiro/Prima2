using System.Collections.Generic;

namespace Prima.DiscordNet.Services
{
    public interface ITemplateProvider
    {
        public IEnumerable<string> GetNames();

        public ResolvedTemplate Execute<T>(string templateName, T templateData) where T : class;
    }
}