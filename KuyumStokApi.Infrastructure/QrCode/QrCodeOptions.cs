namespace KuyumStokApi.Infrastructure.QrCode;

/// <summary>QR kod yapılandırma seçenekleri.</summary>
public sealed class QrCodeOptions
{
    /// <summary>QR kod içinde kullanılacak base URL (örn: http://130.162.231.124/saleReceipt/)</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>QR çözümleme path'i (örn: /r)</summary>
    public string ResolvePath { get; set; } = "/r";

    /// <summary>Frontend yönlendirme için base URL (örn: https://app.example.com)</summary>
    public string FrontendBaseUrl { get; set; } = string.Empty;

    /// <summary>QR hata düzeltme seviyesi (L/M/Q/H).</summary>
    public string ErrorCorrection { get; set; } = "M";

    /// <summary>QR hedef piksel boyutu (varsayılan 300).</summary>
    public int TargetPixelSize { get; set; } = 300;

    /// <summary>Modül başına minimum piksel.</summary>
    public int MinPixelsPerModule { get; set; } = 4;

    /// <summary>Modül başına maksimum piksel.</summary>
    public int MaxPixelsPerModule { get; set; } = 20;

    public static bool IsValidErrorCorrection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Trim().ToUpperInvariant() is "L" or "M" or "Q" or "H";
    }
}

