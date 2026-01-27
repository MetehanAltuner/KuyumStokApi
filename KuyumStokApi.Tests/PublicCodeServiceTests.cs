using KuyumStokApi.Infrastructure.Services.PublicCodeService;
using Xunit;

namespace KuyumStokApi.Tests
{
    public sealed class PublicCodeServiceTests
    {
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        [Fact]
        public void GenerateStockPublicCode_UsesAllowedChars_AndLength()
        {
            var service = new PublicCodeService();

            var code = service.GenerateStockPublicCode(10);

            Assert.Equal(10, code.Length);
            Assert.All(code, ch => Assert.Contains(ch, Alphabet));
        }

        [Theory]
        [InlineData("8f3k-2d9m-pq", "8F3K2D9MPQ")]
        [InlineData("  oiL  ", "011")]
        [InlineData("ab cd", "ABCD")]
        public void Normalize_RemovesSeparatorsAndUppercases(string input, string expected)
        {
            var service = new PublicCodeService();

            var normalized = service.Normalize(input);

            Assert.Equal(expected, normalized);
        }

        [Fact]
        public void IsValid_ValidatesLengthAndAlphabet()
        {
            var service = new PublicCodeService();

            Assert.True(service.IsValid("8F3K2D9MPQ"));
            Assert.False(service.IsValid("8F3K2D9MP"));
            Assert.False(service.IsValid("INVALIDUUU"));
        }
    }
}
