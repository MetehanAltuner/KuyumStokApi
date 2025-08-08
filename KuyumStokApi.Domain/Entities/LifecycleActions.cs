using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class LifecycleActions
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<ProductLifecycles> ProductLifecycles { get; set; } = new List<ProductLifecycles>();
}
