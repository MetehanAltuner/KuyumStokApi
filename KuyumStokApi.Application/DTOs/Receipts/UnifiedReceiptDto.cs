using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KuyumStokApi.Application.Validation.Attributes;

namespace KuyumStokApi.Application.DTOs.Receipts
{
    /// <summary>
    /// Fiş türü: Satış, Alış veya Mixed (aynı fişte hem satış hem alış).
    /// </summary>
    public enum ReceiptMode
    {
        Sale = 1,
        Purchase = 2,
        Mixed = 3
    }

    /// <summary>
    /// Satış kalemi: stoktan düşmek için yalnızca stok ve adet bilgisi gerekir, fiyat satır bazında gelir.
    /// </summary>
    public sealed class UnifiedReceiptSaleItem
    {
        /// <summary>Satılacak stok kimliği.</summary>
        public Guid StockId { get; set; }

        /// <summary>Satılacak adet (>=1).</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; } = 1;

        /// <summary>Satır bazında satış fiyatı.</summary>
        [GreaterThanZero(ErrorMessage = "SoldPrice must be greater than 0.")]
        public decimal SoldPrice { get; set; }

        /// <summary>Satır toplam ağırlığı (gram).</summary>
        [GreaterThanZero(ErrorMessage = "TotalWeightGram must be greater than 0.")]
        public decimal TotalWeightGram { get; set; }
    }

    /// <summary>Alış kalemi: stok oluşturmak için gerekli temel alanlar.</summary>
    public sealed class UnifiedReceiptPurchaseItem
    {
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId must be greater than 0.")]
        public int ProductVariantId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int BranchId { get; set; }
        public string? Barcode { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; } = 1;
        [GreaterThanZero(ErrorMessage = "PurchasePrice must be greater than 0.")]
        public decimal PurchasePrice { get; set; }
        [GreaterThanZero(ErrorMessage = "TotalWeightGram must be greater than 0.")]
        public decimal TotalWeightGram { get; set; }
        public bool GenerateQrCode { get; set; }

        // Opsiyonel fiziksel özellikler
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
        public string? QrCode { get; set; }
    }

    /// <summary>
    /// Birleşik fiş oluşturma DTO (Satış + Alış aynı transaction'da).
    /// </summary>
    public sealed class UnifiedReceiptCreateDto
    {
        /// <summary>Fiş türü (Sale, Purchase, Mixed).</summary>
        public ReceiptMode Mode { get; set; } = ReceiptMode.Mixed;

        /// <summary>Kullanıcı ID (yoksa CurrentUser).</summary>
        [Range(1, int.MaxValue, ErrorMessage = "UserId must be greater than 0.")]
        public int? UserId { get; set; }

        /// <summary>Şube ID (yoksa CurrentUser).</summary>
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int? BranchId { get; set; }

        // Müşteri Bilgileri
        [Range(1, int.MaxValue, ErrorMessage = "CustomerId must be greater than 0.")]
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerNationalId { get; set; }

        // Çoklu Ödeme (UI'daki Nakit/EFT/POS kutularıyla uyumlu)
        public decimal? Cash { get; set; }
        public decimal? Eft { get; set; }
        public decimal? Pos { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "BankId must be greater than 0.")]
        public int? BankId { get; set; }           // POS bankası
        public decimal? POS_CommissionRate { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "PaymentMethodId must be greater than 0.")]
        public int? PaymentMethodId { get; set; }  // genel seçim (opsiyonel)

        // Satış ve Alış kalemleri
        public List<UnifiedReceiptSaleItem> SaleItems { get; set; } = new();
        public List<UnifiedReceiptPurchaseItem> PurchaseItems { get; set; } = new();
    }

    /// <summary>
    /// Birleşik fiş sonuç DTO.
    /// </summary>
    public sealed class UnifiedReceiptResultDto
    {
        public int ReceiptId { get; set; }
        public int? SaleId { get; set; }
        public int? PurchaseId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IReadOnlyList<Guid> AffectedStockIds { get; set; } = Array.Empty<Guid>();
    }
}

