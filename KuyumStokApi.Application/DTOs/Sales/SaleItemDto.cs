using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KuyumStokApi.Application.Validation.Attributes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.DTOs.Sales
{
    /// <summary>Satış kalemi (stok çıkış) DTO’su.</summary>
    public sealed class SaleItemDto
    {
        public Guid StockId { get; set; }         // hangi stok satılıyor
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }        // -adet
        [GreaterThanZero(ErrorMessage = "SoldPrice must be greater than 0.")]
        public decimal SoldPrice { get; set; }   // detay tablosu için
        [GreaterThanZero(ErrorMessage = "TotalWeightGram must be greater than 0.")]
        public decimal TotalWeightGram { get; set; } // satır toplam ağırlık
    }

    /// <summary>Satış fişi oluşturma DTO'su (Çoklu Ödeme Destekli).</summary>
    public sealed class SaleCreateDto
    {
        // Müşteri Bilgileri
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerNationalId { get; set; }

        // Satış Kalemleri
        public List<SaleItemDto> Items { get; set; } = new();

        // Ödeme Bilgileri (Çoklu Ödeme)
        /// <summary>Nakit ödeme tutarı</summary>
        public decimal CashAmount { get; set; }

        /// <summary>EFT/Havale ödeme tutarı</summary>
        public decimal EftAmount { get; set; }

        /// <summary>POS (Kredi Kartı) ödeme tutarı</summary>
        public decimal PosAmount { get; set; }

        /// <summary>POS ödemesi için banka seçimi (Foreign Key → Banks)</summary>
        [Range(1, int.MaxValue, ErrorMessage = "POS_BankId must be greater than 0.")]
        public int? POS_BankId { get; set; }

        /// <summary>POS komisyon oranı (örn: 0.025 = %2.5)</summary>
        public decimal? POS_CommissionRate { get; set; }

        /// <summary>Toplam satış tutarı (Items'ların toplamı, doğrulama için)</summary>
        [GreaterThanZero(ErrorMessage = "TotalAmount must be greater than 0.")]
        public decimal TotalAmount { get; set; }

        // NOT: UserId ve BranchId artık CurrentUser'dan otomatik alınacak!
    }


    public sealed class SaleResultDto
    {
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IReadOnlyList<Guid> StockIds { get; set; } = Array.Empty<Guid>();
    }
    public sealed class SaleFilter
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        public int Page { get; init; } = 1;
        [Range(1, int.MaxValue, ErrorMessage = "PageSize must be greater than 0.")]
        public int PageSize { get; init; } = 20;

        [Range(1, int.MaxValue, ErrorMessage = "BranchId must be greater than 0.")]
        public int? BranchId { get; init; }
        [Range(1, int.MaxValue, ErrorMessage = "UserId must be greater than 0.")]
        public int? UserId { get; init; }
        [Range(1, int.MaxValue, ErrorMessage = "CustomerId must be greater than 0.")]
        public int? CustomerId { get; init; }
        [Range(1, int.MaxValue, ErrorMessage = "PaymentMethodId must be greater than 0.")]
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
        public Guid StockId { get; init; }
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

        public Guid StockId { get; init; }
        public string? ProductName { get; init; } // pv.Name
        public string? Ayar { get; init; }        // pv.Ayar
        public string? Renk { get; init; }        // pv.Color
        public decimal? AgirlikGram { get; init; } // st.Gram

        public decimal? ListeFiyati { get; init; } // Şu an şemada yok => null
        public decimal? SatisFiyati { get; init; } // d.SoldPrice
    }
}
