using System;

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
        public int BranchId { get; set; }
        public string Name { get; set; } = default!;
        public string IpAddress { get; set; } = default!;
        public int Port { get; set; }
        public string? Description { get; set; }
    }

    public sealed class ThermalPrinterUpdateDto
    {
        public string? Name { get; set; }
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public sealed record ThermalPrinterFilter(
        int Page = 1,
        int PageSize = 20,
        int? BranchId = null,
        bool? IsActive = null,
        bool IncludeDeleted = false,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );
}


