using System;
using System.Collections.Generic;
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
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Query { get; set; }
    }
}
