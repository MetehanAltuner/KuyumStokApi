using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Sales
{
    /// <summary>Satış kalemi (stok çıkış) DTO’su.</summary>
    public sealed class SaleItemDto
    {
        public int StockId { get; set; }         // hangi stok satılıyor
        public int Quantity { get; set; }        // -adet
        public decimal SoldPrice { get; set; }   // detay tablosu için
    }

    /// <summary>Satış fişi oluşturma DTO’su.</summary>
    public sealed class SaleCreateDto
    {
        public int? UserId { get; set; }          // yoksa CurrentUser
        public int BranchId { get; set; }

        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerNationalId { get; set; } // T.C. alanı (customers’da karşılığı yoksa sadece notta tutabiliriz)

        public int? PaymentMethodId { get; set; } // Nakit/EFT/POS tek seçim
        public int? BankId { get; set; }          // POS ise
        public decimal? CommissionRate { get; set; }
        public decimal? ExpectedAmount { get; set; }

        public List<SaleItemDto> Items { get; set; } = new();
    }


    public sealed class SaleResultDto
    {
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IReadOnlyList<int> StockIds { get; set; } = Array.Empty<int>();
    }
    public sealed class SaleFilter
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        public int? BranchId { get; init; }
        public int? UserId { get; init; }
        public int? CustomerId { get; init; }
        public int? PaymentMethodId { get; init; }

        public DateTime? FromUtc { get; init; }
        public DateTime? ToUtc { get; init; }
    }

    public sealed class SaleListDto
    {
        public int SaleId { get; init; }          // İşlem butonları için
        public int LineId { get; init; }          // sale_details.id
        public DateTime? CreatedAt { get; init; } // s.CreatedAt
        public int? BranchId { get; init; }
        public string? BranchName { get; init; }
        public int? UserId { get; init; }
        public string? UserName { get; init; }
        public int StockId { get; init; }
        public string? ProductName { get; init; } // pv.Name
        public string? Ayar { get; init; }        // pv.Ayar
        public string? Renk { get; init; }        // pv.Color
        public decimal? AgirlikGram { get; init; } // st.Gram
        public int Quantity { get; init; }
        public decimal? SoldPrice { get; init; }
    }

    public sealed class SaleLineDetailDto
    {
        public int SaleId { get; init; }
        public int LineId { get; init; }          // sale_details.id
        public DateTime? CreatedAt { get; init; } // s.CreatedAt (UI’da tarih istersen)
        public string? PaymentMethod { get; init; }

        public int StockId { get; init; }
        public string? ProductName { get; init; } // pv.Name
        public string? Ayar { get; init; }        // pv.Ayar
        public string? Renk { get; init; }        // pv.Color
        public decimal? AgirlikGram { get; init; } // st.Gram

        public decimal? ListeFiyati { get; init; } // Şu an şemada yok => null
        public decimal? SatisFiyati { get; init; } // d.SoldPrice
    }
}
