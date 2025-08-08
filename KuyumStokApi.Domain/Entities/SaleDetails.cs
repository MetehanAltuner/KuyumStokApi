using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class SaleDetails
{
    public int Id { get; set; }

    public int? SaleId { get; set; }

    public int? Quantity { get; set; }

    public decimal? SoldPrice { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? StockId { get; set; }

    public virtual Sales? Sale { get; set; }

    public virtual Stocks? Stock { get; set; }
}
