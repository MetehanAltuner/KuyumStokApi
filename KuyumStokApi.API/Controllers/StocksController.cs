using KuyumStokApi.Application.DTOs.Stocks;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>Stok işlemleri API uçları.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class StocksController : ControllerBase
    {
        private readonly IStocksService _svc;
        public StocksController(IStocksService svc) => _svc = svc;

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] StockFilter filter, CancellationToken ct)
        {
            var r = await _svc.GetPagedAsync(filter, ct);
            return StatusCode(r.StatusCode, r);
        }

        [HttpGet("variant/{variantId:int}/detail")]
        [Authorize]
        public async Task<IActionResult> GetVariantDetail(int variantId, CancellationToken ct)
        {
            var r = await _svc.GetVariantDetailInStoreAsync(variantId, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Id’ye göre stok detayını getirir.</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var r = await _svc.GetByIdAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Barkoda göre stok getirir.</summary>
        [HttpGet("by-barcode/{barcode}")]
        [Authorize]
        public async Task<IActionResult> GetByBarcode(string barcode, CancellationToken ct)
        {
            var r = await _svc.GetByBarcodeAsync(barcode, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Yeni stok kaydı oluşturur.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] StockCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Stok kaydını günceller.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] StockUpdateDto dto, CancellationToken ct)
        {
            var r = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Stok kaydını siler (bağlı kayıt varsa 409).</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var r = await _svc.DeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Stok kaydını kalıcı siler (bağlı kayıt yoksa).</summary>
        [HttpDelete("{id:int}/hard")]
        [Authorize]
        public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
        {
            var r = await _svc.HardDeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
