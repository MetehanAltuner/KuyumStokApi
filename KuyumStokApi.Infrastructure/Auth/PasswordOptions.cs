using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Auth
{
    public sealed class PasswordOptions
    {
        [Range(1_000, 1_000_000)]
        public int Iterations { get; init; } = 100_000;

        // Opsiyonel ama önerilir: uygulama seviyesinde “pepper”
        [MinLength(0)]
        public string Pepper { get; init; } = string.Empty;
    }
}
