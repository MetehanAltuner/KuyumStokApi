using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class ProductVariants
{
    public int Id { get; set; }

    public int? ProductTypeId { get; set; }

    public string? Ayar { get; set; }

    public string? Brand { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public bool IsActive { get; set; }

    public string Name { get; set; } = null!;

    public string? Color { get; set; }

    public bool IsFavorite { get; set; }

    public virtual ICollection<Limits> Limits { get; set; } = new List<Limits>();

    public virtual ProductTypes? ProductType { get; set; }

    public virtual ICollection<Stocks> Stocks { get; set; } = new List<Stocks>();
}
