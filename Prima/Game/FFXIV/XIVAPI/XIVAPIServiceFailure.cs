using System;
using System.Runtime.Serialization;

namespace Prima.Game.FFXIV.XIVAPI
{
    public class XIVAPIServiceFailure : Exception
    {
        public XIVAPIServiceFailure() { }
        public XIVAPIServiceFailure(string message) : base(message) { }
        public XIVAPIServiceFailure(string message, Exception inner) : base(message, inner) { }
        protected XIVAPIServiceFailure(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}