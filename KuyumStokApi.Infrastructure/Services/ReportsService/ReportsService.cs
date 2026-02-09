using ClosedXML.Excel;
using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Reports;
using KuyumStokApi.Application.Interfaces.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.ReportsService
{
    public sealed class ReportsService : IReportsService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ICurrentUserContext _currentUser;
        private readonly ILogger<ReportsService> _logger;

        private static readonly string[] OwnerRoleHints = { "owner", "admin", "developer" };
        private static readonly string[] ManagerRoleHints = { "manager" };

        public ReportsService(IDbContextFactory<AppDbContext> dbFactory, ICurrentUserContext currentUser, ILogger<ReportsService> logger)
        {
            _dbFactory = dbFactory;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<ApiResult<StoreOverviewReportDto>> GetStoreOverviewAsync(ReportDateRange range, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var scope = await ResolveScopeAsync(db, ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<StoreOverviewReportDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizeRange(range);

            var saleLines = BuildSaleLineQuery(db, fromUtc, toUtc, scope.AccessibleBranchIds);
            var purchaseLines = BuildPurchaseLineQuery(db, fromUtc, toUtc, scope.AccessibleBranchIds);
            var salesQuery = BuildSalesQuery(db, fromUtc, toUtc, scope.AccessibleBranchIds);

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
                RevenueTrend = await BuildTrendAsync(db, fromUtc, toUtc, scope.AccessibleBranchIds, ReportTrendGranularity.Daily, ct)
            };

            return ApiResult<StoreOverviewReportDto>.Ok(overview, "Mağaza raporu hazırlandı", 200);
        }

        public async Task<ApiResult<BranchOverviewReportDto>> GetBranchOverviewAsync(int? branchId, ReportDateRange range, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var scope = await ResolveScopeAsync(db, ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<BranchOverviewReportDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var targetBranchId = branchId ?? scope.BranchId ?? scope.AccessibleBranchIds.First();
            if (!scope.AccessibleBranchIds.Contains(targetBranchId))
                return ApiResult<BranchOverviewReportDto>.Fail("Bu şube için rapor görüntüleme yetkiniz yok.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizeRange(range);
            var branchIds = new[] { targetBranchId };

            var saleLines = BuildSaleLineQuery(db, fromUtc, toUtc, branchIds);
            var purchaseLines = BuildPurchaseLineQuery(db, fromUtc, toUtc, branchIds);
            var salesQuery = BuildSalesQuery(db, fromUtc, toUtc, branchIds);

            var totalRevenue = await saleLines.SumAsync(x => x.Revenue, ct);
            var totalQuantity = await saleLines.SumAsync(x => x.Quantity, ct);
            var totalSalesCount = await salesQuery.CountAsync(ct);
            var uniqueCustomerCount = await salesQuery.Select(x => x.CustomerId).Where(x => x != null).Distinct().CountAsync(ct);
            var totalCost = await purchaseLines.SumAsync(x => x.Cost, ct);
            var branchName = await db.Branches.AsNoTracking().Where(b => b.Id == targetBranchId).Select(b => b.Name).FirstOrDefaultAsync(ct);
            var stockQuery = db.Stocks.AsNoTracking().Where(s => s.BranchId == targetBranchId);
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
                RevenueTrend = await BuildTrendAsync(db, fromUtc, toUtc, branchIds, ReportTrendGranularity.Daily, ct)
            };

            return ApiResult<BranchOverviewReportDto>.Ok(report, "Şube raporu hazırlandı", 200);
        }

        public async Task<ApiResult<UserPerformanceReportDto>> GetUserPerformanceAsync(int? userId, ReportDateRange range, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var scope = await ResolveScopeAsync(db, ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<UserPerformanceReportDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var targetUserId = userId ?? scope.UserId;

            var userInfo = await db.Users.AsNoTracking()
                .Include(u => u.Branch)
                .Where(u => u.Id == targetUserId)
                .Select(u => new { u.Id, u.Username, u.FirstName, u.LastName, u.BranchId, BranchName = u.Branch != null ? u.Branch.Name : null })
                .FirstOrDefaultAsync(ct);

            if (userInfo == null)
                return ApiResult<UserPerformanceReportDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

            if (userId.HasValue && !IsOwnerRole(scope.RoleName) && scope.UserId != userId)
                return ApiResult<UserPerformanceReportDto>.Fail("Bu kullanıcı için rapor görüntüleme yetkiniz yok.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizeRange(range);

            var saleLines = BuildSaleLineQuery(db, fromUtc, toUtc, scope.AccessibleBranchIds)
                .Where(x => x.UserId == userInfo.Id);
            var salesQuery = BuildSalesQuery(db, fromUtc, toUtc, scope.AccessibleBranchIds)
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
                RevenueTrend = await BuildTrendAsync(db, fromUtc, toUtc, scope.AccessibleBranchIds, ReportTrendGranularity.Daily, ct, userInfo.Id)
            };

            return ApiResult<UserPerformanceReportDto>.Ok(report, "Kullanıcı performans raporu hazırlandı", 200);
        }

        public async Task<ApiResult<SalesTrendReportDto>> GetSalesTrendAsync(ReportTrendGranularity granularity, ReportDateRange range, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var scope = await ResolveScopeAsync(db, ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<SalesTrendReportDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizeRange(range);

            var trend = await BuildTrendAsync(db, fromUtc, toUtc, scope.AccessibleBranchIds, granularity, ct);

            var dto = new SalesTrendReportDto
            {
                RangeStartUtc = fromUtc,
                RangeEndUtc = toUtc,
                Granularity = granularity,
                Trend = trend
            };

            return ApiResult<SalesTrendReportDto>.Ok(dto, "Satış trend raporu hazırlandı", 200);
        }

        public async Task<ApiResult<PagedResponseDto<PersonnelPerformanceRowDto>>> GetPersonnelPerformanceAsync(
            PersonnelPerformanceQueryDto query,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var scope = await ResolveScopeAsync(db, ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<PagedResponseDto<PersonnelPerformanceRowDto>>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var branchIds = ResolveBranchScope(scope, query.BranchId);
            if (!branchIds.Any())
                return ApiResult<PagedResponseDto<PersonnelPerformanceRowDto>>.Fail("Bu şube için rapor görüntüleme yetkiniz yok.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizePersonnelRange(query);

            var rows = await BuildPersonnelPerformanceRowsAsync(db, branchIds, fromUtc, toUtc, ct);
            var sorted = SortPersonnelRows(rows, query.SortBy, query.SortDir);

            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var totalItems = sorted.Count;
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

            var paged = new PagedResponseDto<PersonnelPerformanceRowDto>
            {
                Items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return ApiResult<PagedResponseDto<PersonnelPerformanceRowDto>>.Ok(paged, "Personel performans raporu hazırlandı", 200);
        }

        public async Task<ApiResult<byte[]>> ExportPersonnelPerformanceXlsxAsync(
            PersonnelPerformanceQueryDto query,
            CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var scope = await ResolveScopeAsync(db, ct);
            if (!scope.AccessibleBranchIds.Any())
                return ApiResult<byte[]>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

            var branchIds = ResolveBranchScope(scope, query.BranchId);
            if (!branchIds.Any())
                return ApiResult<byte[]>.Fail("Bu şube için rapor görüntüleme yetkiniz yok.", statusCode: 403);

            var (fromUtc, toUtc) = NormalizePersonnelRange(query);

            var rows = await BuildPersonnelPerformanceRowsAsync(db, branchIds, fromUtc, toUtc, ct);
            var sorted = SortPersonnelRows(rows, query.SortBy, query.SortDir);

            var bytes = BuildPersonnelPerformanceXlsx(sorted);
            return ApiResult<byte[]>.Ok(bytes, "Personel performans dosyası hazırlandı", 200);
        }

        #region Helpers

        private static IReadOnlyCollection<int> ResolveBranchScope(ReportScope scope, int? requestedBranchId)
        {
            if (requestedBranchId.HasValue)
            {
                if (IsOwnerRole(scope.RoleName))
                {
                    if (scope.AccessibleBranchIds.Contains(requestedBranchId.Value))
                        return new[] { requestedBranchId.Value };

                    return Array.Empty<int>();
                }

                if (scope.BranchId.HasValue)
                    return new[] { scope.BranchId.Value };
            }

            return scope.AccessibleBranchIds;
        }

        private async Task<List<PersonnelPerformanceRowDto>> BuildPersonnelPerformanceRowsAsync(
            AppDbContext db,
            IReadOnlyCollection<int> branchIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken ct)
        {
            var baseUsersQuery =
                from s in db.Sales.IgnoreQueryFilters().AsNoTracking()
                where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                let created = s.CreatedAt ?? s.UpdatedAt ?? fromUtc
                where created >= fromUtc && created <= toUtc
                where s.UserId != null
                join u in db.Users.IgnoreQueryFilters().AsNoTracking() on s.UserId equals u.Id
                join r in db.Roles.IgnoreQueryFilters().AsNoTracking() on u.RoleId equals r.Id into jr
                from r in jr.DefaultIfEmpty()
                select new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    RoleName = r != null ? r.Name : null,
                    IsActive = u.IsActive ?? false
                };

            var baseUsers = await baseUsersQuery.Distinct().ToListAsync(ct);
            if (baseUsers.Count == 0)
                return new List<PersonnelPerformanceRowDto>();

            var nonCanceledQuery =
                from d in db.SaleDetails.AsNoTracking()
                join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                let created = s.CreatedAt ?? s.UpdatedAt ?? fromUtc
                where created >= fromUtc && created <= toUtc
                where s.UserId != null
                join u in db.Users.AsNoTracking() on s.UserId equals u.Id
                join r in db.Roles.AsNoTracking() on u.RoleId equals r.Id into jr
                from r in jr.DefaultIfEmpty()
                select new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    RoleName = r != null ? r.Name : null,
                    IsActive = u.IsActive ?? false,
                    SoldPrice = d.SoldPrice ?? 0m
                };

            var aggregates = await nonCanceledQuery
                .GroupBy(x => new { x.Id, x.FirstName, x.LastName, x.RoleName, x.IsActive })
                .Select(g => new
                {
                    UserId = g.Key.Id,
                    g.Key.FirstName,
                    g.Key.LastName,
                    g.Key.RoleName,
                    g.Key.IsActive,
                    TotalSales = g.Sum(x => x.SoldPrice),
                    TransactionCount = g.Count()
                })
                .ToListAsync(ct);

            var cancelCounts = await BuildCancelCountsAsync(db, branchIds, fromUtc, toUtc, ct);
            var cancelMap = cancelCounts.ToDictionary(x => x.UserId, x => x.CancelCount);
            var totalsMap = aggregates.ToDictionary(x => x.UserId, x => x);

            var rows = new List<PersonnelPerformanceRowDto>(baseUsers.Count);
            foreach (var user in baseUsers)
            {
                totalsMap.TryGetValue(user.Id, out var totals);
                cancelMap.TryGetValue(user.Id, out var cancelCount);

                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = string.Empty;

                rows.Add(new PersonnelPerformanceRowDto
                {
                    UserId = user.Id,
                    PersonnelName = fullName,
                    Department = user.RoleName,
                    TotalSales = totals?.TotalSales ?? 0m,
                    TransactionCount = totals?.TransactionCount ?? 0,
                    CancelCount = cancelCount,
                    IsActive = user.IsActive
                });
            }

            ApplyPerformanceScores(rows);
            return rows;
        }

        private async Task<List<CancelCountRow>> BuildCancelCountsAsync(
            AppDbContext db,
            IReadOnlyCollection<int> branchIds,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken ct)
        {
            var detailCanceled = BuildCancellationPredicate<SaleDetails>(db);
            var saleCanceled = BuildCancellationPredicate<Sales>(db);

            if (detailCanceled == null && saleCanceled == null)
                return new List<CancelCountRow>();

            var emptyPairs =
                db.SaleDetails.IgnoreQueryFilters()
                    .Where(d => false)
                    .Select(d => new CanceledLinePair { UserId = 0, LineId = 0 });

            IQueryable<CanceledLinePair> canceledByDetail = emptyPairs;
            if (detailCanceled != null)
            {
                canceledByDetail =
                    from d in db.SaleDetails.IgnoreQueryFilters().AsNoTracking().Where(detailCanceled)
                    join s in db.Sales.IgnoreQueryFilters().AsNoTracking() on d.SaleId equals s.Id
                    where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                    let created = s.CreatedAt ?? s.UpdatedAt ?? fromUtc
                    where created >= fromUtc && created <= toUtc
                    where s.UserId != null
                    select new CanceledLinePair
                    {
                        UserId = s.UserId!.Value,
                        LineId = d.Id
                    };
            }

            IQueryable<CanceledLinePair> canceledBySale = emptyPairs;
            if (saleCanceled != null)
            {
                canceledBySale =
                    from s in db.Sales.IgnoreQueryFilters().AsNoTracking().Where(saleCanceled)
                    join d in db.SaleDetails.IgnoreQueryFilters().AsNoTracking() on s.Id equals d.SaleId
                    where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                    let created = s.CreatedAt ?? s.UpdatedAt ?? fromUtc
                    where created >= fromUtc && created <= toUtc
                    where s.UserId != null
                    select new CanceledLinePair
                    {
                        UserId = s.UserId!.Value,
                        LineId = d.Id
                    };
            }

            var merged = canceledByDetail.Union(canceledBySale);

            return await merged
                .Distinct()
                .GroupBy(x => x.UserId)
                .Select(g => new CancelCountRow { UserId = g.Key, CancelCount = g.Count() })
                .ToListAsync(ct);
        }

        private static void ApplyPerformanceScores(List<PersonnelPerformanceRowDto> rows)
        {
            if (rows.Count == 0)
                return;

            var minSales = rows.Min(x => x.TotalSales);
            var maxSales = rows.Max(x => x.TotalSales);
            var minTx = rows.Min(x => x.TransactionCount);
            var maxTx = rows.Max(x => x.TransactionCount);

            foreach (var row in rows)
            {
                var normSales = maxSales == minSales
                    ? 1m
                    : (row.TotalSales - minSales) / (maxSales - minSales);

                var normTx = maxTx == minTx
                    ? 1m
                    : (row.TransactionCount - minTx) / (decimal)(maxTx - minTx);

                var totalOps = row.TransactionCount + row.CancelCount;
                var cancelRate = totalOps <= 0 ? 0m : row.CancelCount / (decimal)totalOps;
                var baseScore = 100m * (0.7m * normSales + 0.3m * normTx);
                var penalty = 100m * 0.5m * cancelRate;
                var score = Math.Clamp(baseScore - penalty, 0m, 100m);
                row.PerformanceScorePercent = score.ToRoundedPercentInt();
            }
        }

        private static List<PersonnelPerformanceRowDto> SortPersonnelRows(
            IEnumerable<PersonnelPerformanceRowDto> rows,
            string? sortBy,
            string? sortDir)
        {
            var list = rows.ToList();
            if (list.Count == 0)
                return list;

            var key = (sortBy ?? "totalSales").Trim().ToLowerInvariant();
            var desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

            IOrderedEnumerable<PersonnelPerformanceRowDto> ordered = key switch
            {
                "transactioncount" => desc
                    ? list.OrderByDescending(x => x.TransactionCount)
                    : list.OrderBy(x => x.TransactionCount),
                "cancelcount" => desc
                    ? list.OrderByDescending(x => x.CancelCount)
                    : list.OrderBy(x => x.CancelCount),
                "performancescore" => desc
                    ? list.OrderByDescending(x => x.PerformanceScorePercent)
                    : list.OrderBy(x => x.PerformanceScorePercent),
                "name" => desc
                    ? list.OrderByDescending(x => x.PersonnelName)
                    : list.OrderBy(x => x.PersonnelName),
                _ => desc
                    ? list.OrderByDescending(x => x.TotalSales)
                    : list.OrderBy(x => x.TotalSales)
            };

            return ordered.ToList();
        }

        private static (DateTime fromUtc, DateTime toUtc) NormalizePersonnelRange(PersonnelPerformanceQueryDto query)
        {
            var toUtc = query.To ?? DateTime.UtcNow;
            if (toUtc.Kind == DateTimeKind.Unspecified)
                toUtc = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);

            var fromUtc = query.From ?? new DateTime(toUtc.Year, toUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            if (fromUtc.Kind == DateTimeKind.Unspecified)
                fromUtc = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);

            if (fromUtc > toUtc)
                (fromUtc, toUtc) = (toUtc, fromUtc);

            return (fromUtc, toUtc);
        }

        private static Expression<Func<T, bool>>? BuildCancellationPredicate<T>(AppDbContext db) where T : class
        {
            var entityType = db.Model.FindEntityType(typeof(T));
            if (entityType == null)
                return null;

            var param = Expression.Parameter(typeof(T), "e");
            Expression? body = null;

            void Add(Expression expr)
            {
                body = body == null ? expr : Expression.OrElse(body, expr);
            }

            var isDeletedProp = entityType.FindProperty("IsDeleted");
            if (isDeletedProp != null && (isDeletedProp.ClrType == typeof(bool) || isDeletedProp.ClrType == typeof(bool?)))
            {
                var prop = Expression.Property(param, "IsDeleted");
                var constant = Expression.Constant(true, prop.Type);
                Add(Expression.Equal(prop, constant));
            }

            var deletedAtProp = entityType.FindProperty("DeletedAt");
            if (deletedAtProp != null && (deletedAtProp.ClrType == typeof(DateTime?) || deletedAtProp.ClrType == typeof(DateTime)))
            {
                var prop = Expression.Property(param, "DeletedAt");
                var constant = Expression.Constant(null, prop.Type);
                Add(Expression.NotEqual(prop, constant));
            }

            var isCanceledProp = entityType.FindProperty("IsCanceled");
            if (isCanceledProp != null && (isCanceledProp.ClrType == typeof(bool) || isCanceledProp.ClrType == typeof(bool?)))
            {
                var prop = Expression.Property(param, "IsCanceled");
                var constant = Expression.Constant(true, prop.Type);
                Add(Expression.Equal(prop, constant));
            }

            var canceledAtProp = entityType.FindProperty("CanceledAt");
            if (canceledAtProp != null && (canceledAtProp.ClrType == typeof(DateTime?) || canceledAtProp.ClrType == typeof(DateTime)))
            {
                var prop = Expression.Property(param, "CanceledAt");
                var constant = Expression.Constant(null, prop.Type);
                Add(Expression.NotEqual(prop, constant));
            }

            var statusProp = entityType.FindProperty("Status");
            if (statusProp != null && statusProp.ClrType == typeof(string))
            {
                var prop = Expression.Property(param, "Status");
                var notNull = Expression.NotEqual(prop, Expression.Constant(null, typeof(string)));
                var toLower = Expression.Call(prop, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
                var values = new[] { "canceled", "cancelled", "iptal", "iptal edildi" };

                Expression? statusExpr = null;
                foreach (var value in values)
                {
                    var eq = Expression.Equal(toLower, Expression.Constant(value));
                    statusExpr = statusExpr == null ? eq : Expression.OrElse(statusExpr, eq);
                }

                if (statusExpr != null)
                    Add(Expression.AndAlso(notNull, statusExpr));
            }

            if (body == null)
                return null;

            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        private static byte[] BuildPersonnelPerformanceXlsx(IReadOnlyList<PersonnelPerformanceRowDto> rows)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Personnel Report");

            var headers = new[]
            {
                "Personnel Name",
                "Department",
                "Total Sales",
                "Transaction Count",
                "Cancel Count",
                "Performance Score (%)",
                "Status"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var row in rows)
            {
                sheet.Cell(rowIndex, 1).Value = row.PersonnelName;
                sheet.Cell(rowIndex, 2).Value = row.Department ?? string.Empty;
                sheet.Cell(rowIndex, 3).Value = row.TotalSales;
                sheet.Cell(rowIndex, 4).Value = row.TransactionCount;
                sheet.Cell(rowIndex, 5).Value = row.CancelCount;
                sheet.Cell(rowIndex, 6).Value = row.PerformanceScorePercent;
                sheet.Cell(rowIndex, 7).Value = row.IsActive ? "Aktif" : "Pasif";
                rowIndex++;
            }

            sheet.Column(3).Style.NumberFormat.Format = "#,##0.00";
            sheet.Column(6).Style.NumberFormat.Format = "0";
            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static IQueryable<SaleLineProjection> BuildSaleLineQuery(AppDbContext db, DateTime fromUtc, DateTime toUtc, IReadOnlyCollection<int> branchIds)
        {
            return
                from d in db.SaleDetails.AsNoTracking()
                join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                let created = s.CreatedAt ?? s.UpdatedAt ?? fromUtc
                where created >= fromUtc && created <= toUtc
                join b in db.Branches.AsNoTracking() on s.BranchId equals b.Id
                join u in db.Users.AsNoTracking() on s.UserId equals u.Id into ju
                from u in ju.DefaultIfEmpty()
                join st in db.Stocks.AsNoTracking() on d.StockId equals st.Id into js
                from st in js.DefaultIfEmpty()
                join pv in db.ProductVariants.AsNoTracking() on st.ProductVariantId equals pv.Id into jpv
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

        private static IQueryable<PurchaseLineProjection> BuildPurchaseLineQuery(AppDbContext db, DateTime fromUtc, DateTime toUtc, IReadOnlyCollection<int> branchIds)
        {
            return
                from d in db.PurchaseDetails.AsNoTracking()
                join p in db.Purchases.AsNoTracking() on d.PurchaseId equals p.Id
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

        private static IQueryable<SaleHeaderProjection> BuildSalesQuery(AppDbContext db, DateTime fromUtc, DateTime toUtc, IReadOnlyCollection<int> branchIds)
        {
            return
                from s in db.Sales.AsNoTracking()
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

        private static async Task<List<TrendPointDto>> BuildTrendAsync(
            AppDbContext db,
            DateTime fromUtc,
            DateTime toUtc,
            IReadOnlyCollection<int> branchIds,
            ReportTrendGranularity granularity,
            CancellationToken ct,
            int? userId = null)
        {
            var salesBuckets = await
                (from d in db.SaleDetails.AsNoTracking()
                 join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
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
                (from d in db.PurchaseDetails.AsNoTracking()
                 join p in db.Purchases.AsNoTracking() on d.PurchaseId equals p.Id
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

        private async Task<ReportScope> ResolveScopeAsync(AppDbContext db, CancellationToken ct)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
                throw new InvalidOperationException("Kullanıcı kimliği doğrulanamadı.");

            var user = await db.Users.AsNoTracking()
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
                    branchIds = await db.Branches.AsNoTracking()
                        .Where(b => b.StoreId == user.Branch.StoreId && !b.IsDeleted)
                        .Select(b => b.Id)
                        .ToListAsync(ct);
                }
                else
                {
                    branchIds = await db.Branches.AsNoTracking()
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

        private sealed class CancelCountRow
        {
            public int UserId { get; set; }
            public int CancelCount { get; set; }
        }

        private sealed class CanceledLinePair
        {
            public int UserId { get; set; }
            public int LineId { get; set; }
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

