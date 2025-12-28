namespace KuyumStokApi.Infrastructure.QrCode;

/// <summary>QR kod yapılandırma seçenekleri.</summary>
public sealed class QrCodeOptions
{
    /// <summary>QR kod içinde kullanılacak base URL (örn: http://130.162.231.124/saleReceipt/)</summary>
    public string BaseUrl { get; set; } = string.Empty;
}

