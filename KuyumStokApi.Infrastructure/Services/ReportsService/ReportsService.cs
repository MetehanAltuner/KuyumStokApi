using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Reports;
using KuyumStokApi.Application.Interfaces.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.ReportsService
{
    public sealed class ReportsService : IReportsService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUserContext _currentUser;
        private readonly ILogger<ReportsService> _logger;

        private static readonly string[] OwnerRoleHints = { "owner", "admin" };
        private static readonly string[] ManagerRoleHints = { "manager" };

        public ReportsService(AppDbContext db, ICurrentUserContext currentUser, ILogger<ReportsService> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<ApiResult<StoreOverviewReportDto>> GetStoreOverviewAsync(ReportDateRange range, CancellationToken ct = default)
        {
            var scope = await ResolveScopeAsync(ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<StoreOverviewReportDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizeRange(range);

            var saleLines = BuildSaleLineQuery(fromUtc, toUtc, scope.AccessibleBranchIds);
            var purchaseLines = BuildPurchaseLineQuery(fromUtc, toUtc, scope.AccessibleBranchIds);
            var salesQuery = BuildSalesQuery(fromUtc, toUtc, scope.AccessibleBranchIds);

            var totalRevenue = await saleLines.SumAsync(x => x.Revenue, ct);
            var totalQuantity = await saleLines.SumAsync(x => x.Quantity, ct);
            var totalSalesCount = await salesQuery.CountAsync(ct);
            var uniqueCustomerCount = await salesQuery.Select(x => x.CustomerId).Where(x => x != null).Distinct().CountAsync(ct);
            var totalCost = await purchaseLines.SumAsync(x => x.Cost, ct);
            var overview = new StoreOverviewReportDto
            {
                RangeStartUtc = fromUtc,
                RangeEndUtc = toUtc,
                TotalRevenue = totalRevenue,
                TotalCost = totalCost,
                TotalProfit = totalRevenue - totalCost,
                TotalQuantitySold = totalQuantity,
                TotalSalesCount = totalSalesCount,
                UniqueCustomerCount = uniqueCustomerCount,
                RevenueByBranch = await saleLines
                    .GroupBy(x => new { x.BranchId, x.BranchName })
                    .Select(g => new MetricItemDto
                    {
                        Label = g.Key.BranchName ?? $"Şube #{g.Key.BranchId}",
                        Value = g.Sum(x => x.Revenue)
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .ToListAsync(ct),
                TopSellingProducts = await saleLines
                    .GroupBy(x => x.ProductKey)
                    .Select(g => new MetricItemDto
                    {
                        Label = g.Key ?? "Tanımsız Ürün",
                        Value = g.Sum(x => x.Revenue)
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .ToListAsync(ct),
                TopUsersByRevenue = await saleLines
                    .Where(x => x.UserId != null)
                    .GroupBy(x => new { x.UserId, x.UserFullName })
                    .Select(g => new MetricItemDto
                    {
                        Label = g.Key.UserFullName ?? $"Kullanıcı #{g.Key.UserId}",
                        Value = g.Sum(x => x.Revenue)
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .ToListAsync(ct),
                RevenueTrend = await BuildTrendAsync(fromUtc, toUtc, scope.AccessibleBranchIds, ReportTrendGranularity.Daily, ct)
            };

            return ApiResult<StoreOverviewReportDto>.Ok(overview, "Mağaza raporu hazırlandı", 200);
        }

        public async Task<ApiResult<BranchOverviewReportDto>> GetBranchOverviewAsync(int? branchId, ReportDateRange range, CancellationToken ct = default)
        {
            var scope = await ResolveScopeAsync(ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<BranchOverviewReportDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var targetBranchId = branchId ?? scope.BranchId ?? scope.AccessibleBranchIds.First();
            if (!scope.AccessibleBranchIds.Contains(targetBranchId))
                return ApiResult<BranchOverviewReportDto>.Fail("Bu şube için rapor görüntüleme yetkiniz yok.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizeRange(range);
            var branchIds = new[] { targetBranchId };

            var saleLines = BuildSaleLineQuery(fromUtc, toUtc, branchIds);
            var purchaseLines = BuildPurchaseLineQuery(fromUtc, toUtc, branchIds);
            var salesQuery = BuildSalesQuery(fromUtc, toUtc, branchIds);

            var totalRevenue = await saleLines.SumAsync(x => x.Revenue, ct);
            var totalQuantity = await saleLines.SumAsync(x => x.Quantity, ct);
            var totalSalesCount = await salesQuery.CountAsync(ct);
            var uniqueCustomerCount = await salesQuery.Select(x => x.CustomerId).Where(x => x != null).Distinct().CountAsync(ct);
            var totalCost = await purchaseLines.SumAsync(x => x.Cost, ct);
            var branchName = await _db.Branches.AsNoTracking().Where(b => b.Id == targetBranchId).Select(b => b.Name).FirstOrDefaultAsync(ct);
            var stockQuery = _db.Stocks.AsNoTracking().Where(s => s.BranchId == targetBranchId);
            var activeStockCount = await stockQuery.CountAsync(ct);
            var totalStockQuantity = await stockQuery.SumAsync(s => s.Quantity ?? 0, ct);

            var report = new BranchOverviewReportDto
            {
                BranchId = targetBranchId,
                BranchName = branchName,
                RangeStartUtc = fromUtc,
                RangeEndUtc = toUtc,
                TotalRevenue = totalRevenue,
                TotalCost = totalCost,
                TotalProfit = totalRevenue - totalCost,
                TotalQuantitySold = totalQuantity,
                TotalSalesCount = totalSalesCount,
                UniqueCustomerCount = uniqueCustomerCount,
                ActiveStockCount = activeStockCount,
                TotalStockQuantity = totalStockQuantity,
                TopUsers = await saleLines
                    .Where(x => x.UserId != null)
                    .GroupBy(x => new { x.UserId, x.UserFullName })
                    .Select(g => new MetricItemDto
                    {
                        Label = g.Key.UserFullName ?? $"Kullanıcı #{g.Key.UserId}",
                        Value = g.Sum(x => x.Revenue)
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .ToListAsync(ct),
                TopProducts = await saleLines
                    .GroupBy(x => x.ProductKey)
                    .Select(g => new MetricItemDto
                    {
                        Label = g.Key ?? "Tanımsız Ürün",
                        Value = g.Sum(x => x.Revenue)
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .ToListAsync(ct),
                RevenueTrend = await BuildTrendAsync(fromUtc, toUtc, branchIds, ReportTrendGranularity.Daily, ct)
            };

            return ApiResult<BranchOverviewReportDto>.Ok(report, "Şube raporu hazırlandı", 200);
        }

        public async Task<ApiResult<UserPerformanceReportDto>> GetUserPerformanceAsync(int? userId, ReportDateRange range, CancellationToken ct = default)
        {
            var scope = await ResolveScopeAsync(ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<UserPerformanceReportDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var targetUserId = userId ?? scope.UserId;

            var userInfo = await _db.Users.AsNoTracking()
                .Include(u => u.Branch)
                .Where(u => u.Id == targetUserId)
                .Select(u => new { u.Id, u.Username, u.FirstName, u.LastName, u.BranchId, BranchName = u.Branch != null ? u.Branch.Name : null })
                .FirstOrDefaultAsync(ct);

            if (userInfo == null)
                return ApiResult<UserPerformanceReportDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

            if (userId.HasValue && !IsOwnerRole(scope.RoleName) && scope.UserId != userId)
                return ApiResult<UserPerformanceReportDto>.Fail("Bu kullanıcı için rapor görüntüleme yetkiniz yok.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizeRange(range);

            var saleLines = BuildSaleLineQuery(fromUtc, toUtc, scope.AccessibleBranchIds)
                .Where(x => x.UserId == userInfo.Id);
            var salesQuery = BuildSalesQuery(fromUtc, toUtc, scope.AccessibleBranchIds)
                .Where(x => x.UserId == userInfo.Id);

            var totalRevenue = await saleLines.SumAsync(x => x.Revenue, ct);
            var totalQuantity = await saleLines.SumAsync(x => x.Quantity, ct);
            var totalSalesCount = await salesQuery.CountAsync(ct);
            var uniqueCustomerCount = await salesQuery.Select(x => x.CustomerId).Where(x => x != null).Distinct().CountAsync(ct);

            var report = new UserPerformanceReportDto
            {
                UserId = userInfo.Id,
                UserName = string.IsNullOrWhiteSpace(userInfo.FirstName) && string.IsNullOrWhiteSpace(userInfo.LastName)
                    ? userInfo.Username
                    : $"{userInfo.FirstName} {userInfo.LastName}".Trim(),
                BranchName = userInfo.BranchName,
                RangeStartUtc = fromUtc,
                RangeEndUtc = toUtc,
                TotalRevenue = totalRevenue,
                TotalCost = 0m,
                TotalProfit = totalRevenue,
                TotalQuantitySold = totalQuantity,
                TotalSalesCount = totalSalesCount,
                UniqueCustomerCount = uniqueCustomerCount,
                SalesByBranch = await saleLines
                    .GroupBy(x => new { x.BranchId, x.BranchName })
                    .Select(g => new MetricItemDto
                    {
                        Label = g.Key.BranchName ?? $"Şube #{g.Key.BranchId}",
                        Value = g.Sum(x => x.Revenue)
                    })
                    .OrderByDescending(x => x.Value)
                    .ToListAsync(ct),
                TopProducts = await saleLines
                    .GroupBy(x => x.ProductKey)
                    .Select(g => new MetricItemDto
                    {
                        Label = g.Key ?? "Tanımsız Ürün",
                        Value = g.Sum(x => x.Revenue)
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(10)
                    .ToListAsync(ct),
                RevenueTrend = await BuildTrendAsync(fromUtc, toUtc, scope.AccessibleBranchIds, ReportTrendGranularity.Daily, ct, userInfo.Id)
            };

            return ApiResult<UserPerformanceReportDto>.Ok(report, "Kullanıcı performans raporu hazırlandı", 200);
        }

        public async Task<ApiResult<SalesTrendReportDto>> GetSalesTrendAsync(ReportTrendGranularity granularity, ReportDateRange range, CancellationToken ct = default)
        {
            var scope = await ResolveScopeAsync(ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<SalesTrendReportDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizeRange(range);

            var trend = await BuildTrendAsync(fromUtc, toUtc, scope.AccessibleBranchIds, granularity, ct);

            var dto = new SalesTrendReportDto
            {
                RangeStartUtc = fromUtc,
                RangeEndUtc = toUtc,
                Granularity = granularity,
                Trend = trend
            };

            return ApiResult<SalesTrendReportDto>.Ok(dto, "Satış trend raporu hazırlandı", 200);
        }

        #region Helpers

        private IQueryable<SaleLineProjection> BuildSaleLineQuery(DateTime fromUtc, DateTime toUtc, IReadOnlyCollection<int> branchIds)
        {
            return
                from d in _db.SaleDetails.AsNoTracking()
                join s in _db.Sales.AsNoTracking() on d.SaleId equals s.Id
                where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                let created = s.CreatedAt ?? s.UpdatedAt ?? fromUtc
                where created >= fromUtc && created <= toUtc
                join b in _db.Branches.AsNoTracking() on s.BranchId equals b.Id
                join u in _db.Users.AsNoTracking() on s.UserId equals u.Id into ju
                from u in ju.DefaultIfEmpty()
                join st in _db.Stocks.AsNoTracking() on d.StockId equals st.Id into js
                from st in js.DefaultIfEmpty()
                join pv in _db.ProductVariants.AsNoTracking() on st.ProductVariantId equals pv.Id into jpv
                from pv in jpv.DefaultIfEmpty()
                select new SaleLineProjection
                {
                    BranchId = b.Id,
                    BranchName = b.Name,
                    UserId = u != null ? (int?)u.Id : null,
                    UserFullName = u != null
                        ? ((u.FirstName ?? u.Username) + (u.LastName != null ? " " + u.LastName : ""))
                        : null,
                    CustomerId = s.CustomerId,
                    Quantity = d.Quantity ?? 0,
                    Revenue = (d.Quantity ?? 0) * (d.SoldPrice ?? 0m),
                    CreatedAtUtc = created,
                    ProductKey = pv != null && pv.Name != null
                        ? pv.Name
                        : st != null
                            ? st.Barcode
                            : null,
                    ProductVariantId = st != null ? st.ProductVariantId : null
                };
        }

        private IQueryable<PurchaseLineProjection> BuildPurchaseLineQuery(DateTime fromUtc, DateTime toUtc, IReadOnlyCollection<int> branchIds)
        {
            return
                from d in _db.PurchaseDetails.AsNoTracking()
                join p in _db.Purchases.AsNoTracking() on d.PurchaseId equals p.Id
                where p.BranchId != null && branchIds.Contains(p.BranchId.Value)
                let created = p.CreatedAt ?? p.UpdatedAt ?? fromUtc
                where created >= fromUtc && created <= toUtc
                select new PurchaseLineProjection
                {
                    BranchId = p.BranchId!.Value,
                    Cost = (d.Quantity ?? 0) * (d.PurchasePrice ?? 0m),
                    CreatedAtUtc = created
                };
        }

        private IQueryable<SaleHeaderProjection> BuildSalesQuery(DateTime fromUtc, DateTime toUtc, IReadOnlyCollection<int> branchIds)
        {
            return
                from s in _db.Sales.AsNoTracking()
                where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                let created = s.CreatedAt ?? s.UpdatedAt ?? fromUtc
                where created >= fromUtc && created <= toUtc
                select new SaleHeaderProjection
                {
                    SaleId = s.Id,
                    BranchId = s.BranchId!.Value,
                    CustomerId = s.CustomerId,
                    UserId = s.UserId
                };
        }

        private async Task<List<TrendPointDto>> BuildTrendAsync(
            DateTime fromUtc,
            DateTime toUtc,
            IReadOnlyCollection<int> branchIds,
            ReportTrendGranularity granularity,
            CancellationToken ct,
            int? userId = null)
        {
            var salesBuckets = await
                (from d in _db.SaleDetails.AsNoTracking()
                 join s in _db.Sales.AsNoTracking() on d.SaleId equals s.Id
                 where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                 let created = s.CreatedAt ?? s.UpdatedAt ?? fromUtc
                 where created >= fromUtc && created <= toUtc
                 where userId == null || s.UserId == userId
                 let bucket = new DateTime(created.Year, created.Month, created.Day, 0, 0, 0, DateTimeKind.Utc)
                 group d by bucket into g
                 select new
                 {
                     Bucket = g.Key,
                     Revenue = g.Sum(x => (x.Quantity ?? 0) * (x.SoldPrice ?? 0m))
                 }).ToListAsync(ct);

            var purchaseBuckets = await
                (from d in _db.PurchaseDetails.AsNoTracking()
                 join p in _db.Purchases.AsNoTracking() on d.PurchaseId equals p.Id
                 where p.BranchId != null && branchIds.Contains(p.BranchId.Value)
                 let created = p.CreatedAt ?? p.UpdatedAt ?? fromUtc
                 where created >= fromUtc && created <= toUtc
                 let bucket = new DateTime(created.Year, created.Month, created.Day, 0, 0, 0, DateTimeKind.Utc)
                 group d by bucket into g
                 select new
                 {
                     Bucket = g.Key,
                     Cost = g.Sum(x => (x.Quantity ?? 0) * (x.PurchasePrice ?? 0m))
                 }).ToListAsync(ct);

            var map = new Dictionary<DateTime, TrendPointDto>();

            foreach (var item in salesBuckets)
            {
                var key = item.Bucket;
                if (!map.TryGetValue(key, out var entry))
                {
                    entry = new TrendPointDto { BucketStartUtc = key };
                    map[key] = entry;
                }
                entry.Revenue = item.Revenue;
            }

            foreach (var item in purchaseBuckets)
            {
                var key = item.Bucket;
                if (!map.TryGetValue(key, out var entry))
                {
                    entry = new TrendPointDto { BucketStartUtc = key };
                    map[key] = entry;
                }
                entry.Cost = item.Cost;
            }

            foreach (var kvp in map.Values)
            {
                kvp.Profit = kvp.Revenue - kvp.Cost;
            }

            var dailyPoints = map.Values.OrderBy(x => x.BucketStartUtc).ToList();

            return granularity switch
            {
                ReportTrendGranularity.Weekly => AggregateTrend(dailyPoints, ReportTrendGranularity.Weekly),
                ReportTrendGranularity.Monthly => AggregateTrend(dailyPoints, ReportTrendGranularity.Monthly),
                _ => dailyPoints
            };
        }

        private async Task<ReportScope> ResolveScopeAsync(CancellationToken ct)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
                throw new InvalidOperationException("Kullanıcı kimliği doğrulanamadı.");

            var user = await _db.Users.AsNoTracking()
                .Include(u => u.Branch)
                .ThenInclude(b => b.Store)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, ct);

            if (user == null)
                throw new InvalidOperationException("Kullanıcı kaydı bulunamadı.");

            var roleName = user.Role?.Name?.ToLowerInvariant();
            var branchIds = new List<int>();

            if (IsOwnerRole(roleName))
            {
                if (user.Branch?.StoreId != null)
                {
                    branchIds = await _db.Branches.AsNoTracking()
                        .Where(b => b.StoreId == user.Branch.StoreId && !b.IsDeleted)
                        .Select(b => b.Id)
                        .ToListAsync(ct);
                }
                else
                {
                    branchIds = await _db.Branches.AsNoTracking()
                        .Where(b => !b.IsDeleted)
                        .Select(b => b.Id)
                        .ToListAsync(ct);
                }
            }
            else if (IsManagerRole(roleName) && user.BranchId != null)
            {
                branchIds.Add(user.BranchId.Value);
            }
            else if (user.BranchId != null)
            {
                branchIds.Add(user.BranchId.Value);
            }

            return new ReportScope
            {
                UserId = user.Id,
                BranchId = user.BranchId,
                StoreId = user.Branch?.StoreId,
                RoleName = roleName,
                AccessibleBranchIds = branchIds.Distinct().ToList()
            };
        }

        private static (DateTime fromUtc, DateTime toUtc) NormalizeRange(ReportDateRange range)
        {
            var toUtc = range.ToUtc ?? DateTime.UtcNow;
            var fromUtc = range.FromUtc ?? toUtc.AddDays(-30);
            if (fromUtc > toUtc)
            {
                (fromUtc, toUtc) = (toUtc, fromUtc);
            }
            return (fromUtc, toUtc);
        }

        private static bool IsOwnerRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return false;
            return OwnerRoleHints.Any(h => roleName.Contains(h, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsManagerRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return false;
            return ManagerRoleHints.Any(h => roleName.Contains(h, StringComparison.OrdinalIgnoreCase));
        }

        private sealed class ReportScope
        {
            public int UserId { get; set; }
            public int? BranchId { get; set; }
            public int? StoreId { get; set; }
            public string? RoleName { get; set; }
            public List<int> AccessibleBranchIds { get; set; } = new();
        }

        private sealed class SaleLineProjection
        {
            public int BranchId { get; set; }
            public string? BranchName { get; set; }
            public int? UserId { get; set; }
            public string? UserFullName { get; set; }
            public int? CustomerId { get; set; }
            public int Quantity { get; set; }
            public decimal Revenue { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public string? ProductKey { get; set; }
            public int? ProductVariantId { get; set; }
        }

        private sealed class PurchaseLineProjection
        {
            public int BranchId { get; set; }
            public decimal Cost { get; set; }
            public DateTime CreatedAtUtc { get; set; }
        }

        private sealed class SaleHeaderProjection
        {
            public int SaleId { get; set; }
            public int BranchId { get; set; }
            public int? CustomerId { get; set; }
            public int? UserId { get; set; }
        }

        private static List<TrendPointDto> AggregateTrend(List<TrendPointDto> dailyPoints, ReportTrendGranularity granularity)
        {
            return dailyPoints
                .GroupBy(p => GetBucketStart(p.BucketStartUtc, granularity))
                .Select(g => new TrendPointDto
                {
                    BucketStartUtc = g.Key,
                    Revenue = g.Sum(x => x.Revenue),
                    Cost = g.Sum(x => x.Cost),
                    Profit = g.Sum(x => x.Profit)
                })
                .OrderBy(x => x.BucketStartUtc)
                .ToList();
        }

        private static DateTime GetBucketStart(DateTime dateUtc, ReportTrendGranularity granularity)
        {
            var utc = dateUtc.Kind == DateTimeKind.Utc ? dateUtc : DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc);
            utc = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);

            return granularity switch
            {
                ReportTrendGranularity.Weekly => utc.AddDays(-((int)utc.DayOfWeek + 6) % 7),
                ReportTrendGranularity.Monthly => new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                _ => utc
            };
        }

        #endregion
    }
}

