using System;
using System.Collections.Generic;

namespace KuyumStokApi.Application.DTOs.Reports
{
    public sealed class PersonnelPerformanceRowDto
    {
        public int UserId { get; set; }
        public string PersonnelName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        public int CancelCount { get; set; }
        public int PerformanceScorePercent { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed record PersonnelPerformanceQueryDto(
        DateTime? From = null,
        DateTime? To = null,
        int Page = 1,
        int PageSize = 10,
        string? SortBy = null,
        string? SortDir = null,
        int? BranchId = null
    );

    public sealed class PagedResponseDto<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
