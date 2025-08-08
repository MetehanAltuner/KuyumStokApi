using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class PurchaseDetails
{
    public int Id { get; set; }

    public int? PurchaseId { get; set; }

    public int? Quantity { get; set; }

    public decimal? PurchasePrice { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? StockId { get; set; }

    public virtual Purchases? Purchase { get; set; }

    public virtual Stocks? Stock { get; set; }
}
