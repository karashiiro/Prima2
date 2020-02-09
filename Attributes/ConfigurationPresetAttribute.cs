using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Prima.Services;
using System;
using System.Threading.Tasks;

namespace Prima.Attributes
{
    public class ConfigurationPresetAttribute : PreconditionAttribute
    {
        private readonly Preset _preset;

        public ConfigurationPresetAttribute(Preset preset)
        {
            _preset = preset;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (_preset == services.GetRequiredService<ConfigurationService>().CurrentPreset)
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError(Properties.Resources.IncompatiblePresetError));
        }
    }
}
