using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.ProductCategory
{
    public sealed class ProductCategoryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    public sealed class ProductCategoryCreateDto
    {
        public string Name { get; set; } = null!;
    }

    public sealed class ProductCategoryUpdateDto
    {
        public string Name { get; set; } = null!;
    }

    // filtre/paging
    public sealed record ProductCategoryFilter(
        int Page = 1,
        int PageSize = 20,
        string? Query = null,
        bool? IsActive = null,
        bool IncludeDeleted = false,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );
}
