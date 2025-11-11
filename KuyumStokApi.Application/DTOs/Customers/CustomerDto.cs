using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Customers
{
    /// <summary>Müşteri özet bilgisi.</summary>
    public sealed class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Note { get; set; }
        public string? NationalId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>Yeni müşteri oluşturma talebi.</summary>
    public sealed class CustomerCreateDto
    {
        public string Name { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Note { get; set; }
        public string? NationalId { get; set; }
    }

    /// <summary>Müşteri güncelleme talebi.</summary>
    public sealed class CustomerUpdateDto
    {
        public string Name { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Note { get; set; }
        public string? NationalId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Müşteri listeleme filtresi.</summary>
    public sealed class CustomerFilter
    {
        public string? Query { get; set; }
        public bool? OnlyActive { get; set; } = true;
        public DateTime? UpdatedFromUtc { get; set; }
        public DateTime? UpdatedToUtc { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
