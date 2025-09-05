using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Sales
{
    /// <summary>Satış kalemi (stok çıkış) DTO’su.</summary>
    public sealed class SaleItemDto
    {
        public int StockId { get; set; }         // hangi stok satılıyor
        public int Quantity { get; set; }        // -adet
        public decimal SoldPrice { get; set; }   // detay tablosu için
    }

    /// <summary>Satış fişi oluşturma DTO’su.</summary>
    public sealed class SaleCreateDto
    {
        public int UserId { get; set; }          // opsiyonel: CurrentUser’dan da alınabilir
        public int BranchId { get; set; }
        public int? CustomerId { get; set; }
        public int? PaymentMethodId { get; set; }
        public int? BankId { get; set; }         // kart vb. ise opsiyonel
        public decimal? CommissionRate { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public List<SaleItemDto> Items { get; set; } = new();
    }

    public sealed class SaleResultDto
    {
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IReadOnlyList<int> StockIds { get; set; } = Array.Empty<int>();
    }
    public sealed class SaleFilter
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

    public sealed class SaleListDto
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

    public sealed class SaleDetailLineDto
    {
        public int Id { get; init; }
        public int StockId { get; init; }
        public string? Barcode { get; init; }
        public int Quantity { get; init; }
        public decimal? SoldPrice { get; init; }

        public int? ProductVariantId { get; init; }
        public string? VariantDisplay { get; init; }
    }

    public sealed class SaleDetailDto
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

        public IReadOnlyList<SaleDetailLineDto> Lines { get; init; } = Array.Empty<SaleDetailLineDto>();
    }
}
