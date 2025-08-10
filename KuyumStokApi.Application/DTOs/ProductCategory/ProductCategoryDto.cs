using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.ProductCategories
{
    public class ProductCategoryCreateDto
    {
        public string Name { get; set; } = default!;
    }

    public class ProductCategoryUpdateDto
    {
        public string Name { get; set; } = default!;
    }

    /// <summary>
    /// Ürün kategorisi bilgilerini temsil eder.
    /// </summary>
    public class ProductCategoryDto
    {
        /// <summary>
        /// Kategori benzersiz ID değeri.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Kategori adı.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Kategorinin oluşturulma tarihi.
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Kategorinin son güncellenme tarihi.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
