using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.ProductLifecycles
{
    public sealed class ProductLifecycleDto
    {
        public int Id { get; set; }
        public int? StockId { get; set; }
        public int? UserId { get; set; }
        public int? ActionId { get; set; }
        public string? Note { get; set; }
        public DateTime? Timestamp { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Görsel alanlar
        public string? ActionName { get; set; }
        public string? UserName { get; set; }
        public string? StockBarcode { get; set; }
        public int? BranchId { get; set; }
        public int? ProductVariantId { get; set; }
    }

    public sealed class ProductLifecycleCreateDto
    {
        public int StockId { get; set; }
        public int ActionId { get; set; }
        public string? Note { get; set; }
        // user_id'i servis CurrentUserService ile dolduracağız
        public DateTime? Timestamp { get; set; } // null gelirse UtcNow
    }

    public sealed class ProductLifecycleFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public int? StockId { get; set; }
        public int? ActionId { get; set; }
        public int? UserId { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
    }
}
