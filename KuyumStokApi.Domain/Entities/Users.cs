using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class Users
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public int? RoleId { get; set; }

    public int? BranchId { get; set; }

    public string PasswordSalt { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branches? Branch { get; set; }

    public virtual ICollection<ProductLifecycles> ProductLifecycles { get; set; } = new List<ProductLifecycles>();

    public virtual ICollection<Purchases> Purchases { get; set; } = new List<Purchases>();

    public virtual Roles? Role { get; set; }

    public virtual ICollection<Sales> Sales { get; set; } = new List<Sales>();
}
