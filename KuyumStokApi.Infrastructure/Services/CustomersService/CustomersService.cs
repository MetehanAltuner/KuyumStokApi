using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Customers;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.CustomersService
{
    /// <summary>Müşteri operasyonları (listele, detay, ekle, güncelle, sil).</summary>
    // Not: nationalId null dönüyordu çünkü CustomersService içinde create/update ve
    // DTO dönüş/projeksiyonlarında NationalId eşlemesi yoktu. Bu dosyada eşlemeler
    // güncellendi. Hızlı doğrulama: Swagger'da POST /api/Customers ile nationalId
    // gönderip yanıtta geldiğini ve GET /api/Customers listesinde dolu olduğunu kontrol edin.
    public sealed class CustomersService : ICustomersService
    {
        private readonly AppDbContext _db;
        public CustomersService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<CustomerDto>>> GetPagedAsync(CustomerFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            var q = _db.Customers.AsNoTracking().AsQueryable();

            if (filter.OnlyActive == true)
                q = q.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(x =>
                    EF.Functions.ILike(x.Name!, $"%{qstr}%") ||
                    EF.Functions.ILike(x.Phone ?? "", $"%{qstr}%") ||
                    EF.Functions.ILike(x.Note ?? "", $"%{qstr}%"));
            }

            if (filter.UpdatedFromUtc.HasValue)
                q = q.Where(x => x.UpdatedAt == null || x.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc.HasValue)
                q = q.Where(x => x.UpdatedAt == null || x.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CustomerDto
                {
                    Id = x.Id,
                    Name = x.Name!,
                    Phone = x.Phone,
                    Note = x.Note,
                    NationalId = x.NationalId,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);

            var paged = new PagedResult<CustomerDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };

            return ApiResult<PagedResult<CustomerDto>>.Ok(paged, "Liste getirildi", 200);
        }

        public async Task<ApiResult<CustomerDetailResponseDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var customer = await _db.Customers.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(x => new CustomerDto
                {
                    Id = x.Id,
                    Name = x.Name!,
                    Phone = x.Phone,
                    Note = x.Note,
                    NationalId = x.NationalId,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync(ct);

            if (customer is null)
                return ApiResult<CustomerDetailResponseDto>.Fail("Müşteri bulunamadı", statusCode: 404);

            // Purchases header bilgileri (User bilgileri ile)
            var purchases = await (from p in _db.Purchases.AsNoTracking()
                                   join b in _db.Branches.AsNoTracking() on p.BranchId equals b.Id into jb
                                   from b in jb.DefaultIfEmpty()
                                   join pm in _db.PaymentMethods.AsNoTracking() on p.PaymentMethodId equals pm.Id into jpm
                                   from pm in jpm.DefaultIfEmpty()
                                   join u in _db.Users.AsNoTracking() on p.UserId equals u.Id into ju
                                   from u in ju.DefaultIfEmpty()
                                   where p.CustomerId == id
                                   orderby p.CreatedAt descending
                                   select new CustomerPurchaseDto
                                   {
                                       Id = p.Id,
                                       CreatedAt = p.CreatedAt,
                                       TotalAmount = 0m, // Line items'tan hesaplanacak
                                       PaymentMethodId = p.PaymentMethodId,
                                       PaymentMethodName = pm != null ? pm.Name : null,
                                       BranchId = p.BranchId,
                                       BranchName = b != null ? b.Name : null,
                                       UserId = p.UserId,
                                       UserFullName = u != null && (!string.IsNullOrEmpty(u.FirstName) || !string.IsNullOrEmpty(u.LastName))
                                           ? $"{u.FirstName ?? ""} {u.LastName ?? ""}".Trim()
                                           : null,
                                       LineItems = Array.Empty<CustomerPurchaseLineDto>()
                                   }).ToListAsync(ct);

            // Sales header bilgileri (User bilgileri ile)
            var sales = await (from s in _db.Sales.AsNoTracking()
                               join b in _db.Branches.AsNoTracking() on s.BranchId equals b.Id into jb
                               from b in jb.DefaultIfEmpty()
                               join pm in _db.PaymentMethods.AsNoTracking() on s.PaymentMethodId equals pm.Id into jpm
                               from pm in jpm.DefaultIfEmpty()
                               join u in _db.Users.AsNoTracking() on s.UserId equals u.Id into ju
                               from u in ju.DefaultIfEmpty()
                               where s.CustomerId == id
                               orderby s.CreatedAt descending
                               select new CustomerSaleDto
                               {
                                   Id = s.Id,
                                   CreatedAt = s.CreatedAt,
                                   TotalAmount = 0m, // Line items'tan hesaplanacak
                                   PaymentMethodId = s.PaymentMethodId,
                                   PaymentMethodName = pm != null ? pm.Name : null,
                                   BranchId = s.BranchId,
                                   BranchName = b != null ? b.Name : null,
                                   UserId = s.UserId,
                                   UserFullName = u != null && (!string.IsNullOrEmpty(u.FirstName) || !string.IsNullOrEmpty(u.LastName))
                                       ? $"{u.FirstName ?? ""} {u.LastName ?? ""}".Trim()
                                       : null,
                                   LineItems = Array.Empty<CustomerSaleLineDto>()
                               }).ToListAsync(ct);

            // Purchase line items (N+1 önlemek için tek query)
            var purchaseIds = purchases.Select(p => p.Id).ToList();
            var purchaseLinesData = new List<CustomerPurchaseLineItemDto>();

            if (purchaseIds.Count > 0)
            {
                purchaseLinesData = await (from d in _db.PurchaseDetails.AsNoTracking()
                                           join s in _db.Stocks.AsNoTracking() on d.StockId equals s.Id into js
                                           from s in js.DefaultIfEmpty()
                                           join pv in _db.ProductVariants.AsNoTracking() on (s != null ? s.ProductVariantId : null) equals pv.Id into jpv
                                           from pv in jpv.DefaultIfEmpty()
                                           join pt in _db.ProductTypes.AsNoTracking() on (pv != null ? pv.ProductTypeId : null) equals pt.Id into jpt
                                           from pt in jpt.DefaultIfEmpty()
                                           join pc in _db.ProductCategories.AsNoTracking() on (pt != null ? pt.CategoryId : null) equals pc.Id into jpc
                                           from pc in jpc.DefaultIfEmpty()
                                           where purchaseIds.Contains(d.PurchaseId ?? 0)
                                           select new CustomerPurchaseLineItemDto
                                           {
                                               PurchaseId = d.PurchaseId ?? 0,
                                               LineId = d.Id,
                                               StockId = d.StockId,
                                               Quantity = d.Quantity,
                                               TotalWeightGram = d.TotalWeightGram,
                                               PurchasePrice = d.PurchasePrice,
                                               ProductVariantId = pv != null ? pv.Id : (int?)null,
                                               ProductVariantName = pv != null ? pv.Name : null,
                                               ProductTypeId = pt != null ? pt.Id : (int?)null,
                                               ProductTypeName = pt != null ? pt.Name : null,
                                               CategoryName = pc != null ? pc.Name : null,
                                               Brand = pv != null ? pv.Brand : null,
                                               Ayar = pv != null ? pv.Ayar : null,
                                               Color = pv != null ? pv.Color : null
                                           }).ToListAsync(ct);
            }

            // Sale line items (N+1 önlemek için tek query)
            var saleIds = sales.Select(s => s.Id).ToList();
            var saleLinesData = new List<CustomerSaleLineItemDto>();

            if (saleIds.Count > 0)
            {
                saleLinesData = await (from d in _db.SaleDetails.AsNoTracking()
                                       join s in _db.Stocks.AsNoTracking() on d.StockId equals s.Id into js
                                       from s in js.DefaultIfEmpty()
                                       join pv in _db.ProductVariants.AsNoTracking() on (s != null ? s.ProductVariantId : null) equals pv.Id into jpv
                                       from pv in jpv.DefaultIfEmpty()
                                       join pt in _db.ProductTypes.AsNoTracking() on (pv != null ? pv.ProductTypeId : null) equals pt.Id into jpt
                                       from pt in jpt.DefaultIfEmpty()
                                       join pc in _db.ProductCategories.AsNoTracking() on (pt != null ? pt.CategoryId : null) equals pc.Id into jpc
                                       from pc in jpc.DefaultIfEmpty()
                                       where saleIds.Contains(d.SaleId ?? 0)
                                       select new CustomerSaleLineItemDto
                                       {
                                           SaleId = d.SaleId ?? 0,
                                           LineId = d.Id,
                                           StockId = d.StockId,
                                           Quantity = d.Quantity,
                                           TotalWeightGram = d.TotalWeightGram,
                                           SoldPrice = d.SoldPrice,
                                           ProductVariantId = pv != null ? pv.Id : (int?)null,
                                           ProductVariantName = pv != null ? pv.Name : null,
                                           ProductTypeId = pt != null ? pt.Id : (int?)null,
                                           ProductTypeName = pt != null ? pt.Name : null,
                                           CategoryName = pc != null ? pc.Name : null,
                                           Brand = pv != null ? pv.Brand : null,
                                           Ayar = pv != null ? pv.Ayar : null,
                                           Color = pv != null ? pv.Color : null
                                       }).ToListAsync(ct);
            }

            // Purchase line items'ı purchase'lara ekle ve TotalAmount hesapla
            var purchaseLinesDict = purchaseLinesData
                .GroupBy(x => x.PurchaseId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new CustomerPurchaseLineDto
                    {
                        LineId = x.LineId,
                        StockId = x.StockId,
                        Quantity = x.Quantity,
                        TotalWeightGram = x.TotalWeightGram,
                        PurchasePrice = x.PurchasePrice,
                        ProductVariantId = x.ProductVariantId,
                        ProductVariantName = x.ProductVariantName,
                        ProductTypeId = x.ProductTypeId,
                        ProductTypeName = x.ProductTypeName,
                        CategoryName = x.CategoryName,
                        Brand = x.Brand,
                        Ayar = x.Ayar,
                        Color = x.Color
                    }).ToList());

            foreach (var purchase in purchases)
            {
                if (purchaseLinesDict.TryGetValue(purchase.Id, out var lines))
                {
                    purchase.LineItems = lines;
                    purchase.TotalAmount = lines.Sum(l => (l.PurchasePrice ?? 0) * (l.Quantity ?? 0));
                }
            }

            // Sale line items'ı sales'lere ekle ve TotalAmount hesapla
            var saleLinesDict = saleLinesData
                .GroupBy(x => x.SaleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new CustomerSaleLineDto
                    {
                        LineId = x.LineId,
                        StockId = x.StockId,
                        Quantity = x.Quantity,
                        TotalWeightGram = x.TotalWeightGram,
                        SoldPrice = x.SoldPrice,
                        ProductVariantId = x.ProductVariantId,
                        ProductVariantName = x.ProductVariantName,
                        ProductTypeId = x.ProductTypeId,
                        ProductTypeName = x.ProductTypeName,
                        CategoryName = x.CategoryName,
                        Brand = x.Brand,
                        Ayar = x.Ayar,
                        Color = x.Color
                    }).ToList());

            foreach (var sale in sales)
            {
                if (saleLinesDict.TryGetValue(sale.Id, out var lines))
                {
                    sale.LineItems = lines;
                    sale.TotalAmount = lines.Sum(l => (l.SoldPrice ?? 0) * (l.Quantity ?? 0));
                }
            }

            var dto = new CustomerDetailResponseDto
            {
                Customer = customer,
                Purchases = purchases,
                Sales = sales
            };

            return ApiResult<CustomerDetailResponseDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<CustomerDto>> CreateAsync(CustomerCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var e = new Domain.Entities.Customers
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Note = dto.Note,
                NationalId = dto.NationalId,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            _db.Customers.Add(e);
            await _db.SaveChangesAsync(ct);

            var result = new CustomerDto
            {
                Id = e.Id,
                Name = e.Name!,
                Phone = e.Phone,
                Note = e.Note,
                NationalId = e.NationalId,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                IsActive = e.IsActive
            };
            return ApiResult<CustomerDto>.Ok(result, "Oluşturuldu", 201);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, CustomerUpdateDto dto, CancellationToken ct = default)
        {
            var e = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (e is null) return ApiResult<bool>.Fail("Müşteri bulunamadı", statusCode: 404);

            e.Name = dto.Name;
            e.Phone = dto.Phone;
            e.Note = dto.Note;
            e.NationalId = dto.NationalId;
            e.IsActive = dto.IsActive;
            e.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (e is null) return ApiResult<bool>.Fail("Müşteri bulunamadı", statusCode: 404);

            // referans kontrolü (satış/alış)
            var hasRefs = await _db.Sales.AsNoTracking().AnyAsync(s => s.CustomerId == id, ct)
                       || await _db.Purchases.AsNoTracking().AnyAsync(p => p.CustomerId == id, ct);

            if (hasRefs)
            {
                // tercihe göre 409 veya soft-delete:
                // return ApiResult<bool>.Fail("Müşterinin hareketleri var, silinemez.", statusCode: 409);
                e.IsDeleted = true;
                e.DeletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return ApiResult<bool>.Ok(true, "Müşteri soft-delete yapıldı (kayıtları olduğu için).", 200);
            }

            _db.Customers.Remove(e);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }
    }
}
