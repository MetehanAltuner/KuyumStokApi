using System;

namespace KuyumStokApi.Infrastructure.Validation
{
    internal static class PositiveNumberGuard
    {
        public static void RequirePositive(string fieldName, int value)
        {
            if (value <= 0)
                throw new InvalidOperationException($"{fieldName} must be greater than 0.");
        }

        public static void RequirePositive(string fieldName, int? value)
        {
            if (value.HasValue)
                RequirePositive(fieldName, value.Value);
        }

        public static void RequirePositive(string fieldName, long value)
        {
            if (value <= 0)
                throw new InvalidOperationException($"{fieldName} must be greater than 0.");
        }

        public static void RequirePositive(string fieldName, long? value)
        {
            if (value.HasValue)
                RequirePositive(fieldName, value.Value);
        }

        public static void RequirePositive(string fieldName, double value)
        {
            if (value <= 0)
                throw new InvalidOperationException($"{fieldName} must be greater than 0.");
        }

        public static void RequirePositive(string fieldName, double? value)
        {
            if (value.HasValue)
                RequirePositive(fieldName, value.Value);
        }

        public static void RequirePositive(string fieldName, decimal value)
        {
            if (value <= 0)
                throw new InvalidOperationException($"{fieldName} must be greater than 0.");
        }

        public static void RequirePositive(string fieldName, decimal? value)
        {
            if (value.HasValue)
                RequirePositive(fieldName, value.Value);
        }
    }
}
