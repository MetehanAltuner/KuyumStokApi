namespace KuyumStokApi.Application.Interfaces.Services
{
    /// <summary>QR kod üretim sözleşmesi.</summary>
    public interface IQrCodeService
    {
        string GenerateQrPngBase64(string payload);
    }
}
