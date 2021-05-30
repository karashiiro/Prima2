using System.Collections.Generic;
using Prima.Templates;

namespace Prima.Services
{
    public interface ITemplateProvider
    {
        public IEnumerable<string> GetNames();

        public ResolvedTemplate Execute<T>(string templateName, T templateData) where T : class;
    }
}