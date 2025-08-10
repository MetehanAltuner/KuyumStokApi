using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class PaymentMethods
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Purchases> Purchases { get; set; } = new List<Purchases>();

    public virtual ICollection<Sales> Sales { get; set; } = new List<Sales>();
}
