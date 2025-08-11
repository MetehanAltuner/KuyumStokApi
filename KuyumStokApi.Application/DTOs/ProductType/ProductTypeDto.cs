using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.ProductType
{
    public sealed class ProductTypeDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public CategoryBrief? Category { get; set; }

        public sealed class CategoryBrief
        {
            public int? Id { get; set; }
            public string? Name { get; set; }
        }
    }

    public sealed class ProductTypeCreateDto
    {
        public string Name { get; set; } = null!;
        public int? CategoryId { get; set; }
    }

    public sealed class ProductTypeUpdateDto
    {
        public string Name { get; set; } = null!;
        public int? CategoryId { get; set; }
    }

    public sealed record ProductTypeFilter(
        int Page = 1,
        int PageSize = 20,
        string? Query = null,
        int? CategoryId = null,
        bool? IsActive = null,
        bool IncludeDeleted = false,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );
}
