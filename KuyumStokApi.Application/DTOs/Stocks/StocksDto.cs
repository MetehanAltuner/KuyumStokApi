using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Stocks
{
    /// <summary>Stok kaydı DTO.</summary>
    public sealed class StockDto
    {
        /// <summary>Stok kimliği.</summary>
        public int Id { get; set; }

        /// <summary>Bağlı ürün varyantı özeti.</summary>
        public VariantBrief? ProductVariant { get; set; }

        /// <summary>Bağlı şube özeti.</summary>
        public BranchBrief? Branch { get; set; }

        /// <summary>Adet.</summary>
        public int? Quantity { get; set; }

        /// <summary>Barkod (unique).</summary>
        public string Barcode { get; set; } = null!;

        /// <summary>QR kod.</summary>
        public string? QrCode { get; set; }

        /// <summary>Oluşturulma (UTC).</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>Güncellenme (UTC).</summary>
        public DateTime? UpdatedAt { get; set; }

        public sealed class VariantBrief
        {
            public int? Id { get; set; }
            public string? Ayar { get; set; }
            public decimal? Gram { get; set; }
            public string? Brand { get; set; }
            public int? ProductTypeId { get; set; }
            public string? ProductTypeName { get; set; }
        }

        public sealed class BranchBrief
        {
            public int? Id { get; set; }
            public string? Name { get; set; }
        }
    }

    /// <summary>Stok oluşturma DTO.</summary>
    public sealed class StockCreateDto
    {
        public int? ProductVariantId { get; set; }
        public int? BranchId { get; set; }
        public int? Quantity { get; set; }
        public string Barcode { get; set; } = null!;
        public string? QrCode { get; set; }
    }

    /// <summary>Stok güncelleme DTO.</summary>
    public sealed class StockUpdateDto
    {
        public int? ProductVariantId { get; set; }
        public int? BranchId { get; set; }
        public int? Quantity { get; set; }
        public string? Barcode { get; set; } // boşsa değiştirme
        public string? QrCode { get; set; }
    }

    /// <summary>Stoklar için filtre/sayfalama.</summary>
    public sealed record StockFilter(
        int Page = 1,
        int PageSize = 20,
        string? Query = null,           // barcode/qr/brand/ayar serbest arama
        int? BranchId = null,
        int? ProductTypeId = null,
        int? ProductVariantId = null,
        decimal? GramMin = null,
        decimal? GramMax = null,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );
}
