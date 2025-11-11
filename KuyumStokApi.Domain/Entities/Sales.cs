using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Sales
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? BranchId { get; set; }

    public int? CustomerId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? PaymentMethodId { get; set; }

    public virtual ICollection<BankTransactions> BankTransactions { get; set; } = new List<BankTransactions>();

    public virtual Branches? Branch { get; set; }

    public virtual Customers? Customer { get; set; }

    public virtual PaymentMethods? PaymentMethod { get; set; }

    public virtual ICollection<SaleDetails> SaleDetails { get; set; } = new List<SaleDetails>();

    public virtual ICollection<SalePayments> SalePayments { get; set; } = new List<SalePayments>();

    public virtual Users? User { get; set; }
}
