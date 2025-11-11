using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Stocks
{
    public int Id { get; set; }

    public int? ProductVariantId { get; set; }

    public int? BranchId { get; set; }

    public int? Quantity { get; set; }

    public string Barcode { get; set; } = null!;

    public string? QrCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public decimal? Gram { get; set; }

    public decimal? Thickness { get; set; }

    public decimal? Width { get; set; }

    public string? StoneType { get; set; }

    public decimal? Carat { get; set; }

    public int? Milyem { get; set; }

    public string? Color { get; set; }

    public virtual Branches? Branch { get; set; }

    public virtual ICollection<ProductLifecycles> ProductLifecycles { get; set; } = new List<ProductLifecycles>();

    public virtual ProductVariants? ProductVariant { get; set; }

    public virtual ICollection<PurchaseDetails> PurchaseDetails { get; set; } = new List<PurchaseDetails>();

    public virtual ICollection<SaleDetails> SaleDetails { get; set; } = new List<SaleDetails>();
}
