using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Banks
{
    /// <summary>Yeni banka oluşturmak için veri.</summary>
    public sealed class BankCreateDto
    {
        /// <summary>Banka adı (zorunlu).</summary>
        public string Name { get; set; } = default!;
        /// <summary>Açıklama (opsiyonel).</summary>
        public string? Description { get; set; }
    }

    /// <summary>Var olan bankayı güncellemek için veri.</summary>
    public sealed class BankUpdateDto
    {
        /// <summary>Banka adı (zorunlu).</summary>
        public string Name { get; set; } = default!;
        /// <summary>Açıklama (opsiyonel).</summary>
        public string? Description { get; set; }
    }

    /// <summary>API’nin liste/detay döndürdüğü model.</summary>
    public sealed class BankDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    /// <summary>Listeleme için filtre + sayfalama parametreleri.</summary>
    public sealed record BankFilter(
        int Page = 1,
        int PageSize = 20,
        string? Query = null,
        bool? IsActive = null,
        bool IncludeDeleted = false,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );
}
