using System;

namespace KuyumStokApi.Application.DTOs.Customers
{
    /// <summary>
    /// Müşteri detayında kullanılmak üzere satış kalemlerinin ham satır verisi.
    /// Satırları satış başlıkları ile eşleyebilmek için SaleId içerir.
    /// </summary>
    public sealed class CustomerSaleLineItemDto
    {
        public int SaleId { get; init; }
        public int LineId { get; init; }
        public Guid? StockId { get; init; }
        public int? Quantity { get; init; }
        public decimal TotalWeightGram { get; init; }
        public decimal? SoldPrice { get; init; }

        public int? ProductVariantId { get; init; }
        public string? ProductVariantName { get; init; }
        public int? ProductTypeId { get; init; }
        public string? ProductTypeName { get; init; }
        public string? CategoryName { get; init; }
        public string? Brand { get; init; }
        public string? Ayar { get; init; }
        public string? Color { get; init; }
    }

    /// <summary>
    /// Müşteri detayında kullanılmak üzere alış kalemlerinin ham satır verisi.
    /// Satırları alış başlıkları ile eşleyebilmek için PurchaseId içerir.
    /// </summary>
    public sealed class CustomerPurchaseLineItemDto
    {
        public int PurchaseId { get; init; }
        public int LineId { get; init; }
        public Guid? StockId { get; init; }
        public int? Quantity { get; init; }
        public decimal TotalWeightGram { get; init; }
        public decimal? PurchasePrice { get; init; }

        public int? ProductVariantId { get; init; }
        public string? ProductVariantName { get; init; }
        public int? ProductTypeId { get; init; }
        public string? ProductTypeName { get; init; }
        public string? CategoryName { get; init; }
        public string? Brand { get; init; }
        public string? Ayar { get; init; }
        public string? Color { get; init; }
    }
}

