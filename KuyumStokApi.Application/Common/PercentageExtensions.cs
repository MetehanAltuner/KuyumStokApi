using System;

namespace KuyumStokApi.Application.Common
{
    public static class PercentageExtensions
    {
        // Percentage rounding policy: >= .50 up, < .50 down; output as int
        public static int ToRoundedPercentInt(this decimal value)
        {
            if (value == 0m)
                return 0;

            var sign = value < 0m ? -1 : 1;
            var abs = Math.Abs(value);
            var truncated = decimal.Truncate(abs);
            var fraction = abs - truncated;

            var rounded = fraction >= 0.5m ? truncated + 1m : truncated;
            return sign * (int)rounded;
        }
    }
}
