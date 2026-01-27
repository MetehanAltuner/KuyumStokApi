using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using QRCoder;
using System;

namespace KuyumStokApi.Infrastructure.QrCode
{
    /// <summary>QR kod üretim servisi.</summary>
    public sealed class QrCodeService : IQrCodeService
    {
        private readonly QrCodeOptions _options;

        public QrCodeService(IOptions<QrCodeOptions> options)
        {
            _options = options.Value;
        }

        public string GenerateQrPngBase64(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                throw new ArgumentException("QR payload boş olamaz.", nameof(payload));

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, MapEccLevel(_options.ErrorCorrection));

            var modules = data.ModuleMatrix.Count;
            var pixelsPerModule = CalculatePixelsPerModule(modules);

            using var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(pixelsPerModule, drawQuietZones: true);
            return Convert.ToBase64String(bytes);
        }

        private int CalculatePixelsPerModule(int modules)
        {
            var target = _options.TargetPixelSize > 0 ? _options.TargetPixelSize : 300;
            var min = _options.MinPixelsPerModule > 0 ? _options.MinPixelsPerModule : 4;
            var max = _options.MaxPixelsPerModule > 0 ? _options.MaxPixelsPerModule : 20;

            var denom = modules + 8;
            var ppm = denom > 0 ? target / denom : min;

            if (ppm < min) ppm = min;
            if (ppm > max) ppm = max;
            return ppm;
        }

        private static QRCodeGenerator.ECCLevel MapEccLevel(string? value)
        {
            return value?.Trim().ToUpperInvariant() switch
            {
                "L" => QRCodeGenerator.ECCLevel.L,
                "Q" => QRCodeGenerator.ECCLevel.Q,
                "H" => QRCodeGenerator.ECCLevel.H,
                _ => QRCodeGenerator.ECCLevel.M
            };
        }
    }
}
