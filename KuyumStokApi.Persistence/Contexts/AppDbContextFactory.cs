using KuyumStokApi.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Persistence.Contexts
{
    public sealed class AppDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly IServiceProvider _serviceProvider;

        public AppDbContextFactory(DbContextOptions<AppDbContext> options, IServiceProvider serviceProvider)
        {
            _options = options;
            _serviceProvider = serviceProvider;
        }

        public AppDbContext CreateDbContext()
        {
            var currentUser = _serviceProvider.GetService(typeof(ICurrentUserService)) as ICurrentUserService;
            var context = new AppDbContext(_options, currentUser);
            context.SetServiceProvider(_serviceProvider);
            return context;
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
