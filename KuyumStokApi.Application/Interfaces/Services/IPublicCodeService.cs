namespace KuyumStokApi.Application.Interfaces.Services
{
    /// <summary>Public code üretim ve doğrulama sözleşmesi.</summary>
    public interface IPublicCodeService
    {
        string GenerateStockPublicCode(int length = 10);
        string Normalize(string input);
        bool IsValid(string normalizedCode);
    }
}
