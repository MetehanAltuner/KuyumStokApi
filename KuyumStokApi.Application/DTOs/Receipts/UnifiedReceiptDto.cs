using System;
using System.Collections.Generic;

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
        public int StockId { get; set; }

        /// <summary>Satılacak adet (>=1).</summary>
        public int Quantity { get; set; } = 1;

        /// <summary>Satır bazında satış fiyatı.</summary>
        public decimal SoldPrice { get; set; }
    }

    /// <summary>Alış kalemi: stok oluşturmak için gerekli temel alanlar.</summary>
    public sealed class UnifiedReceiptPurchaseItem
    {
        public int ProductVariantId { get; set; }
        public int BranchId { get; set; }
        public string? Barcode { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal PurchasePrice { get; set; }
        public bool GenerateQrCode { get; set; }

        // Opsiyonel fiziksel özellikler
        public decimal? Gram { get; set; }
        public decimal? Thickness { get; set; }
        public decimal? Width { get; set; }
        public string? StoneType { get; set; }
        public decimal? Carat { get; set; }
        public int? Milyem { get; set; }
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
        public int? UserId { get; set; }

        /// <summary>Şube ID (yoksa CurrentUser).</summary>
        public int? BranchId { get; set; }

        // Müşteri Bilgileri
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerNationalId { get; set; }

        // Çoklu Ödeme (UI'daki Nakit/EFT/POS kutularıyla uyumlu)
        public decimal? Cash { get; set; }
        public decimal? Eft { get; set; }
        public decimal? Pos { get; set; }
        public int? BankId { get; set; }           // POS bankası
        public decimal? POS_CommissionRate { get; set; }
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
        public IReadOnlyList<int> AffectedStockIds { get; set; } = Array.Empty<int>();
    }
}

