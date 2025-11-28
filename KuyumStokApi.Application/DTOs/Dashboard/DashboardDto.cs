using System;
using System.Collections.Generic;

namespace KuyumStokApi.Application.DTOs.Dashboard
{
    /// <summary>
    /// Gerçek zamanlı canlı sayaçlar için DTO
    /// </summary>
    public sealed class LiveCountersDto
    {
        public int MinutesSinceLastSale { get; set; }
        public int TodayTransactionCount { get; set; }
        public DateTime? LastStockSyncTime { get; set; }
        public string LastStockSyncFormatted { get; set; } = string.Empty;
    }

    /// <summary>
    /// Gün sonu raporu için DTO
    /// </summary>
    public sealed class DailySummaryDto
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitPercentage { get; set; }
        public string TopSellingProduct { get; set; } = string.Empty;
        public int CriticalStockCount { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Anomali algılama için DTO
    /// </summary>
    public sealed class AnomalyDto
    {
        public string Type { get; set; } = string.Empty; // "HighSalesDrop", "LowStockLevel", "NormalSales"
        public string Description { get; set; } = string.Empty;
        public int RiskScore { get; set; } // 0-100
    }

    /// <summary>
    /// Aylık satış hedefi için DTO
    /// </summary>
    public sealed class MonthlyTargetDto
    {
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal ProgressPercentage { get; set; }
        public decimal RemainingAmount { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Hatırlatıcı ve ajanda için DTO
    /// </summary>
    public sealed class ReminderDto
    {
        public string Type { get; set; } = string.Empty; // "CriticalStock", "UnsoldProduct", "StockDepletion"
        public string Message { get; set; } = string.Empty;
        public int Priority { get; set; } // 1-5
    }

    /// <summary>
    /// En çok satan ürünler için DTO
    /// </summary>
    public sealed class TopProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Günlük iş yükü tahmini için DTO
    /// </summary>
    public sealed class WorkloadEstimateDto
    {
        public int EstimatedWorkloadPercentage { get; set; } // 0-100
        public string IntensityLevel { get; set; } = string.Empty; // "Düşük", "Orta", "Yüksek"
        public int EstimatedTransactionCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Şube karşılaştırması için DTO
    /// </summary>
    public sealed class BranchComparisonDto
    {
        public List<BranchComparisonItemDto> Branches { get; set; } = new();
    }

    /// <summary>
    /// Şube karşılaştırması item DTO
    /// </summary>
    public sealed class BranchComparisonItemDto
    {
        public string BranchName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalProfit { get; set; }
        public int ReceiptCount { get; set; }
        public decimal PosPercentage { get; set; } // POS ödemelerinin toplam ödemeye oranı
        public int CriticalStockCount { get; set; }
        public string Trend { get; set; } = string.Empty; // "up", "down", "stable" (son 7 gün karşılaştırması)
    }

    /// <summary>
    /// Kar-Zarar tablosu için DTO
    /// </summary>
    public sealed class ProfitLossDto
    {
        public List<ProfitLossItemDto> Items { get; set; } = new();
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalProfitPercentage { get; set; }
    }

    /// <summary>
    /// Kar-Zarar tablosu item DTO
    /// </summary>
    public sealed class ProfitLossItemDto
    {
        public DateTime Period { get; set; }
        public string PeriodLabel { get; set; } = string.Empty; // "25 Eylül 2024", "Hafta 1", "Ocak 2024"
        public decimal Sales { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercentage { get; set; }
        public string Trend { get; set; } = string.Empty; // "up", "down", "stable"
    }
}

