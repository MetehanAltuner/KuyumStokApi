using System;
using System.ComponentModel.DataAnnotations;

namespace KuyumStokApi.Application.Validation.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class GreaterThanZeroAttribute : ValidationAttribute
    {
        public GreaterThanZeroAttribute()
            : base("The {0} field must be greater than 0.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
            {
                return true; // Let [Required] handle nulls when needed.
            }

            return value switch
            {
                decimal d => d > 0m,
                double d => d > 0d,
                float f => f > 0f,
                long l => l > 0L,
                int i => i > 0,
                short s => s > 0,
                byte b => b > 0,
                sbyte sb => sb > 0,
                uint ui => ui > 0U,
                ulong ul => ul > 0UL,
                ushort us => us > 0,
                _ => false
            };
        }
    }
}
