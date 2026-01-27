using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>QR resolve endpoint'i.</summary>
    [ApiController]
    [Route("r")]
    public sealed class ResolveController : ControllerBase
    {
        private readonly IStocksService _stocksService;

        public ResolveController(IStocksService stocksService)
        {
            _stocksService = stocksService;
        }

        /// <summary>Public code'u resolve eder ve frontend'e yönlendirir.</summary>
        [HttpGet("{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> Resolve(string code, CancellationToken ct)
        {
            var r = await _stocksService.GetResolveRedirectUrlAsync(code, ct);
            if (!r.Success || string.IsNullOrWhiteSpace(r.Data))
                return StatusCode(r.StatusCode, r);

            return Redirect(r.Data);
        }
    }
}
