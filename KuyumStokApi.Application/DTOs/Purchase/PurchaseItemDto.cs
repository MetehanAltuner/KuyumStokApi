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
}
