using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Customers
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Purchases> Purchases { get; set; } = new List<Purchases>();

    public virtual ICollection<Sales> Sales { get; set; } = new List<Sales>();
}
