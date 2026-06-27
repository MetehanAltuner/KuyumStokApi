using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Dashboard;
using KuyumStokApi.Application.DTOs.Reports;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<ApiResult<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default);
        Task<ApiResult<LiveCountersDto>> GetLiveCountersAsync(CancellationToken ct = default);
        Task<ApiResult<SalesTrendReportDto>> GetWeeklyTrendAsync(CancellationToken ct = default);
        Task<ApiResult<DailySummaryDto>> GetDailySummaryAsync(DateTime? date = null, CancellationToken ct = default);
        Task<ApiResult<List<AnomalyDto>>> GetAnomaliesAsync(CancellationToken ct = default);
        Task<ApiResult<MonthlyTargetDto>> GetMonthlyTargetAsync(CancellationToken ct = default);
        Task<ApiResult<List<ReminderDto>>> GetRemindersAsync(CancellationToken ct = default);
        Task<ApiResult<List<TopProductDto>>> GetTopProductsAsync(int limit = 5, string period = "week", CancellationToken ct = default);
        Task<ApiResult<List<DailyTopSellingTrendItemDto>>> GetDailyTopSellingTrendAsync(int days, CancellationToken ct = default);
        Task<ApiResult<List<SalesPieChartCategoryItemDto>>> GetSalesPieChartAsync(int storeId, int? branchId, int days, CancellationToken ct = default);
        Task<ApiResult<WorkloadEstimateDto>> GetWorkloadEstimateAsync(CancellationToken ct = default);
        Task<ApiResult<BranchComparisonDto>> GetBranchComparisonAsync(CancellationToken ct = default);
        Task<ApiResult<ProfitLossDto>> GetProfitLossAsync(string period = "week", CancellationToken ct = default);
        Task<ApiResult<RiskScoreLegendDto>> GetRiskScoreLegendAsync(CancellationToken ct = default);
    }
}

