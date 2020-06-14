using System;

namespace Prima.Attributes
{
    public class RateLimitAttribute : Attribute
    {
        public int TimeSeconds { get; set; }

        public bool Global { get; set; }
    }
}