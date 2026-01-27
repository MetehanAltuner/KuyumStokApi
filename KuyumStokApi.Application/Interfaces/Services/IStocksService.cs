using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Stocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    /// <summary>Stok servis sözleşmesi.</summary>
    public interface IStocksService
    {
        // LİSTE: Aynı ProductVariantId + BranchId'ye sahip stokları gruplayarak toplam adet ve ağırlık ile tek kayıt olarak döndürür
        Task<ApiResult<PagedResult<StockDto>>> GetPagedAsync(StockFilter filter, CancellationToken ct = default);

        // DETAY: seçili varyant, aynı store’daki tüm şubeler
        Task<ApiResult<StockVariantDetailByStoreDto>> GetVariantDetailInStoreAsync(int variantId, CancellationToken ct = default);

        // Mevcut CRUD imzaları
        Task<ApiResult<StockDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<ApiResult<StockDto>> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
        Task<ApiResult<StockDto>> GetByPublicCodeAsync(string code, CancellationToken ct = default);
        Task<ApiResult<string>> GetResolveRedirectUrlAsync(string code, CancellationToken ct = default);
        Task<ApiResult<StockDto>> CreateAsync(StockCreateDto dto, CancellationToken ct = default, bool skipPurchaseCreation = false);
        Task<ApiResult<bool>> UpdateAsync(Guid id, StockUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<StockPublicCodeBackfillResultDto>> BackfillPublicCodesAsync(int limit = 500, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(Guid id, CancellationToken ct = default);

        // FAVORİLER: En çok satılan ürünler (ProductVariant bazında)
        Task<ApiResult<List<FavoriteProductDto>>> GetFavoritesAsync(int top = 10, int days = 30, bool onlyMarked = false, CancellationToken ct = default);
    }
}
