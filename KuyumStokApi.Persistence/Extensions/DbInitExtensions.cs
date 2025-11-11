using KuyumStokApi.Persistence.Contexts;
using KuyumStokApi.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KuyumStokApi.Persistence.Extensions;

/// <summary>
/// Veritabanı migrasyonu ve seed data ekleme extension metodları.
/// </summary>
public static class DbInitExtensions
{
    /// <summary>
    /// Uygulama başlatıldığında migration ve seed işlemlerini gerçekleştirir.
    /// </summary>
    /// <param name="host">IHost instance</param>
    /// <returns>Task</returns>
    public static async Task MigrateAndSeedAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // Ortam değişkeni kontrolü - SKIP_DB_MIGRATE=true ise atla
            var skipMigrate = Environment.GetEnvironmentVariable("SKIP_DB_MIGRATE");
            if (string.Equals(skipMigrate, "true", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("⏭️  SKIP_DB_MIGRATE=true — Migration ve seed atlandı");
                return;
            }

            var context = services.GetRequiredService<AppDbContext>();

            // ============================================================
            // ADIM 1: Veritabanı Bağlantısı ve Varlık Kontrolü
            // ============================================================
            logger.LogInformation("🔄 Veritabanı başlatma işlemi başlıyor...");
            
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogInformation("  → Veritabanı bağlantısı kuruluyor...");
            }
            
            // ============================================================
            // ADIM 2: Migration Dosyaları Kontrolü
            // ============================================================
            // NOT: GetMigrations() SENKRON, GetMigrationsAsync() YOK!
            var allMigrations = context.Database.GetMigrations();
            var hasMigrations = allMigrations.Any();
            
            if (hasMigrations)
            {
                // ========================================
                // YOL 1: MİGRATİON DOSYALARI VAR (ÖNERİLEN)
                // ========================================
                logger.LogInformation("  ℹ️  Migration dosyaları bulundu: {Count} adet", allMigrations.Count());
                
                var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                
                logger.LogInformation("     • Uygulanmış migration: {Applied}", appliedMigrations.Count());
                logger.LogInformation("     • Bekleyen migration: {Pending}", pendingMigrations.Count());
                
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("  🔄 {Count} migration uygulanıyor...", pendingMigrations.Count());
                    foreach (var migration in pendingMigrations)
                    {
                        logger.LogInformation("     → {Migration}", migration);
                    }
                    
                    // MİGRATİON UYGULA (DB yoksa oluşturur, tablolar yoksa ekler, şema değişti ise günceller)
                    // MEVCUT VERİLER KORUNUR! ✅
                    await context.Database.MigrateAsync();
                    
                    logger.LogInformation("  ✅ Migration'lar başarıyla uygulandı");
                    logger.LogInformation("     💾 Veritabanı şeması güncellendi");
                }
                else
                {
                    logger.LogInformation("  ✅ Veritabanı zaten güncel (pending migration yok)");
                }
            }
            else
            {
                // ========================================
                // YOL 2: MİGRATİON DOSYALARI YOK (FALLBACK - SADECE GELİŞTİRME)
                // ========================================
                logger.LogWarning("  ⚠️  Migration dosyası bulunamadı!");
                logger.LogWarning("     Bu mod SADECE geliştirme ortamı için uygundur.");
                logger.LogWarning("     Production'da MUTLAKA migration kullanın!");
                
                logger.LogInformation("  → EnsureCreated() ile veritabanı oluşturuluyor...");
                
                // EnsureCreated: Sadece DB tamamen boşsa çalışır!
                // DB varsa ve tablolar varsa HİÇBİR ŞEY YAPMAZ!
                var created = await context.Database.EnsureCreatedAsync();
                
                if (created)
                {
                    logger.LogInformation("  ✅ Veritabanı ve tablolar oluşturuldu");
                    logger.LogInformation("     💡 Şema değişikliği için migration oluşturun:");
                    logger.LogInformation("        cd KuyumStokApi.Persistence");
                    logger.LogInformation("        dotnet ef migrations add InitialCreate --startup-project ../KuyumStokApi.API");
                }
                else
                {
                    logger.LogWarning("  ℹ️  Veritabanı zaten mevcut");
                    logger.LogWarning("     ⚠️  EnsureCreated() şema değişikliği YAPMAZ!");
                    logger.LogWarning("     ⚠️  Entity'lerde değişiklik varsa migration oluşturun:");
                    logger.LogWarning("        dotnet ef migrations add <AciklamaAdi> --startup-project ../KuyumStokApi.API");
                }
            }

            // Seed data ekleme
            logger.LogInformation("🌱 Seed data ekleniyor...");
            
            await SeedData.SeedAsync(context, logger);
            
            logger.LogInformation("✅ Seed data başarıyla uygulandı");
            logger.LogInformation("🎉 Veritabanı başlatma işlemi tamamlandı");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Veritabanı migration veya seed işlemi sırasında hata oluştu");
            throw;
        }
    }
}

