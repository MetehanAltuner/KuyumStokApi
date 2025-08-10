using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Banks
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<BankTransactions> BankTransactions { get; set; } = new List<BankTransactions>();
}
