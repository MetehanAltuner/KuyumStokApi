using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class BankTransactions
{
    public int Id { get; set; }

    public int? SaleId { get; set; }

    public int? BankId { get; set; }

    public decimal? CommissionRate { get; set; }

    public decimal? ExpectedAmount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Banks? Bank { get; set; }

    public virtual Sales? Sale { get; set; }
}
