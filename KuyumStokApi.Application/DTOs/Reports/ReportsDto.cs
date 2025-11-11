using System;
using System.Collections.Generic;

namespace KuyumStokApi.Application.DTOs.Reports
{
    public sealed record ReportDateRange(DateTime? FromUtc = null, DateTime? ToUtc = null);

    public enum ReportTrendGranularity
    {
        Daily,
        Weekly,
        Monthly
    }

    public sealed class MetricItemDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public sealed class TrendPointDto
    {
        public DateTime BucketStartUtc { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
    }

    public sealed class StoreOverviewReportDto
    {
        public DateTime RangeStartUtc { get; set; }
        public DateTime RangeEndUtc { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalSalesCount { get; set; }
        public int TotalQuantitySold { get; set; }
        public int UniqueCustomerCount { get; set; }
        public List<MetricItemDto> RevenueByBranch { get; set; } = new();
        public List<MetricItemDto> TopSellingProducts { get; set; } = new();
        public List<MetricItemDto> TopUsersByRevenue { get; set; } = new();
        public List<TrendPointDto> RevenueTrend { get; set; } = new();
    }

    public sealed class BranchOverviewReportDto
    {
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public DateTime RangeStartUtc { get; set; }
        public DateTime RangeEndUtc { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalSalesCount { get; set; }
        public int TotalQuantitySold { get; set; }
        public int UniqueCustomerCount { get; set; }
        public int ActiveStockCount { get; set; }
        public int TotalStockQuantity { get; set; }
        public List<MetricItemDto> TopUsers { get; set; } = new();
        public List<MetricItemDto> TopProducts { get; set; } = new();
        public List<TrendPointDto> RevenueTrend { get; set; } = new();
    }

    public sealed class UserPerformanceReportDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? BranchName { get; set; }
        public DateTime RangeStartUtc { get; set; }
        public DateTime RangeEndUtc { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalSalesCount { get; set; }
        public int TotalQuantitySold { get; set; }
        public int UniqueCustomerCount { get; set; }
        public List<MetricItemDto> SalesByBranch { get; set; } = new();
        public List<MetricItemDto> TopProducts { get; set; } = new();
        public List<TrendPointDto> RevenueTrend { get; set; } = new();
    }

    public sealed class SalesTrendReportDto
    {
        public DateTime RangeStartUtc { get; set; }
        public DateTime RangeEndUtc { get; set; }
        public ReportTrendGranularity Granularity { get; set; }
        public List<TrendPointDto> Trend { get; set; } = new();
    }
}

