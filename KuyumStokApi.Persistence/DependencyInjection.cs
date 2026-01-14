using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration cfg)
        {
            var connectionString = cfg.GetConnectionString("DefaultConnection");
            
            // AddDbContext factory overload kullanarak ServiceProvider'ı set edebiliriz
            // Factory delegate içinde AppDbContext oluşturulduğunda ServiceProvider'ı set ediyoruz
            services.AddDbContext<AppDbContext>((serviceProvider, opt) =>
            {
                opt.UseNpgsql(connectionString);
            });
            
            // AddDbContext zaten bir factory oluşturuyor, ancak ServiceProvider'ı set etmek için
            // factory'yi override ediyoruz. Bu, her DbContext instance'ı oluşturulduğunda
            // ServiceProvider'ın set edilmesini sağlar.
            // NOT: AddScoped ile override ettiğimiz için, AddDbContext'in oluşturduğu factory
            // kullanılmayacak ve bizim factory'miz kullanılacak.
            services.AddScoped<AppDbContext>(sp =>
            {
                // DbContextOptionsBuilder kullanarak options oluştur
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseNpgsql(connectionString);
                
                var currentUser = sp.GetService<KuyumStokApi.Application.Common.ICurrentUserService>();
                
                // AppDbContext'i oluştur
                var context = new AppDbContext(optionsBuilder.Options, currentUser);
                
                // ServiceProvider'ı set et (SaveChanges sırasında broadcast için gerekli)
                context.SetServiceProvider(sp);
                
                return context;
            });
            
            return services;
        }
    }
}
