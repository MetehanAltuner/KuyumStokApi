using KuyumStokApi.Application.DTOs.Stocks;
using KuyumStokApi.Application.Interfaces.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Infrastructure.QrCode;
using KuyumStokApi.Infrastructure.Services.PublicCodeService;
using KuyumStokApi.Infrastructure.Services.StocksService;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace KuyumStokApi.Tests
{
    public sealed class StocksServiceTests
    {
        [Fact]
        public async Task CreateAsync_GeneratesPublicCode_AndQrPayload()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var db = new AppDbContext(options);
            var qrOptions = new QrCodeOptions { BaseUrl = "https://example.test", ResolvePath = "/r" };
            var qrService = new FakeQrCodeService();
            var publicCodeService = new PublicCodeService();
            var currentUser = new FakeCurrentUserContext { BranchId = 1 };

            var service = new StocksService(
                db,
                currentUser,
                Options.Create(qrOptions),
                publicCodeService,
                qrService,
                NullLogger<StocksService>.Instance);

            var result = await service.CreateAsync(new StockCreateDto
            {
                ProductVariantId = 1,
                BranchId = 1,
                Quantity = 1,
                TotalWeightGram = 1m,
                GenerateQrCode = true
            });

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.False(string.IsNullOrWhiteSpace(result.Data!.PublicCode));
            Assert.False(string.IsNullOrWhiteSpace(result.Data.QrCode));
            Assert.Equal(result.Data.PublicCode, qrService.LastPayload);
            Assert.DoesNotContain("/r/", qrService.LastPayload, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BackfillPublicCodesAsync_SetsCode_AndRegeneratesQr()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var db = new AppDbContext(options);
            var qrOptions = new QrCodeOptions { BaseUrl = "https://example.test", ResolvePath = "/r" };
            var qrService = new FakeQrCodeService();
            var publicCodeService = new PublicCodeService();
            var currentUser = new FakeCurrentUserContext { BranchId = 1 };

            db.Stocks.Add(new KuyumStokApi.Domain.Entities.Stocks
            {
                Id = Guid.NewGuid(),
                ProductVariantId = 1,
                BranchId = 1,
                Quantity = 1,
                TotalWeightGram = 1m,
                Barcode = "STK-001",
                QrCode = Convert.ToBase64String(Encoding.UTF8.GetBytes("old")),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = new StocksService(
                db,
                currentUser,
                Options.Create(qrOptions),
                publicCodeService,
                qrService,
                NullLogger<StocksService>.Instance);

            var result = await service.BackfillPublicCodesAsync(limit: 10);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.UpdatedCount);
            Assert.Equal(0, result.Data.RemainingCount);

            var updated = await db.Stocks.FirstAsync();
            Assert.False(string.IsNullOrWhiteSpace(updated.PublicCode));
            Assert.Equal(updated.PublicCode, qrService.LastPayload);
            Assert.DoesNotContain("http", qrService.LastPayload, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(Convert.ToBase64String(Encoding.UTF8.GetBytes("old")), updated.QrCode);
        }

        private sealed class FakeQrCodeService : IQrCodeService
        {
            public string? LastPayload { get; private set; }

            public string GenerateQrPngBase64(string payload)
            {
                LastPayload = payload;
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            }
        }

        private sealed class FakeCurrentUserContext : ICurrentUserContext
        {
            public bool IsAuthenticated => true;
            public int? UserId => 1;
            public int? BranchId { get; set; }
            public string? UserName => "test";
        }
    }
}
