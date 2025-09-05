using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Limits
{
    public sealed class LimitDto
    {
        public int Id { get; set; }
        public int? BranchId { get; set; }
        public int? ProductVariantId { get; set; }
        public decimal? MinThreshold { get; set; }
        public decimal? MaxThreshold { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Görünüm kolaylığı (opsiyonel)
        public string? BranchName { get; set; }
        public string? VariantLabel { get; set; }
    }

    public sealed class LimitCreateDto
    {
        public int? BranchId { get; set; }
        public int? ProductVariantId { get; set; }
        public decimal? MinThreshold { get; set; }
        public decimal? MaxThreshold { get; set; }
    }

    public sealed class LimitUpdateDto
    {
        public decimal? MinThreshold { get; set; }
        public decimal? MaxThreshold { get; set; }
    }

    // İstersen filtreli/paged liste için (opsiyonel ama faydalı)
    public sealed class LimitFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public int? BranchId { get; set; }
        public int? ProductVariantId { get; set; }
    }
}
