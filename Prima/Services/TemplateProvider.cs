using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Prima.Services
{
    public class TemplateProvider
    {
        private readonly IReadOnlyDictionary<string, string> _templates;

        public TemplateProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _templates = assembly.GetManifestResourceNames()
                .Where(resourceName => resourceName.StartsWith("Prima.Templates"))
                .Select(resourceName =>
                {
                    using var s = assembly.GetManifestResourceStream(resourceName);
                    if (s == null)
                    {
                        throw new InvalidOperationException($"Resource {resourceName} failed to be loaded from the assembly.");
                    }

                    var resourceKey = resourceName
                        .Replace(".md", "")
                        .Replace(".", "/")
                        .ToLowerInvariant();

                    using var sr = new StreamReader(s);
                    return new { Name = resourceKey, Data = sr.ReadToEnd() };
                })
                .ToImmutableDictionary(resource => resource.Name, resource => resource.Data);
        }

        public string Execute<T>(string templateName, T templateData) where T : class
        {
            var template = _templates[templateName];
            var replaceableTokens = GetReplaceableTokens(template);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var token in replaceableTokens)
            {
                template = template.Replace("{{." + token + "}}", templateData.GetPropertyValue(token)?.ToString());
            }

            return template;
        }

        private static IEnumerable<string> GetReplaceableTokens(string template)
        {
            return template
                .Split('\n', '\r', ' ')
                .Where(token => token.StartsWith("{{.") && token.EndsWith("}}"))
                .Select(token => token[3..^2])
                .Distinct();
        }
    }
}