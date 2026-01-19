# Stock List DTO Catalog

Bu doküman stok listeleme endpoint’inin döndüğü DTO şeklinin alanlarını, her alanın kaynağını ve türetme/aggregation kurallarını koddan çıkarır.

## Endpoint → Service mapping

- **Endpoint**: `GET /api/Stocks`  
  - Controller: `KuyumStokApi.API/Controllers/StocksController.cs` → `GetPaged`  
  - Service: `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs` → `GetPagedAsync`

## DTO definition (list response)

- **DTO class**: `StockDto`  
  - File: `KuyumStokApi.Application/DTOs/Stocks/StocksDto.cs`
  - `StockDto.VariantBrief` (nested)
  - `StockDto.BranchBrief` (nested)

## Field table (list endpoint)

`StocksService.GetPagedAsync` `groupedItems` listesini `StockDto`'ya map eder. Aşağıdaki tablo **liste endpoint’i** için **tam alan setini** ve **kaynağını** gösterir.

| Property (type) | Source in code | DB field / calculation | Grouped / aggregated? | Notes |
|---|---|---|---|---|
| `StockDto.Id` (`Guid`) | `StocksService.GetPagedAsync` | `Guid.Empty` **hardcoded** | N/A | Gruplama nedeniyle tekil stok id yok; **aşağıda “Neden Id yok?”** |
| `StockDto.Quantity` (`int?`) | `StocksService.GetPagedAsync` | `TotalQuantity = Sum(Stocks.Quantity)` | **Aggregated** | `grouped` projection: `g.Sum(z => z.s.Quantity ?? 0)` |
| `StockDto.Barcode` (`string`) | `StocksService.GetPagedAsync` | `string.Empty` **hardcoded** | N/A | Gruplama nedeniyle tek barkod yok |
| `StockDto.QrCode` (`string?`) | `StocksService.GetPagedAsync` | İlk QR kodlu stok kaydı (`Stocks.QrCode`) | **Derived** | `qrCodeMap` ile grup bazlı ilk `QrCode` |
| `StockDto.CreatedAt` (`DateTime?`) | `StocksService.GetPagedAsync` | `FirstCreatedAt = Min(Stocks.CreatedAt)` | **Aggregated** | `DateTime.MinValue` ise `null` |
| `StockDto.UpdatedAt` (`DateTime?`) | `StocksService.GetPagedAsync` | `LastUpdatedAt = Max(Stocks.UpdatedAt ?? Stocks.CreatedAt)` | **Aggregated** | `DateTime.MinValue` ise `null` |
| `StockDto.TotalWeight` (`decimal`) | `StocksService.GetPagedAsync` | `Sum(Stocks.Gram * Stocks.Quantity)` | **Aggregated** | `g.Sum(z => (z.s.Gram ?? 0) * (decimal)(z.s.Quantity ?? 0))` |
| `StockDto.WorkmanshipMilyem` (`int?`) | `StocksService.GetPagedAsync` | `null` **hardcoded** | N/A | Gruplama nedeniyle tek işçilik milyem yok |
| `StockDto.TotalMilyem` (`int?`) | `StocksService.GetPagedAsync` | `AyarMilyemHelper.GetMilyemFromAyar(VariantAyar)` | **Derived** | Sadece ham milyem; işçilik eklenmez |
| `StockDto.Branch.Id` (`int?`) | `StocksService.GetPagedAsync` | `BranchId` | **Grouped key** | `grouped` key: `x.s.BranchId` |
| `StockDto.Branch.Name` (`string?`) | `StocksService.GetPagedAsync` | `BranchName` | **Grouped key** | `join b in Branches` |
| `StockDto.ProductVariant.Id` (`int?`) | `StocksService.GetPagedAsync` | `ProductVariantId` | **Grouped key** | `grouped` key: `x.s.ProductVariantId` |
| `StockDto.ProductVariant.Name` (`string?`) | `StocksService.GetPagedAsync` | `VariantName` | **Grouped key** | `ProductVariants.Name` |
| `StockDto.ProductVariant.Ayar` (`string?`) | `StocksService.GetPagedAsync` | `VariantAyar` | **Grouped key** | `ProductVariants.Ayar` |
| `StockDto.ProductVariant.Color` (`string?`) | `StocksService.GetPagedAsync` | `VariantColor` | **Grouped key** | `ProductVariants.Color` |
| `StockDto.ProductVariant.Brand` (`string?`) | `StocksService.GetPagedAsync` | `VariantBrand` | **Grouped key** | `ProductVariants.Brand` |
| `StockDto.ProductVariant.Gram` (`decimal?`) | `StocksService.GetPagedAsync` | `null` **hardcoded** | N/A | Gruplama nedeniyle tek gram yok |
| `StockDto.ProductVariant.ProductTypeId` (`int?`) | `StocksService.GetPagedAsync` | `TypeId` | **Grouped key** | `ProductTypes.Id` |
| `StockDto.ProductVariant.ProductTypeName` (`string?`) | `StocksService.GetPagedAsync` | `TypeName` | **Grouped key** | `ProductTypes.Name` |
| `StockDto.ProductVariant.CategoryName` (`string?`) | `StocksService.GetPagedAsync` | `CategoryName` | **Grouped key** | `ProductCategories.Name` |

## Grouping/aggregation rules (list endpoint)

`StocksService.GetPagedAsync` stokları **`ProductVariantId + BranchId`** bazında gruplar ve **variant bilgilerini key’e ekler**:

```92:128:KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs
var grouped =
    from x in baseQ
    group x by new {
        ProductVariantId = x.s.ProductVariantId,
        BranchId = x.s.BranchId,
        VariantName = x.v != null ? x.v.Name : null,
        VariantAyar = x.v != null ? x.v.Ayar : null,
        VariantColor = x.v != null ? x.v.Color : null,
        VariantBrand = x.v != null ? x.v.Brand : null,
        TypeId = x.t != null ? x.t.Id : (int?)null,
        TypeName = x.t != null ? x.t.Name : null,
        CategoryName = x.c != null ? x.c.Name : null,
        BranchName = x.b != null ? x.b.Name : null
    } into g
    select new
    {
        TotalQuantity = g.Sum(z => z.s.Quantity ?? 0),
        TotalWeight = g.Sum(z => (z.s.Gram ?? 0) * (decimal)(z.s.Quantity ?? 0)),
        LastUpdatedAt = g.Max(z => z.s.UpdatedAt ?? z.s.CreatedAt ?? DateTime.MinValue),
        FirstCreatedAt = g.Min(z => z.s.CreatedAt ?? DateTime.MinValue)
    };
```

## Neden `Id` / `StockId` listede yok?

Liste endpoint’i **tekil `Stocks.Id` döndürmez**; `StockDto.Id` **`Guid.Empty` olarak set edilir**. Bu durum, stokların **`ProductVariantId + BranchId` bazında gruplandığı** için tekil bir stok kimliği bulunmamasından kaynaklanır.

```195:204:KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs
return new StockDto
{
    Id = Guid.Empty, // Gruplama yapıldığı için ID anlamsız, UI'da kullanılmıyor
    Quantity = x.TotalQuantity,
    Barcode = string.Empty, // Gruplama yapıldığı için tek bir barkod yok
    QrCode = qrCode,
    CreatedAt = x.FirstCreatedAt != DateTime.MinValue ? x.FirstCreatedAt : null,
    UpdatedAt = x.LastUpdatedAt != DateTime.MinValue ? x.LastUpdatedAt : null,
    TotalWeight = x.TotalWeight,
    WorkmanshipMilyem = null,
    TotalMilyem = hamMilyem,
    // ...
};
```

## Field origins (by DTO file)

- DTO tanımı: `KuyumStokApi.Application/DTOs/Stocks/StocksDto.cs`
- Liste map işlemi: `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs` (`GetPagedAsync`)

## Notes

- `StockDto.TotalMilyem`, listede **sadece ham milyem** olarak hesaplanır (`AyarMilyemHelper.GetMilyemFromAyar`).  
  - İşçilik milyem listede `null` döner; detay endpoint’inde `WorkmanshipMilyem + ham` hesaplanır.  
  - `StocksService.GetPagedAsync`, `StocksService.MapToDto`
- `StockDto.QrCode` listede grup içindeki **ilk QR kodlu stoktan** gelir.  
  - `qrCodeMap` logic: `StocksService.GetPagedAsync`

