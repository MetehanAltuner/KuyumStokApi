using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Banks
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BankTransactions> BankTransactions { get; set; } = new List<BankTransactions>();
}
