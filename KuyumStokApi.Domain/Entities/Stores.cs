using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Stores
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Branches> Branches { get; set; } = new List<Branches>();
}
