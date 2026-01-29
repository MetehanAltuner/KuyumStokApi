using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.LifeCycleActions
{
    public sealed class LifecycleActionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public sealed class LifecycleActionCreateDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public sealed class LifecycleActionUpdateDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public sealed class LifecycleActionFilter
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        public int Page { get; set; } = 1;
        [Range(1, int.MaxValue, ErrorMessage = "PageSize must be greater than 0.")]
        public int PageSize { get; set; } = 20;
        public string? Query { get; set; }
    }
}
