using System;
using Prima;

namespace TestBed
{
    public static class Program
    {
        public static void Main()
        {
            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());

            var now = DateTime.Now;
            Console.WriteLine(now);

            var runTime = DateTime.FromBinary(637408872000000000);
            Console.WriteLine(runTime);

            var timeDiffHours = (runTime
                .AddDays(-6) - now).TotalHours;
            Console.WriteLine("Time difference (hours): {0}", timeDiffHours);
        }
    }
}
