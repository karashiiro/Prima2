using System;

namespace Prima.DiscordNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleScopeAttribute : Attribute
    {
        public enum ModuleScoping
        {
            Global,
            Guild,
        }

        public ModuleScoping Scope { get; }

        public ulong GuildId { get; init; }

        public ModuleScopeAttribute(ModuleScoping scope)
        {
            Scope = scope;
        }
    }
}