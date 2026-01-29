using KuyumStokApi.Application.DTOs.Purchase;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Infrastructure.Security;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KuyumStokApi.Infrastructure.DevSeeding;

public static class DevSeedRunner
{
    private const string DevStockBarcode = "DEV-PL-001";
    private const int PurchaseQuantity = 2;
    private const decimal PurchaseTotalWeight = 10.50m;
    private const decimal PurchaseUnitPrice = 10000m;
    private const int SaleQuantity = 1;
    private const decimal SaleTotalWeight = 3.00m;
    private const decimal SaleUnitPrice = 12000m;

    public static async Task RunAsync(IServiceProvider services, IHostEnvironment env, IConfiguration configuration)
    {
        if (!env.IsDevelopment())
            return;

        var enabled = Environment.GetEnvironmentVariable("DEV_SEED_ENABLE");
        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            return;

        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DevSeedRunner");

        try
        {
            await RunInternalAsync(sp, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dev seed calistirilirken hata olustu.");
            throw;
        }
    }

    private static async Task RunInternalAsync(IServiceProvider sp, ILogger logger)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var purchasesService = sp.GetRequiredService<IPurchasesService>();

        var username = Environment.GetEnvironmentVariable("DEV_SEED_USERNAME");
        if (string.IsNullOrWhiteSpace(username))
            username = "mete";

        var password = Environment.GetEnvironmentVariable("DEV_SEED_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("DEV_SEED_PASSWORD bos. Dev seed durduruldu.");
            return;
        }

        var branch = await db.Branches.AsNoTracking()
            .Where(b => !b.IsDeleted && b.IsActive)
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync();

        if (branch == null)
        {
            logger.LogWarning("Dev seed icin aktif sube bulunamadi.");
            return;
        }

        var variant = await db.ProductVariants.AsNoTracking()
            .Where(v => !v.IsDeleted && v.IsActive)
            .OrderBy(v => v.Id)
            .FirstOrDefaultAsync();

        if (variant == null)
        {
            logger.LogWarning("Dev seed icin aktif urun varyanti bulunamadi.");
            return;
        }

        var roleId = await db.Roles.AsNoTracking()
            .Where(r => !r.IsDeleted && r.IsActive)
            .OrderBy(r => r.Id)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync();

        var paymentMethodId = await db.PaymentMethods.AsNoTracking()
            .Where(pm => !pm.IsDeleted && pm.IsActive)
            .OrderBy(pm => pm.Id)
            .Select(pm => (int?)pm.Id)
            .FirstOrDefaultAsync();

        var normalizedUsername = username.Trim().ToLowerInvariant();
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

        var now = DateTime.UtcNow;
        if (user == null)
        {
            var salt = hasher.GenerateSalt();
            user = new Users
            {
                Username = normalizedUsername,
                PasswordSalt = salt,
                PasswordHash = hasher.Hash(password, salt),
                FirstName = "Dev",
                LastName = "Seed",
                RoleId = roleId,
                BranchId = branch.Id,
                IsActive = true,
                IsDeleted = false,
                MustChangePassword = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Users.Add(user);
        }
        else
        {
            if (user.BranchId == null)
                user.BranchId = branch.Id;
            if (user.RoleId == null && roleId.HasValue)
                user.RoleId = roleId;

            user.IsActive = true;
            user.IsDeleted = false;
            user.DeletedAt = null;
            user.DeletedBy = null;

            var salt = hasher.GenerateSalt();
            user.PasswordSalt = salt;
            user.PasswordHash = hasher.Hash(password, salt);
            user.MustChangePassword = false;
            user.UpdatedAt = now;
        }

        await db.SaveChangesAsync();

        var branchId = user.BranchId ?? branch.Id;

        var stockByBarcode = await db.Stocks
            .FirstOrDefaultAsync(s => s.Barcode == DevStockBarcode);

        if (stockByBarcode != null)
        {
            if (stockByBarcode.BranchId.HasValue && stockByBarcode.BranchId != branchId)
            {
                user.BranchId = stockByBarcode.BranchId;
                user.UpdatedAt = DateTime.UtcNow;
                branchId = stockByBarcode.BranchId.Value;
                await db.SaveChangesAsync();
            }

            if (stockByBarcode.ProductVariantId.HasValue)
            {
                var variantFromStock = await db.ProductVariants.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == stockByBarcode.ProductVariantId.Value);
                if (variantFromStock != null)
                    variant = variantFromStock;
            }
        }

        var stockByVariant = await db.Stocks
            .FirstOrDefaultAsync(s => s.BranchId == branchId && s.ProductVariantId == variant.Id);

        var barcodeToUse = stockByVariant?.Barcode ?? stockByBarcode?.Barcode ?? DevStockBarcode;

        var hasDevPurchase = false;
        if (stockByVariant != null)
        {
            hasDevPurchase = await db.ProductLifecycles
                .AnyAsync(l => l.StockId == stockByVariant.Id && l.Notes == "DEV Purchase");
        }

        if (!hasDevPurchase)
        {
            var purchaseDto = new PurchaseCreateDto
            {
                UserId = user.Id,
                BranchId = branchId,
                PaymentMethodId = paymentMethodId,
                CustomerId = null,
                Items = new List<PurchaseItemDto>
                {
                    new()
                    {
                        ProductVariantId = variant.Id,
                        BranchId = branchId,
                        Barcode = barcodeToUse,
                        Quantity = PurchaseQuantity,
                        PurchasePrice = PurchaseUnitPrice,
                        TotalWeightGram = PurchaseTotalWeight
                    }
                }
            };

            var purchaseResult = await purchasesService.CreateAsync(purchaseDto);
            if (!purchaseResult.Success)
            {
                logger.LogWarning("Dev seed purchase olusturulamadi: {Message}", purchaseResult.Message);
                return;
            }

            if (purchaseResult.Data != null)
            {
                foreach (var stockId in purchaseResult.Data.StockIds)
                {
                    var exists = await db.ProductLifecycles
                        .AnyAsync(l => l.StockId == stockId && l.Notes == "DEV Purchase");
                    if (!exists)
                    {
                        db.ProductLifecycles.Add(new ProductLifecycles
                        {
                            StockId = stockId,
                            UserId = user.Id,
                            ActionId = await db.LifecycleActions
                                .Where(x => x.Name == "Purchase")
                                .Select(x => (int?)x.Id)
                                .FirstOrDefaultAsync(),
                            Notes = "DEV Purchase",
                            Timestamp = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
                await db.SaveChangesAsync();
            }
        }

        var stock = stockByVariant ?? await db.Stocks.FirstOrDefaultAsync(s => s.Barcode == barcodeToUse);
        if (stock == null)
        {
            logger.LogWarning("Dev seed stok bulunamadi (barcode: {Barcode}).", barcodeToUse);
            return;
        }

        var hasDevSale = await db.ProductLifecycles
            .AnyAsync(l => l.StockId == stock.Id && l.Notes == "DEV Sale");

        if (hasDevSale)
            return;

        var currentQty = stock.Quantity ?? 0;
        if (currentQty < SaleQuantity || stock.TotalWeightGram < SaleTotalWeight)
        {
            logger.LogWarning("Dev seed satis icin stok yetersiz. Mevcut Adet={Qty}, Agirlik={Weight}.", currentQty, stock.TotalWeightGram);
            return;
        }

        using var tx = await db.Database.BeginTransactionAsync();

        var sale = new Sales
        {
            UserId = user.Id,
            BranchId = branchId,
            CustomerId = null,
            PaymentMethodId = paymentMethodId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        stock.Quantity = currentQty - SaleQuantity;
        stock.TotalWeightGram -= SaleTotalWeight;
        stock.UpdatedAt = DateTime.UtcNow;

        db.SaleDetails.Add(new SaleDetails
        {
            SaleId = sale.Id,
            StockId = stock.Id,
            Quantity = SaleQuantity,
            SoldPrice = SaleUnitPrice,
            TotalWeightGram = SaleTotalWeight,
            UpdatedAt = DateTime.UtcNow
        });

        var saleActionId = await db.LifecycleActions
            .Where(x => x.Name == "Sale")
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        db.ProductLifecycles.Add(new ProductLifecycles
        {
            StockId = stock.Id,
            UserId = user.Id,
            ActionId = saleActionId,
            Notes = "DEV Sale",
            Timestamp = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        if (paymentMethodId.HasValue)
        {
            var totalAmount = SaleQuantity * SaleUnitPrice;
            db.SalePayments.Add(new SalePayments
            {
                SaleId = sale.Id,
                PaymentMethodId = paymentMethodId.Value,
                Amount = totalAmount,
                NetAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
