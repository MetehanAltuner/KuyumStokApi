using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Purchase
{
    /// <summary>Alış kalemi (stok giriş) DTO’su.</summary>
    public sealed class PurchaseItemDto
    {
        public int ProductVariantId { get; set; }
        public int BranchId { get; set; }               // istasyon/şube
        public string Barcode { get; set; } = default!; // stocks.barcode UNIQUE
        public int Quantity { get; set; }               // +adet
        public decimal PurchasePrice { get; set; }      // detay tablosu için
    }

    /// <summary>Alış fişi oluşturma DTO’su.</summary>
    public sealed class PurchaseCreateDto
    {
        public int UserId { get; set; }                 // opsiyonel: CurrentUser’dan da alınabilir
        public int BranchId { get; set; }
        public int? CustomerId { get; set; }
        public int? PaymentMethodId { get; set; }
        public List<PurchaseItemDto> Items { get; set; } = new();
    }

    /// <summary>Oluşan alış fişi dönen DTO.</summary>
    public sealed class PurchaseResultDto
    {
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IReadOnlyList<int> StockIds { get; set; } = Array.Empty<int>();
    }
    public sealed class PurchaseFilter
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public int? BranchId { get; init; }
        public int? UserId { get; init; }
        public int? CustomerId { get; init; }
        public int? PaymentMethodId { get; init; }

        public DateTime? FromUtc { get; init; }
        public DateTime? ToUtc { get; init; }
    }

    public sealed class PurchaseListDto
    {
        public int Id { get; init; }
        public DateTime? CreatedAt { get; init; }
        public int? BranchId { get; init; }
        public string? BranchName { get; init; }
        public int? UserId { get; init; }
        public string? UserName { get; init; }
        public int? CustomerId { get; init; }
        public string? CustomerName { get; init; }
        public int? PaymentMethodId { get; init; }
        public string? PaymentMethod { get; init; }
        public decimal TotalAmount { get; init; }
        public int ItemCount { get; init; }
    }

    public sealed class PurchaseDetailLineDto
    {
        public int Id { get; init; }
        public int StockId { get; init; }
        public string? Barcode { get; init; }
        public int Quantity { get; init; }
        public decimal? PurchasePrice { get; init; }

        // vitrin için okunur isim (varsa)
        public int? ProductVariantId { get; init; }
        public string? VariantDisplay { get; init; }
    }

    public sealed class PurchaseDetailDto
    {
        public int Id { get; init; }
        public DateTime? CreatedAt { get; init; }
        public int? BranchId { get; init; }
        public string? BranchName { get; init; }
        public int? UserId { get; init; }
        public string? UserName { get; init; }
        public int? CustomerId { get; init; }
        public string? CustomerName { get; init; }
        public int? PaymentMethodId { get; init; }
        public string? PaymentMethod { get; init; }

        public decimal TotalAmount { get; init; }
        public int ItemCount { get; init; }

        public IReadOnlyList<PurchaseDetailLineDto> Lines { get; init; } = Array.Empty<PurchaseDetailLineDto>();
    }
}
