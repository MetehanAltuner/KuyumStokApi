using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Persistence.Contexts
{
    public partial class AppDbContext
    {
        // Dashboard notification servisi için IServiceProvider (lazy loading)
        private IServiceProvider? _serviceProvider;

        /// <summary>
        /// ServiceProvider'ı set et (DependencyInjection tarafından çağrılır)
        /// </summary>
        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            foreach (var et in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDeletable).IsAssignableFrom(et.ClrType))
                {
                    var p = Expression.Parameter(et.ClrType, "e");
                    var body = Expression.Equal(
                        Expression.Property(p, nameof(ISoftDeletable.IsDeleted)),
                        Expression.Constant(false)
                    );
                    var lambda = Expression.Lambda(body, p);
                    modelBuilder.Entity(et.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        public override int SaveChanges()
        {
            ApplySoftDelete();

            // Değişen entity türlerini topla (SaveChanges öncesi)
            var changedEntityTypes = GetChangedEntityTypes();

            var result = base.SaveChanges();

            // SaveChanges başarılı olduysa broadcast tetikle
            if (result > 0 && changedEntityTypes.Any())
            {
                TriggerDashboardNotifications(changedEntityTypes);
            }

            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplySoftDelete();

            // Değişen entity türlerini topla (SaveChanges öncesi)
            var changedEntityTypes = GetChangedEntityTypes();

            var result = await base.SaveChangesAsync(cancellationToken);

            // SaveChanges başarılı olduysa broadcast tetikle
            if (result > 0 && changedEntityTypes.Any())
            {
                TriggerDashboardNotifications(changedEntityTypes);
            }

            return result;
        }

        private void ApplySoftDelete()
        {
            var now = DateTime.UtcNow;
            var by = _currentUser?.UserId;


            foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = by;
                }
            }
        }

        /// <summary>
        /// ChangeTracker'dan değişen entity türlerini toplar
        /// </summary>
        private HashSet<string> GetChangedEntityTypes()
        {
            var entityTypes = new HashSet<string>();

            foreach (var entry in ChangeTracker.Entries())
            {
                // Sadece Added, Modified, Deleted state'lerindeki entity'leri al
                if (entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified ||
                    entry.State == EntityState.Deleted)
                {
                    var entityTypeName = entry.Entity.GetType().Name;

                    // Generic type'lardan sadece tip adını al (örn: SaleDetails -> SaleDetails)
                    if (entityTypeName.Contains('`'))
                    {
                        entityTypeName = entityTypeName.Substring(0, entityTypeName.IndexOf('`'));
                    }

                    entityTypes.Add(entityTypeName);
                }
            }

            return entityTypes;
        }

        /// <summary>
        /// Dashboard bildirim servisini tetikler (fire-and-forget)
        /// </summary>
        private void TriggerDashboardNotifications(HashSet<string> changedEntityTypes)
        {
            if (_serviceProvider == null) return;

            try
            {
                // Servisi scope içinde al
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider
                    .GetService<IDashboardNotificationService>();

                if (notificationService != null)
                {
                    // Fire-and-forget: Async çağrıyı başlat ama bekleme
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await notificationService.NotifyDashboardChangesAsync(changedEntityTypes);
                        }
                        catch (Exception ex)
                        {
                            var logger = scope.ServiceProvider.GetService<ILogger<AppDbContext>>();
                            logger?.LogWarning(ex, "Dashboard notification tetiklenirken hata oluştu");
                        }
                    });
                }
            }
            catch
            {
                // Hata durumunda loglama yap ama SaveChanges'ı etkileme
                // Logger'a erişemezsek sessizce devam et
            }
        }
    }
}
