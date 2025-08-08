using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class PaymentMethods
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Purchases> Purchases { get; set; } = new List<Purchases>();

    public virtual ICollection<Sales> Sales { get; set; } = new List<Sales>();
}
