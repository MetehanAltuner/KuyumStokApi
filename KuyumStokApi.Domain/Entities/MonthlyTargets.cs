using System;

namespace KuyumStokApi.Domain.Entities;

public partial class MonthlyTargets
{
    public int Id { get; set; }

    public int? StoreId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; } // 1-12

    public decimal TargetAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual Stores? Store { get; set; }
}

