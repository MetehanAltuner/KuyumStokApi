using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KuyumStokApi.Application.Validation.Attributes;
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
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int? BranchId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId must be greater than 0.")]
        public int? ProductVariantId { get; set; }
        [GreaterThanZero(ErrorMessage = "MinThreshold must be greater than 0.")]
        public decimal? MinThreshold { get; set; }
        [GreaterThanZero(ErrorMessage = "MaxThreshold must be greater than 0.")]
        public decimal? MaxThreshold { get; set; }
    }

    public sealed class LimitUpdateDto
    {
        [GreaterThanZero(ErrorMessage = "MinThreshold must be greater than 0.")]
        public decimal? MinThreshold { get; set; }
        [GreaterThanZero(ErrorMessage = "MaxThreshold must be greater than 0.")]
        public decimal? MaxThreshold { get; set; }
    }

    // İstersen filtreli/paged liste için (opsiyonel ama faydalı)
    public sealed class LimitFilter
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        public int Page { get; set; } = 1;
        [Range(1, int.MaxValue, ErrorMessage = "PageSize must be greater than 0.")]
        public int PageSize { get; set; } = 20;

        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int? BranchId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId must be greater than 0.")]
        public int? ProductVariantId { get; set; }
    }
}
