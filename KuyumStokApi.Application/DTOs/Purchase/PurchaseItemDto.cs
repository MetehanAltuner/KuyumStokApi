using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KuyumStokApi.Application.Validation.Attributes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Purchase
{
    /// <summary>Alış kalemi (stok giriş) DTO'su.</summary>
    public sealed class PurchaseItemDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId must be greater than 0.")]
        public int ProductVariantId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int BranchId { get; set; }               // istasyon/şube
        public string Barcode { get; set; } = default!; // stocks.barcode UNIQUE
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }               // +adet
        [GreaterThanZero(ErrorMessage = "PurchasePrice must be greater than 0.")]
        public decimal PurchasePrice { get; set; }      // detay tablosu için
        [GreaterThanZero(ErrorMessage = "TotalWeightGram must be greater than 0.")]
        public decimal TotalWeightGram { get; set; }    // satır toplam ağırlık
        [Range(1, int.MaxValue, ErrorMessage = "WorkmanshipMilyem must be greater than 0.")]
        public int? WorkmanshipMilyem { get; set; }
    }

    /// <summary>Alış fişi oluşturma DTO’su.</summary>
    public sealed class PurchaseCreateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "UserId must be greater than 0.")]
        public int UserId { get; set; }                 // opsiyonel: CurrentUser’dan da alınabilir
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int BranchId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "CustomerId must be greater than 0.")]
        public int? CustomerId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "PaymentMethodId must be greater than 0.")]
        public int? PaymentMethodId { get; set; }
        public List<PurchaseItemDto> Items { get; set; } = new();
    }

    /// <summary>Oluşan alış fişi dönen DTO.</summary>
    public sealed class PurchaseResultDto
    {
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IReadOnlyList<Guid> StockIds { get; set; } = Array.Empty<Guid>();
    }
    public sealed class PurchaseFilter
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        public int Page { get; init; } = 1;
        [Range(1, int.MaxValue, ErrorMessage = "PageSize must be greater than 0.")]
        public int PageSize { get; init; } = 20;

        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int? BranchId { get; init; }
        [Range(1, int.MaxValue, ErrorMessage = "UserId must be greater than 0.")]
        public int? UserId { get; init; }
        [Range(1, int.MaxValue, ErrorMessage = "CustomerId must be greater than 0.")]
        public int? CustomerId { get; init; }
        [Range(1, int.MaxValue, ErrorMessage = "PaymentMethodId must be greater than 0.")]
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
        public Guid StockId { get; init; }
        public string? Barcode { get; init; }
        public int Quantity { get; init; }
        public decimal? PurchasePrice { get; init; }
        public decimal TotalWeightGram { get; init; }

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
