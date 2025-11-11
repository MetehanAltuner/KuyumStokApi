using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KuyumStokApi.Persistence.Seed;

/// <summary>
/// Veritabanı için default seed data yönetimi.
/// Upsert mantığı: Varsa güncelle, yoksa ekle (silme YOK).
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Tüm seed verilerini transaction içinde upsert eder.
    /// </summary>
    /// <param name="db">AppDbContext instance</param>
    /// <param name="logger">ILogger instance</param>
    /// <returns>Task</returns>
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        // Transaction başlat (tüm seed işlemleri atomik olacak)
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            logger.LogInformation("  → Roles seeding...");
            await SeedRolesAsync(db, logger);

            logger.LogInformation("  → PaymentMethods seeding...");
            await SeedPaymentMethodsAsync(db, logger);

            logger.LogInformation("  → LifecycleActions seeding...");
            await SeedLifecycleActionsAsync(db, logger);

            // Değişiklikleri kaydet
            await db.SaveChangesAsync();

            // Transaction commit
            await transaction.CommitAsync();

            logger.LogInformation("  ✅ Tüm seed veriler başarıyla uygulandı");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "  ❌ Seed data işlemi sırasında hata oluştu, rollback yapılıyor...");
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Roles tablosuna default roller ekler/günceller.
    /// </summary>
    private static async Task SeedRolesAsync(AppDbContext db, ILogger logger)
    {
        var now = DateTime.UtcNow;

        var defaultRoles = new[]
        {
            new { Name = "Admin", Description = "Tüm yetkiler" },
            new { Name = "Manager", Description = "Yönetici" },
            new { Name = "Cashier", Description = "Kasiyer" },
            new { Name = "Viewer", Description = "Salt okunur" }
        };

        foreach (var roleData in defaultRoles)
        {
            // Name bazlı unique kontrol
            var existing = await db.Roles
                .FirstOrDefaultAsync(r => r.Name == roleData.Name);

            if (existing != null)
            {
                // Varsa güncelle
                existing.UpdatedAt = now;
                existing.IsActive = true;
                existing.IsDeleted = false;
                
                logger.LogDebug("    ↻ Role güncellendi: {Name}", roleData.Name);
            }
            else
            {
                // Yoksa ekle
                var newRole = new Roles
                {
                    Name = roleData.Name,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true,
                    IsDeleted = false
                };

                await db.Roles.AddAsync(newRole);
                logger.LogDebug("    + Role eklendi: {Name}", roleData.Name);
            }
        }

        logger.LogInformation("    ✓ {Count} role işlendi", defaultRoles.Length);
    }

    /// <summary>
    /// PaymentMethods tablosuna default ödeme yöntemleri ekler/günceller.
    /// </summary>
    private static async Task SeedPaymentMethodsAsync(AppDbContext db, ILogger logger)
    {
        var now = DateTime.UtcNow;

        var defaultMethods = new[]
        {
            "Nakit",
            "Kredi Kartı",
            "Havale/EFT"
        };

        foreach (var methodName in defaultMethods)
        {
            // Name bazlı unique kontrol
            var existing = await db.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.Name == methodName);

            if (existing != null)
            {
                // Varsa güncelle
                existing.IsActive = true;
                existing.IsDeleted = false;
                
                logger.LogDebug("    ↻ PaymentMethod güncellendi: {Name}", methodName);
            }
            else
            {
                // Yoksa ekle
                var newMethod = new PaymentMethods
                {
                    Name = methodName,
                    IsActive = true,
                    IsDeleted = false
                };

                await db.PaymentMethods.AddAsync(newMethod);
                logger.LogDebug("    + PaymentMethod eklendi: {Name}", methodName);
            }
        }

        logger.LogInformation("    ✓ {Count} payment method işlendi", defaultMethods.Length);
    }

    /// <summary>
    /// LifecycleActions tablosuna default aksiyonlar ekler/günceller.
    /// </summary>
    private static async Task SeedLifecycleActionsAsync(AppDbContext db, ILogger logger)
    {
        var defaultActions = new[]
        {
            new { Name = "Purchase", Description = "Alış" },
            new { Name = "Sale", Description = "Satış" },
            new { Name = "Transfer", Description = "Şube Transfer" },
            new { Name = "Count", Description = "Sayım" },
            new { Name = "Adjustment", Description = "Düzeltme" },
            new { Name = "Damage", Description = "Hasar" },
            new { Name = "Lost", Description = "Kayıp" }
        };

        foreach (var actionData in defaultActions)
        {
            // Name bazlı unique kontrol
            var existing = await db.LifecycleActions
                .FirstOrDefaultAsync(la => la.Name == actionData.Name);

            if (existing != null)
            {
                // Varsa güncelle
                existing.Description = actionData.Description;
                
                logger.LogDebug("    ↻ LifecycleAction güncellendi: {Name}", actionData.Name);
            }
            else
            {
                // Yoksa ekle
                var newAction = new LifecycleActions
                {
                    Name = actionData.Name,
                    Description = actionData.Description
                };

                await db.LifecycleActions.AddAsync(newAction);
                logger.LogDebug("    + LifecycleAction eklendi: {Name}", actionData.Name);
            }
        }

        logger.LogInformation("    ✓ {Count} lifecycle action işlendi", defaultActions.Length);
    }
}

