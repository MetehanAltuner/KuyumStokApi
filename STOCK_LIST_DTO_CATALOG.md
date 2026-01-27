# Stock List DTO Catalog

## Last Updated (UTC)
- 2026-01-27T20:43:46Z
- QR payload kuralı "publicCode only" olarak netleştirildi.
- `StockDto.QrCode` formati ve anlami aciklandi.

Bu doküman stok listeleme endpoint'inin döndüğü DTO alanlarını ve her alanın kaynağını koddan çıkarır.

## Endpoint -> Service mapping

- **Endpoint**: `GET /api/Stocks`
  - Controller: `KuyumStokApi.API/Controllers/StocksController.cs` -> `GetPaged`
  - Service: `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs` -> `GetPagedAsync`

## DTO definition (list response)

- **DTO class**: `StockDto`
  - File: `KuyumStokApi.Application/DTOs/Stocks/StocksDto.cs`
  - `StockDto.VariantBrief` (nested)
  - `StockDto.BranchBrief` (nested)

## Query, filter, paging behavior

- `BranchId`: filtrede yoksa `ICurrentUserContext.BranchId` kullanilir; ikisi de yoksa 400.
- `Page`: en az 1.
- `PageSize`: 1..200 araliginda clamp edilir.
- `Query` (trim edilir):
  - `Stocks.Barcode` `ILIKE` ile aranir.
  - `Stocks.QrCode` `ILIKE` ile aranir.
  - `PublicCode`: once normalize edilir, sonra `Stocks.PublicCode` `ILIKE` ile aranir.
  - Varyant alanlari: `Name`, `Brand`, `Ayar`, `Color` `ILIKE` ile aranir.
- `ProductTypeId`, `ProductVariantId`, `GramMin`, `GramMax`, `UpdatedFromUtc`, `UpdatedToUtc` filtreleri uygulanir.
- Siralama: `UpdatedAt ?? CreatedAt` azalan, sonra `ProductVariantId` artan.

## Field table (list endpoint)

`StocksService.GetPagedAsync` direkt satirlari `StockDto`'ya map eder (grouping yoktur). Aşağıdaki tablo **liste endpoint'i** için **tam alan setini** ve **kaynağını** gösterir.

| Property (type) | Source in code | DB field / calculation | Notes |
|---|---|---|---|
| `StockDto.Id` (`Guid`) | `StocksService.GetPagedAsync` | `Stocks.Id` | Tekil stok kaydidir. |
| `StockDto.Quantity` (`int?`) | `StocksService.GetPagedAsync` | `Stocks.Quantity` | Null olabilir. |
| `StockDto.Barcode` (`string`) | `StocksService.GetPagedAsync` | `Stocks.Barcode` (null ise `""`) | `Barcode ?? string.Empty` ile set edilir. |
| `StockDto.QrCode` (`string?`) | `StocksService.GetPagedAsync` | `Stocks.QrCode` | **Base64 kodlu PNG** goruntu; icerik **yalnizca `publicCode`** payload'idir. |
| `StockDto.PublicCode` (`string?`) | `StocksService.GetPagedAsync` | `Stocks.PublicCode` | **QR payload'inin kendisi**; normalize edilmis Crockford Base32 formatinda saklanir. |
| `StockDto.CreatedAt` (`DateTime?`) | `StocksService.GetPagedAsync` | `Stocks.CreatedAt` | |
| `StockDto.UpdatedAt` (`DateTime?`) | `StocksService.GetPagedAsync` | `Stocks.UpdatedAt` | |
| `StockDto.TotalWeight` (`decimal`) | `StocksService.GetPagedAsync` | `Stocks.TotalWeightGram` | Stok satirinin toplam gram degeri. |
| `StockDto.WorkmanshipMilyem` (`int?`) | `StocksService.GetPagedAsync` | `Stocks.WorkmanshipMilyem` | |
| `StockDto.TotalMilyem` (`int?`) | `StocksService.GetPagedAsync` | `AyarMilyemHelper` + `WorkmanshipMilyem` | Ham milyem ve iscilik (varsa) toplanir. |
| `StockDto.Branch.Id` (`int?`) | `StocksService.GetPagedAsync` | `Branches.Id` | `Stocks.BranchId` uzerinden join. |
| `StockDto.Branch.Name` (`string?`) | `StocksService.GetPagedAsync` | `Branches.Name` | |
| `StockDto.ProductVariant.Id` (`int?`) | `StocksService.GetPagedAsync` | `ProductVariants.Id` | `Stocks.ProductVariantId` uzerinden join. |
| `StockDto.ProductVariant.Name` (`string?`) | `StocksService.GetPagedAsync` | `ProductVariants.Name` | |
| `StockDto.ProductVariant.Ayar` (`string?`) | `StocksService.GetPagedAsync` | `ProductVariants.Ayar` | |
| `StockDto.ProductVariant.Color` (`string?`) | `StocksService.GetPagedAsync` | `ProductVariants.Color` | |
| `StockDto.ProductVariant.Brand` (`string?`) | `StocksService.GetPagedAsync` | `ProductVariants.Brand` | |
| `StockDto.ProductVariant.Gram` (`decimal?`) | `StocksService.GetPagedAsync` | `Stocks.Gram` | Varyant gram alani degil, stok satirinin gram alanidir. |
| `StockDto.ProductVariant.ProductTypeId` (`int?`) | `StocksService.GetPagedAsync` | `ProductTypes.Id` | `ProductVariants.ProductTypeId` uzerinden join. |
| `StockDto.ProductVariant.ProductTypeName` (`string?`) | `StocksService.GetPagedAsync` | `ProductTypes.Name` | |
| `StockDto.ProductVariant.CategoryName` (`string?`) | `StocksService.GetPagedAsync` | `ProductCategories.Name` | |

## Notes

- Liste endpoint'i grup/aggregation yapmaz; her satir bir `Stocks` kaydidir.
- `TotalMilyem`, `AyarMilyemHelper.GetMilyemFromAyar` ile hesaplanan ham milyem + `WorkmanshipMilyem` toplamidir. Ham veya iscilik yoksa `null` kalir.
- `PublicCode` tarama sonucu payload degeridir; frontend bunu link/yonlendirmeye cevirir.
- QR payload **tam olarak** `PublicCode` string'idir (raw short code); URL embed edilmez.
- `QrCode`, `PublicCode` degerinin PNG goruntusunun base64 encoding'idir; URL icermez.

