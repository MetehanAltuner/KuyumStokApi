using System;
using System.Collections.Generic;

namespace KuyumStokApi.Application.DTOs.Customers
{
    /// <summary>Müşteri detay cevabı: müşteri + alış/satış hareketleri.</summary>
    public sealed class CustomerDetailResponseDto
    {
        public CustomerDto Customer { get; set; } = default!;
        public IReadOnlyList<CustomerPurchaseDto> Purchases { get; set; } = Array.Empty<CustomerPurchaseDto>();
        public IReadOnlyList<CustomerSaleDto> Sales { get; set; } = Array.Empty<CustomerSaleDto>();
    }

    public sealed class CustomerPurchaseDto
    {
        public int Id { get; init; }
        public DateTime? CreatedAt { get; init; }
        public decimal TotalAmount { get; set; }
        public int? PaymentMethodId { get; init; }
        public string? PaymentMethodName { get; init; }
        public int? BranchId { get; init; }
        public string? BranchName { get; init; }
        public int? UserId { get; init; }
        public string? UserFullName { get; init; }
        public IReadOnlyList<CustomerPurchaseLineDto> LineItems { get; set; } = Array.Empty<CustomerPurchaseLineDto>();
    }

    public sealed class CustomerPurchaseLineDto
    {
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

    public sealed class CustomerSaleDto
    {
        public int Id { get; init; }
        public DateTime? CreatedAt { get; init; }
        public decimal TotalAmount { get; set; }
        public int? PaymentMethodId { get; init; }
        public string? PaymentMethodName { get; init; }
        public int? BranchId { get; init; }
        public string? BranchName { get; init; }
        public int? UserId { get; init; }
        public string? UserFullName { get; init; }
        public IReadOnlyList<CustomerSaleLineDto> LineItems { get; set; } = Array.Empty<CustomerSaleLineDto>();
    }

    public sealed class CustomerSaleLineDto
    {
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
}
