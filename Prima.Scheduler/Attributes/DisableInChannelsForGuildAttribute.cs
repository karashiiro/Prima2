using System;
using System.Collections.Generic;

namespace Prima.Scheduler.Attributes
{
    public class DisableInChannelsForGuildAttribute : Attribute
    {
        public IEnumerable<ulong> ChannelIds { get; }

        public ulong Guild { get; set; }

        public DisableInChannelsForGuildAttribute(params ulong[] channelIds)
        {
            ChannelIds = channelIds;
        }
    }
}