using System;
using System.Collections.Generic;

namespace KuyumStokApi.Domain.Entities;

public partial class ProductVariants
{
    public int Id { get; set; }

    public int? ProductTypeId { get; set; }

    public decimal? Gram { get; set; }

    public decimal? Thickness { get; set; }

    public decimal? Width { get; set; }

    public string? StoneType { get; set; }

    public decimal? Carat { get; set; }

    public int? Milyem { get; set; }

    public string? Ayar { get; set; }

    public string? Brand { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Limits> Limits { get; set; } = new List<Limits>();

    public virtual ProductTypes? ProductType { get; set; }

    public virtual ICollection<Stocks> Stocks { get; set; } = new List<Stocks>();
}
