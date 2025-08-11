using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.ProductVariant
{
    // Application/DTOs/ProductVariants/ProductVariantDtos.cs
    namespace KuyumStokApi.Application.DTOs.ProductVariants
    {
        /// <summary>Ürün varyantı (ör. yüzük, gram altın vb.) veri transfer nesnesi.</summary>
        public sealed class ProductVariantDto
        {
            /// <summary>Varyant benzersiz kimliği.</summary>
            public int Id { get; set; }

            /// <summary>Bağlı olduğu ürün türü özeti.</summary>
            public ProductTypeBrief? ProductType { get; set; }

            /// <summary>Gram bilgisi.</summary>
            public decimal? Gram { get; set; }

            /// <summary>Kalınlık (mm vb.).</summary>
            public decimal? Thickness { get; set; }

            /// <summary>Genişlik (mm vb.).</summary>
            public decimal? Width { get; set; }

            /// <summary>Taş tipi (varsa).</summary>
            public string? StoneType { get; set; }

            /// <summary>Karat (varsa).</summary>
            public decimal? Carat { get; set; }

            /// <summary>Milyem değeri (varsa).</summary>
            public int? Milyem { get; set; }

            /// <summary>Ayar bilgisi (ör. 14K, 22K).</summary>
            public string? Ayar { get; set; }

            /// <summary>Marka bilgisi.</summary>
            public string? Brand { get; set; }

            /// <summary>Oluşturulma zamanı (UTC).</summary>
            public DateTime? CreatedAt { get; set; }

            /// <summary>Güncellenme zamanı (UTC).</summary>
            public DateTime? UpdatedAt { get; set; }

            /// <summary>Kayıt aktif mi?</summary>
            public bool IsActive { get; set; }

            /// <summary>Yumuşak silinmiş mi?</summary>
            public bool IsDeleted { get; set; }

            /// <summary>Ürün türü özet modeli.</summary>
            public sealed class ProductTypeBrief
            {
                /// <summary>Ürün türü kimliği.</summary>
                public int? Id { get; set; }

                /// <summary>Ürün türü adı.</summary>
                public string? Name { get; set; }
            }
        }

        /// <summary>Yeni ürün varyantı oluşturma modeli.</summary>
        public sealed class ProductVariantCreateDto
        {
            /// <summary>Bağlı ürün türü (opsiyonel).</summary>
            public int? ProductTypeId { get; set; }

            public decimal? Gram { get; set; }
            public decimal? Thickness { get; set; }
            public decimal? Width { get; set; }
            public string? StoneType { get; set; }
            public decimal? Carat { get; set; }
            public int? Milyem { get; set; }
            public string? Ayar { get; set; }
            public string? Brand { get; set; }
        }

        /// <summary>Ürün varyantı güncelleme modeli.</summary>
        public sealed class ProductVariantUpdateDto
        {
            /// <summary>Bağlı ürün türü (opsiyonel).</summary>
            public int? ProductTypeId { get; set; }

            public decimal? Gram { get; set; }
            public decimal? Thickness { get; set; }
            public decimal? Width { get; set; }
            public string? StoneType { get; set; }
            public decimal? Carat { get; set; }
            public int? Milyem { get; set; }
            public string? Ayar { get; set; }
            public string? Brand { get; set; }
        }

        /// <summary>Varyantlar için filtre/sayfalama parametreleri.</summary>
        public sealed record ProductVariantFilter(
            int Page = 1,
            int PageSize = 20,
            string? Query = null,           // Brand/Ayar/StoneType gibi alanlarda arama yapacağız
            int? ProductTypeId = null,
            bool? IsActive = null,
            bool IncludeDeleted = false,
            DateTime? UpdatedFromUtc = null,
            DateTime? UpdatedToUtc = null
        );
    }

}
