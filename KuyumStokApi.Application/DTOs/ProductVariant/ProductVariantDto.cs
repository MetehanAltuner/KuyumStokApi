using System;

namespace KuyumStokApi.Application.DTOs.ProductVariant
{
    namespace KuyumStokApi.Application.DTOs.ProductVariants
    {
        /// <summary>Ürün varyantı (model) DTO.</summary>
        public sealed class ProductVariantDto
        {
            /// <summary>Varyant benzersiz kimliği.</summary>
            public int Id { get; set; }

            /// <summary>Bağlı olduğu ürün türü özeti.</summary>
            public ProductTypeBrief? ProductType { get; set; }

            /// <summary>Model adı (örn. Ajda Bilezik) - ZORUNLU.</summary>
            public string Name { get; set; } = default!;

            /// <summary>Ayar bilgisi (ör. 14, 18, 22, 24, 925, Pt950).</summary>
            public string? Ayar { get; set; }

            /// <summary>Renk (örn. Sarı, Beyaz, Rose).</summary>
            public string? Color { get; set; }

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

                /// <summary>Kategori kimliği (malzeme).</summary>
                public int? CategoryId { get; set; }

                /// <summary>Kategori adı (Altın, Gümüş, Platin, Pırlanta...).</summary>
                public string? CategoryName { get; set; }
            }
        }

        /// <summary>Yeni ürün varyantı oluşturma modeli.</summary>
        public sealed class ProductVariantCreateDto
        {
            /// <summary>Bağlı ürün türü (zorunlu).</summary>
            public int? ProductTypeId { get; set; }

            /// <summary>Model adı (örn. Ajda Bilezik) - ZORUNLU.</summary>
            public string Name { get; set; } = default!;

            public string? Ayar { get; set; }
            public string? Color { get; set; }
            public string? Brand { get; set; }
        }

        /// <summary>Ürün varyantı güncelleme modeli.</summary>
        public sealed class ProductVariantUpdateDto
        {
            /// <summary>Bağlı ürün türü (zorunlu).</summary>
            public int? ProductTypeId { get; set; }

            /// <summary>Model adı (örn. Ajda Bilezik) - ZORUNLU.</summary>
            public string Name { get; set; } = default!;

            public string? Ayar { get; set; }
            public string? Color { get; set; }
            public string? Brand { get; set; }
        }

        /// <summary>Varyantlar için filtre/sayfalama parametreleri.</summary>
        public sealed record ProductVariantFilter(
            int Page = 1,
            int PageSize = 20,
            string? Query = null,           // Name/Brand/Ayar/Color alanlarında arar
            int? ProductTypeId = null,
            bool? IsActive = null,
            bool IncludeDeleted = false,
            DateTime? UpdatedFromUtc = null,
            DateTime? UpdatedToUtc = null
        );
    }
}
