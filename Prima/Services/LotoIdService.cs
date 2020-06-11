using System;
using Yisoft.Crontab;

namespace Prima.Services
{
    public class LotoIdService
    {
        public byte CurrentByte { get; private set; }
        public ushort CurrentUShort { get; private set; }
        public uint CurrentUInt { get; private set; }
        public ulong CurrentULong { get; private set; }

        [Cron("0 0 * * * *")]
        public void NextDraw()
        {
            var generator = new Random();
            CurrentByte = (byte)generator.Next();
            CurrentUShort = (ushort)generator.Next();
            CurrentUInt = (uint)generator.Next();
            CurrentULong = (ulong)generator.Next() << 32;
            CurrentULong |= (uint)generator.Next();
        }
    }
}