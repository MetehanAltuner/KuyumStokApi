using System;

namespace KuyumStokApi.Application.DTOs.Stocks
{
    /// <summary>Stok kaydı DTO (liste satırı).</summary>
    public sealed class StockDto
    {
        public int Id { get; set; }

        public VariantBrief? ProductVariant { get; set; }
        public BranchBrief? Branch { get; set; }

        public int? Quantity { get; set; }
        public string Barcode { get; set; } = null!;
        public string? QrCode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Satır için toplam ağırlık = (Gram * Adet)</summary>
        public decimal TotalWeight { get; set; }

        public sealed class VariantBrief
        {
            public int? Id { get; set; }
            public string? Name { get; set; }              // model (Ajda Bilezik)
            public string? Ayar { get; set; }
            public string? Color { get; set; }
            public string? Brand { get; set; }

            public decimal? Gram { get; set; }             // Stocks.Gram
            public int? ProductTypeId { get; set; }
            public string? ProductTypeName { get; set; }   // tür (bilezik, yüzük)
            public string? CategoryName { get; set; }      // kategori (Altın, Gümüş)
        }

        public sealed class BranchBrief
        {
            public int? Id { get; set; }
            public string? Name { get; set; }
        }
    }

    /// <summary>Stok oluşturma DTO (Entity ile tam uyumlu, merge için gerekli alanlar).</summary>
    public sealed class StockCreateDto
    {
        public int ProductVariantId { get; set; }
        public int? BranchId { get; set; }      // yoksa CurrentUser.BranchId
        public int Quantity { get; set; }       // >= 1
        public string? Barcode { get; set; }
        public string? QrCode { get; set; }
        public bool GenerateQrCode { get; set; }

        // Fiziksel/ayrıştırıcı özellikler (merge için şart)
        public decimal? Gram { get; set; }
        public decimal? Thickness { get; set; }
        public decimal? Width { get; set; }
        public string? StoneType { get; set; }
        public decimal? Carat { get; set; }
        public int? Milyem { get; set; }
        public string? Color { get; set; }
    }

    /// <summary>Stok güncelleme DTO.</summary>
    public sealed class StockUpdateDto
    {
        public int? ProductVariantId { get; set; }
        public int? BranchId { get; set; }
        public int? Quantity { get; set; }
        public string? Barcode { get; set; } // boşsa değiştirme
        public string? QrCode { get; set; }

        // Fiziksel özellikler (nullable - değiştirilmek istenirse)
        public decimal? Gram { get; set; }
        public decimal? Thickness { get; set; }
        public decimal? Width { get; set; }
        public string? StoneType { get; set; }
        public decimal? Carat { get; set; }
        public int? Milyem { get; set; }
        public string? Color { get; set; }
    }

    /// <summary>Liste filtresi (branch boş ise JWT'den alınır).</summary>
    public sealed record StockFilter(
        int Page = 1,
        int PageSize = 20,
        string? Query = null,           // barcode/qr/variant/brand/ayar/renk
        int? BranchId = null,
        int? ProductTypeId = null,
        int? ProductVariantId = null,
        decimal? GramMin = null,
        decimal? GramMax = null,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );

    /// <summary>Favori/Top seller ürün DTO.</summary>
    public sealed class FavoriteProductDto
    {
        public int VariantId { get; set; }
        public string? VariantName { get; set; }
        public string? Ayar { get; set; }
        public string? Color { get; set; }
        public string? Brand { get; set; }
        public int TotalSoldQty { get; set; }
        public bool IsFavorite { get; set; }
    }
}
