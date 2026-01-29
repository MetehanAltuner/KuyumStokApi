using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.PaymentMethods
{
    /// <summary>Ödeme yöntemi özet DTO.</summary>
    public sealed class PaymentMethodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; }
    }

    /// <summary>Ödeme yöntemi oluşturma DTO.</summary>
    public sealed class PaymentMethodCreateDto
    {
        public string Name { get; set; } = default!;
    }

    /// <summary>Ödeme yöntemi güncelleme DTO.</summary>
    public sealed class PaymentMethodUpdateDto
    {
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Ödeme yöntemi listeleme filtresi.</summary>
    public sealed class PaymentMethodFilter
    {
        public string? Query { get; set; }
        public bool? OnlyActive { get; set; } = true;
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        public int Page { get; set; } = 1;
        [Range(1, int.MaxValue, ErrorMessage = "PageSize must be greater than 0.")]
        public int PageSize { get; set; } = 20;
    }
}
