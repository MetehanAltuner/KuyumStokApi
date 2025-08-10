using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Stores
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Branches> Branches { get; set; } = new List<Branches>();
}
