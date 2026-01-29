using System;
using System.ComponentModel.DataAnnotations;

namespace KuyumStokApi.Application.DTOs.ThermalPrinters
{
    public sealed class ThermalPrinterDto
    {
        public int Id { get; set; }
        public int? BranchId { get; set; }
        public string Name { get; set; } = default!;
        public string IpAddress { get; set; } = default!;
        public int Port { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public BranchBrief? Branch { get; set; }

        public sealed class BranchBrief
        {
            public int? Id { get; set; }
            public string? Name { get; set; }
        }
    }

    public sealed class ThermalPrinterCreateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int BranchId { get; set; }
        public string Name { get; set; } = default!;
        public string IpAddress { get; set; } = default!;
        [Range(1, int.MaxValue, ErrorMessage = "Port must be greater than 0.")]
        public int Port { get; set; }
        public string? Description { get; set; }
    }

    public sealed class ThermalPrinterUpdateDto
    {
        public string? Name { get; set; }
        public string? IpAddress { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Port must be greater than 0.")]
        public int? Port { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public sealed record ThermalPrinterFilter(
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")] int Page = 1,
        [Range(1, int.MaxValue, ErrorMessage = "PageSize must be greater than 0.")] int PageSize = 20,
        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")] int? BranchId = null,
        bool? IsActive = null,
        bool IncludeDeleted = false,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );
}


