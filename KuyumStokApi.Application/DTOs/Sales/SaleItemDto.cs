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
}
