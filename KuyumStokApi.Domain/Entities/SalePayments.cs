using System;

namespace KuyumStokApi.Domain.Entities;

/// <summary>
/// Satış fişi ödeme detayları (çoklu ödeme desteği).
/// Bir satış fişi birden fazla ödeme yöntemiyle ödenebilir (Nakit + EFT + POS).
/// </summary>
public partial class SalePayments
{
    /// <summary>Birincil anahtar</summary>
    public int Id { get; set; }

    /// <summary>Hangi satış fişine ait (Foreign Key → Sales)</summary>
    public int? SaleId { get; set; }

    /// <summary>Ödeme yöntemi (Foreign Key → PaymentMethods)</summary>
    public int? PaymentMethodId { get; set; }

    /// <summary>Bu ödeme yöntemiyle ödenen tutar</summary>
    public decimal Amount { get; set; }

    /// <summary>Ödeme tarihi</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Güncelleme tarihi</summary>
    public DateTime? UpdatedAt { get; set; }

    // POS ödemesi için ek alanlar
    /// <summary>Banka (POS ise) (Foreign Key → Banks)</summary>
    public int? BankId { get; set; }

    /// <summary>POS komisyon oranı (örn: 0.025 = %2.5)</summary>
    public decimal? CommissionRate { get; set; }

    /// <summary>Komisyon düşüldükten sonra net tutar</summary>
    public decimal? NetAmount { get; set; }

    // Navigation Properties
    /// <summary>Bağlı olduğu satış fişi</summary>
    public virtual Sales? Sale { get; set; }

    /// <summary>Ödeme yöntemi</summary>
    public virtual PaymentMethods? PaymentMethod { get; set; }

    /// <summary>Banka (POS ödemesi ise)</summary>
    public virtual Banks? Bank { get; set; }
}

