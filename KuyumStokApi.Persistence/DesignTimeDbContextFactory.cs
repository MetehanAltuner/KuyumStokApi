using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using KuyumStokApi.Persistence.Contexts;

namespace KuyumStokApi.Persistence;

/// <summary>
/// EF Core design-time factory for migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Design-time için connection string (appsettings.json'dan okunabilir veya hardcoded)
        // Production'da bu kullanılmaz, sadece migration oluştururken kullanılır
        var connectionString = "Host=localhost;Port=5432;Database=inventorydb;Username=inventory;Password=inventorypass;Include Error Detail=true;TimeZone=UTC";
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new AppDbContext(optionsBuilder.Options);
    }
}

