using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Branches
{
    /// <summary>Şube DTO'su.</summary>
    public sealed class BranchDto
    {
        /// <summary>Şube kimliği.</summary>
        public int Id { get; set; }

        /// <summary>Şube adı.</summary>
        public string? Name { get; set; }

        /// <summary>Adres.</summary>
        public string? Address { get; set; }

        /// <summary>Bağlı olduğu mağaza özeti.</summary>
        public StoreBrief? Store { get; set; }

        /// <summary>Oluşturulma UTC.</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>Güncellenme UTC.</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Kayıt aktif mi?</summary>
        public bool IsActive { get; set; }

        /// <summary>Yumuşak silinmiş mi?</summary>
        public bool IsDeleted { get; set; }

        /// <summary>Mağaza özet modeli.</summary>
        public sealed class StoreBrief
        {
            /// <summary>Mağaza kimliği.</summary>
            public int? Id { get; set; }

            /// <summary>Mağaza adı.</summary>
            public string? Name { get; set; }
        }
    }

    /// <summary>Şube oluşturma modeli.</summary>
    public sealed class BranchCreateDto
    {
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "StoreId must be greater than 0.")]
        public int? StoreId { get; set; }
    }

    /// <summary>Şube güncelleme modeli.</summary>
    public sealed class BranchUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "StoreId must be greater than 0.")]
        public int? StoreId { get; set; }
    }

    /// <summary>Şubeler için filtre/sayfalama.</summary>
    public sealed record BranchFilter(
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")] int Page = 1,
        [Range(1, int.MaxValue, ErrorMessage = "PageSize must be greater than 0.")] int PageSize = 20,
        string? Query = null,      // name/address serbest arama
        [Range(1, int.MaxValue, ErrorMessage = "StoreId must be greater than 0.")] int? StoreId = null,
        bool? IsActive = null,
        bool IncludeDeleted = false,
        DateTime? UpdatedFromUtc = null,
        DateTime? UpdatedToUtc = null
    );
}
