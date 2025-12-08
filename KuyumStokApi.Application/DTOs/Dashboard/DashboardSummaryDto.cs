using KuyumStokApi.Application.DTOs.Reports;
using System.Collections.Generic;

namespace KuyumStokApi.Application.DTOs.Dashboard
{
    /// <summary>
    /// Tüm parametresiz dashboard verilerini içeren birleşik DTO (broadcast yapılan endpoint'ler hariç)
    /// </summary>
    public sealed class DashboardSummaryDto
    {
        public SalesTrendReportDto? WeeklyTrend { get; set; }
        public MonthlyTargetDto? MonthlyTarget { get; set; }
        public List<ReminderDto> Reminders { get; set; } = new();
        public WorkloadEstimateDto? WorkloadEstimate { get; set; }
        public BranchComparisonDto? BranchComparison { get; set; }
        public RiskScoreLegendDto? RiskScoreLegend { get; set; }
    }
}

