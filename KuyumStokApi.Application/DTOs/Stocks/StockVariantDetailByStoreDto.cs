using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Stocks
{
    /// <summary>Seçili varyant için, kullanıcının mağazasındaki tüm şubelerdeki stok özeti.</summary>
    public sealed class StockVariantDetailByStoreDto
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; } = default!;
        public string? Ayar { get; set; }
        public string? Color { get; set; }

        public List<BranchBlock> Branches { get; set; } = new();

        public sealed class BranchBlock
        {
            public int BranchId { get; set; }
            public string BranchName { get; set; } = default!;
            public int ToplamAdet { get; set; }
            public decimal ToplamAgirlik { get; set; }
        }
    }
}
