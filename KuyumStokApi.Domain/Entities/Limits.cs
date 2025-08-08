using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Limits
{
    public int Id { get; set; }

    public int? BranchId { get; set; }

    public int? ProductVariantId { get; set; }

    public decimal? MinThreshold { get; set; }

    public decimal? MaxThreshold { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branches? Branch { get; set; }

    public virtual ProductVariants? ProductVariant { get; set; }
}
