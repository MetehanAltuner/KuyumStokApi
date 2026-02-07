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
        public DateTime? PurchaseDate { get; init; }
        public decimal TotalAmount { get; init; }
        public int? PaymentMethodId { get; init; }
        public string? PaymentMethodName { get; init; }
        public int? BranchId { get; init; }
        public string? BranchName { get; init; }
    }

    public sealed class CustomerSaleDto
    {
        public int Id { get; init; }
        public DateTime? SaleDate { get; init; }
        public decimal TotalAmount { get; init; }
        public int? PaymentMethodId { get; init; }
        public string? PaymentMethodName { get; init; }
        public int? BranchId { get; init; }
        public string? BranchName { get; init; }
    }
}
