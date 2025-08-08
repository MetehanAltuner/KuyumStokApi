using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class ProductCategories
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ProductTypes> ProductTypes { get; set; } = new List<ProductTypes>();
}
