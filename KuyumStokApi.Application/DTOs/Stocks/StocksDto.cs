using System;
using System.ComponentModel.DataAnnotations;
using KuyumStokApi.Application.Validation.Attributes;

namespace KuyumStokApi.Application.DTOs.Stocks
{
    /// <summary>Stok kaydı DTO (liste satırı).</summary>
    public sealed class StockDto
    {
        public Guid Id { get; set; }

        public VariantBrief? ProductVariant { get; set; }
        public BranchBrief? Branch { get; set; }

        public int? Quantity { get; set; }
        public string Barcode { get; set; } = null!;
        public string? QrCode { get; set; }
        public string? PublicCode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Satır için toplam ağırlık (toplam gram).</summary>
        public decimal TotalWeight { get; set; }

        public int? WorkmanshipMilyem { get; set; }

        /// <summary>Toplam milyem = Ham milyem (varyanttan) + İşçilik milyemi</summary>
        public int? TotalMilyem { get; set; }

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
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId must be greater than 0.")]
        public int ProductVariantId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int? BranchId { get; set; }      // yoksa CurrentUser.BranchId
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }       // >= 1
        public string? Barcode { get; set; }
        public string? QrCode { get; set; }
        public bool GenerateQrCode { get; set; }

        /// <summary>Satır toplam ağırlığı (gram).</summary>
        [GreaterThanZero(ErrorMessage = "TotalWeightGram must be greater than 0.")]
        public decimal TotalWeightGram { get; set; }

        // Fiziksel/ayrıştırıcı özellikler (merge için şart)
        [GreaterThanZero(ErrorMessage = "Gram must be greater than 0.")]
        public decimal? Gram { get; set; }
        [GreaterThanZero(ErrorMessage = "Thickness must be greater than 0.")]
        public decimal? Thickness { get; set; }
        [GreaterThanZero(ErrorMessage = "Width must be greater than 0.")]
        public decimal? Width { get; set; }
        public string? StoneType { get; set; }
        [GreaterThanZero(ErrorMessage = "Carat must be greater than 0.")]
        public decimal? Carat { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "WorkmanshipMilyem must be greater than 0.")]
        public int? WorkmanshipMilyem { get; set; }
        public string? Color { get; set; }

        // Alış fiyatı (nullable - varsa alış kaydı açılır)
        [Required(ErrorMessage = "PurchasePrice is required.")]
        [GreaterThanZero(ErrorMessage = "PurchasePrice must be greater than 0.")]
        public decimal? PurchasePrice { get; set; }
    }

    /// <summary>Stok güncelleme DTO.</summary>
    public sealed class StockUpdateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId must be greater than 0.")]
        public int? ProductVariantId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int? BranchId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int? Quantity { get; set; }
        public string? Barcode { get; set; } // boşsa değiştirme
        public string? QrCode { get; set; }

        // Fiziksel özellikler (nullable - değiştirilmek istenirse)
        [GreaterThanZero(ErrorMessage = "Gram must be greater than 0.")]
        public decimal? Gram { get; set; }
        [GreaterThanZero(ErrorMessage = "Thickness must be greater than 0.")]
        public decimal? Thickness { get; set; }
        [GreaterThanZero(ErrorMessage = "Width must be greater than 0.")]
        public decimal? Width { get; set; }
        public string? StoneType { get; set; }
        [GreaterThanZero(ErrorMessage = "Carat must be greater than 0.")]
        public decimal? Carat { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "WorkmanshipMilyem must be greater than 0.")]
        public int? WorkmanshipMilyem { get; set; }
        public string? Color { get; set; }
    }

    /// <summary>Liste filtresi (branch boş ise JWT'den alınır).</summary>
    public sealed record StockFilter(
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")] int Page = 1,
        [Range(1, int.MaxValue, ErrorMessage = "PageSize must be greater than 0.")] int PageSize = 20,
        string? Query = null,           // barcode/qr/variant/brand/ayar/renk
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")] int? BranchId = null,
        [Range(1, int.MaxValue, ErrorMessage = "ProductTypeId must be greater than 0.")] int? ProductTypeId = null,
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId must be greater than 0.")] int? ProductVariantId = null,
        [GreaterThanZero(ErrorMessage = "GramMin must be greater than 0.")] decimal? GramMin = null,
        [GreaterThanZero(ErrorMessage = "GramMax must be greater than 0.")] decimal? GramMax = null,
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

    /// <summary>Public code backfill sonucu.</summary>
    public sealed class StockPublicCodeBackfillResultDto
    {
        public int UpdatedCount { get; set; }
        public int RemainingCount { get; set; }
    }
}
