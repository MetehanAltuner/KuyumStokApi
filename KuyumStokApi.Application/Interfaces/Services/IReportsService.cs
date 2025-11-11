using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Reports;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IReportsService
    {
        Task<ApiResult<StoreOverviewReportDto>> GetStoreOverviewAsync(ReportDateRange range, CancellationToken ct = default);
        Task<ApiResult<BranchOverviewReportDto>> GetBranchOverviewAsync(int? branchId, ReportDateRange range, CancellationToken ct = default);
        Task<ApiResult<UserPerformanceReportDto>> GetUserPerformanceAsync(int? userId, ReportDateRange range, CancellationToken ct = default);
        Task<ApiResult<SalesTrendReportDto>> GetSalesTrendAsync(ReportTrendGranularity granularity, ReportDateRange range, CancellationToken ct = default);
    }
}

