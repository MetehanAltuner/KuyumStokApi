using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Stores
{
    /// <summary>Mağaza DTO'su.</summary>
    public sealed class StoreDto
    {
        /// <summary>Mağaza kimliği.</summary>
        public int Id { get; set; }

        /// <summary>Mağaza adı.</summary>
        public string? Name { get; set; }

        /// <summary>Oluşturulma UTC.</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>Güncellenme UTC.</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Kayıt aktif mi?</summary>
        public bool IsActive { get; set; }

        /// <summary>Yumuşak silinmiş mi?</summary>
        public bool IsDeleted { get; set; }

        /// <summary>Bu mağazaya bağlı şube sayısı (özet bilgi).</summary>
        public int BranchCount { get; set; }
    }

    /// <summary>Mağaza oluşturma modeli.</summary>
    public sealed class StoreCreateDto
    {
        public string Name { get; set; } = null!;
    }

    /// <summary>Mağaza güncelleme modeli.</summary>
    public sealed class StoreUpdateDto
    {
        public string Name { get; set; } = null!;
    }

    /// <summary>Mağazalar için filtre/sayfalama.</summary>
    public sealed record StoreFilter(
        int Page = 1,
        int PageSize = 20,
        string? Query = null,      // name araması
        bool? IsActive = null,
        bool IncludeDeleted = false,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );
}
