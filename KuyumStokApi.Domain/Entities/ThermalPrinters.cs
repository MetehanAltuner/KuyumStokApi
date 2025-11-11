using System;

namespace KuyumStokApi.Domain.Entities;

public partial class ThermalPrinters
{
    public int Id { get; set; }

    public int? BranchId { get; set; }

    public string Name { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public int Port { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual Branches? Branch { get; set; }
}


