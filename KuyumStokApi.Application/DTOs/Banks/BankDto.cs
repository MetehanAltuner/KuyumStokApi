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
    }

    /// <summary>Listeleme için filtre + sayfalama parametreleri.</summary>
    public sealed class BankFilter
    {
        /// <summary>Ad veya açıklama içinde geçen metin (case-insensitive).</summary>
        public string? Query { get; set; }
        /// <summary>Güncellenme başlangıç tarihi (UTC).</summary>
        public DateTime? UpdatedFromUtc { get; set; }
        /// <summary>Güncellenme bitiş tarihi (UTC).</summary>
        public DateTime? UpdatedToUtc { get; set; }

        /// <summary>Sayfa numarası (1..n).</summary>
        public int Page { get; set; } = 1;
        /// <summary>Sayfa boyutu (1..200).</summary>
        public int PageSize { get; set; } = 20;
    }
}
