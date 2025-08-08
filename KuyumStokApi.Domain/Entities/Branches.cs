using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Branches
{
    public int Id { get; set; }

    public int? StoreId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Limits> Limits { get; set; } = new List<Limits>();

    public virtual ICollection<Purchases> Purchases { get; set; } = new List<Purchases>();

    public virtual ICollection<Sales> Sales { get; set; } = new List<Sales>();

    public virtual ICollection<Stocks> Stocks { get; set; } = new List<Stocks>();

    public virtual Stores? Store { get; set; }

    public virtual ICollection<Users> Users { get; set; } = new List<Users>();
}
