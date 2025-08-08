using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Purchases
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? BranchId { get; set; }

    public int? CustomerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? PaymentMethodId { get; set; }

    public virtual Branches? Branch { get; set; }

    public virtual Customers? Customer { get; set; }

    public virtual PaymentMethods? PaymentMethod { get; set; }

    public virtual ICollection<PurchaseDetails> PurchaseDetails { get; set; } = new List<PurchaseDetails>();

    public virtual Users? User { get; set; }
}
