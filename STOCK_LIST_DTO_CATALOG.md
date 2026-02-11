# Stock List DTO Catalog

## Last Updated (UTC)
- 2026-02-11
- Kod tabanı tam tarandı; mevcut `GetPagedAsync` davranışı dokümante edildi.

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

- `BranchId`: filtrede yoksa `ICurrentUserContext.BranchId` kullanılır; ikisi de yoksa 400.
- `Page`: en az 1 (`Math.Max(1, filter.Page)`).
- `PageSize`: 1..200 aralığında clamp edilir (`Math.Clamp(filter.PageSize, 1, 200)`).
- `Query` (trim edilir):
  - `Stocks.Barcode` `ILIKE` ile aranır.
  - `Stocks.QrCode` `ILIKE` ile aranır (DB'de Base64 PNG saklandığı için pratikte nadiren eşleşir).
  - `PublicCode`: `PublicCodeService.Normalize` ile normalize edilir; normalize sonucu boş değilse `Stocks.PublicCode` `ILIKE` ile aranır.
  - Varyant alanları: `Name`, `Brand`, `Ayar`, `Color` `ILIKE` ile aranır.
- `ProductTypeId`, `ProductVariantId`, `GramMin`, `GramMax`, `UpdatedFromUtc`, `UpdatedToUtc` filtreleri uygulanır.
- Gram filtreleri `Stocks.Gram` üzerinden uygulanır (birim gram, `TotalWeightGram` değil).
- Sıralama: `UpdatedAt ?? CreatedAt` azalan, sonra `ProductVariantId` artan.

## Field table (list endpoint)

`StocksService.GetPagedAsync` direkt satırları `StockDto`'ya map eder (grouping yoktur). Aşağıdaki tablo **liste endpoint'i** için **tam alan setini** ve **kaynağını** gösterir.

| Property (type) | Source in code | DB field / calculation | Notes |
|---|---|---|---|
| `StockDto.Id` (`Guid`) | `StocksService.GetPagedAsync` | `Stocks.Id` | Tekil stok kaydıdır. |
| `StockDto.Quantity` (`int?`) | `StocksService.GetPagedAsync` | `Stocks.Quantity` | Null olabilir. |
| `StockDto.Barcode` (`string`) | `StocksService.GetPagedAsync` | `Stocks.Barcode` (null ise `""`) | `Barcode ?? string.Empty` ile set edilir. |
| `StockDto.QrCode` (`string?`) | `StocksService.GetPagedAsync` | `Stocks.QrCode` | **Base64 kodlu PNG** görüntü; içerik **yalnızca `publicCode`** payload'idir. |
| `StockDto.PublicCode` (`string?`) | `StocksService.GetPagedAsync` | `Stocks.PublicCode` | **QR payload'inin kendisi**; normalize edilmiş Crockford Base32 formatında saklanır (10 karakter). |
| `StockDto.CreatedAt` (`DateTime?`) | `StocksService.GetPagedAsync` | `Stocks.CreatedAt` | |
| `StockDto.UpdatedAt` (`DateTime?`) | `StocksService.GetPagedAsync` | `Stocks.UpdatedAt` | |
| `StockDto.TotalWeight` (`decimal`) | `StocksService.GetPagedAsync` | `Stocks.TotalWeightGram` | Stok satırının toplam gram değeri. |
| `StockDto.WorkmanshipMilyem` (`int?`) | `StocksService.GetPagedAsync` | `Stocks.WorkmanshipMilyem` | |
| `StockDto.TotalMilyem` (`int?`) | `StocksService.GetPagedAsync` | `AyarMilyemHelper.GetMilyemFromAyar` + `WorkmanshipMilyem` | Ham milyem (varyant Ayar) ve işçilik (varsa) toplanır. |
| `StockDto.Branch.Id` (`int?`) | `StocksService.GetPagedAsync` | `Branches.Id` | `Stocks.BranchId` üzerinden join. |
| `StockDto.Branch.Name` (`string?`) | `StocksService.GetPagedAsync` | `Branches.Name` | |
| `StockDto.ProductVariant.Id` (`int?`) | `StocksService.GetPagedAsync` | `ProductVariants.Id` | `Stocks.ProductVariantId` üzerinden join. |
| `StockDto.ProductVariant.Name` (`string?`) | `StocksService.GetPagedAsync` | `ProductVariants.Name` | |
| `StockDto.ProductVariant.Ayar` (`string?`) | `StocksService.GetPagedAsync` | `ProductVariants.Ayar` | |
| `StockDto.ProductVariant.Color` (`string?`) | `StocksService.GetPagedAsync` | `ProductVariants.Color` | |
| `StockDto.ProductVariant.Brand` (`string?`) | `StocksService.GetPagedAsync` | `ProductVariants.Brand` | |
| `StockDto.ProductVariant.Gram` (`decimal?`) | `StocksService.GetPagedAsync` | `Stocks.Gram` | Varyant gram alanı değil, stok satırının gram alanıdır. |
| `StockDto.ProductVariant.ProductTypeId` (`int?`) | `StocksService.GetPagedAsync` | `ProductTypes.Id` | `ProductVariants.ProductTypeId` üzerinden join. |
| `StockDto.ProductVariant.ProductTypeName` (`string?`) | `StocksService.GetPagedAsync` | `ProductTypes.Name` | |
| `StockDto.ProductVariant.CategoryName` (`string?`) | `StocksService.GetPagedAsync` | `ProductCategories.Name` | |

## Notes

- Liste endpoint'i grup/aggregation yapmaz; her satır bir `Stocks` kaydıdır.
- `TotalMilyem`, `AyarMilyemHelper.GetMilyemFromAyar` ile hesaplanan ham milyem + `WorkmanshipMilyem` toplamıdır. Ham veya işçilik yoksa `null` kalır.
- `PublicCode` tarama sonucu payload değeridir; frontend bunu link/yönlendirmeye çevirir.
- QR payload **tam olarak** `PublicCode` string'idir (raw short code); URL embed edilmez.
- `QrCode`, `PublicCode` değerinin PNG görüntüsünün base64 encoding'idir; URL içermez.
- `TotalWeight` = `Stocks.TotalWeightGram` (satır toplam gram), `ProductVariant.Gram` = `Stocks.Gram` (birim gram).
