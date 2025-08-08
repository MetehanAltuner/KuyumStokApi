using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class ProductLifecycles
{
    public int Id { get; set; }

    public int? StockId { get; set; }

    public int? UserId { get; set; }

    public string? Notes { get; set; }

    public DateTime? Timestamp { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? ActionId { get; set; }

    public virtual LifecycleActions? Action { get; set; }

    public virtual Stocks? Stock { get; set; }

    public virtual Users? User { get; set; }
}
