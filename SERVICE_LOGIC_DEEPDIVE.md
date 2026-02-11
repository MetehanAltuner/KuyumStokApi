# Service Logic Deep Dive

## Last Updated (UTC)
- 2026-02-11
- Kod tabanı tam tarandı; servis envanteri, akışlar ve doğrulama kuralları güncellendi.

Bu doküman, `KuyumStokApi.Infrastructure/Services/**` ve destekleyici katmanlardaki davranışı kod tabanlı olarak açıklar. Her iddia doğrudan koddan çıkarılmıştır; belirsiz alanlar "Unknown" olarak işaretlenmiştir.

## 1) Service Inventory (Index)

| Service | Interface | File path | Main responsibilities | Depends on |
|---|---|---|---|---|
| `BanksService` | `IBanksService` | `KuyumStokApi.Infrastructure/Services/BanksService/BanksService.cs` | Banka CRUD + soft/hard delete + aktif/pasif | `AppDbContext` |
| `BranchesService` | `IBranchesService` | `KuyumStokApi.Infrastructure/Services/BranchesService/BranchesService.cs` | Şube CRUD + guard'lar | `AppDbContext` |
| `CustomersService` | `ICustomersService` | `KuyumStokApi.Infrastructure/Services/CustomersService/CustomersService.cs` | Müşteri CRUD + satış/alış referans kontrolü | `AppDbContext` |
| `DashboardService` | `IDashboardService` | `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardService.cs` | Dashboard metrikleri + SignalR broadcast | `AppDbContext`, `ICurrentUserContext`, `IReportsService`, `AnomalyDetectionService`, `WorkloadEstimationService`, `IHubContext<DashboardHub>` |
| `DashboardNotificationService` | `IDashboardNotificationService` | `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardNotificationService.cs` | SaveChanges sonrası dashboard yayınları + NotifySaleCommittedAsync | `IHubContext<DashboardHub>`, `IDashboardService` |
| `DashboardBackgroundService` | `BackgroundService` | `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardBackgroundService.cs` | Periyodik dashboard broadcast (30s/1m/2m) | `IServiceProvider`, `IHubContext<DashboardHub>` |
| `JwtService` | `IJwtService` | `KuyumStokApi.Infrastructure/Services/JwtService/JwtService.cs` | JWT üretimi | `IOptions<JwtOptions>` |
| `RefreshTokenService` | `IRefreshTokenService` | `KuyumStokApi.Infrastructure/Services/RefreshTokenService/RefreshTokenService.cs` | Refresh token üretimi/rotate/revoke | `AppDbContext` |
| `TokenBlacklistService` | `ITokenBlacklistService` | `KuyumStokApi.Infrastructure/Services/TokenBlacklistService/TokenBlacklistService.cs` | JWT blacklist yönetimi | `AppDbContext` |
| `UserService` | `IUserService` | `KuyumStokApi.Infrastructure/Services/UserService/UserService.cs` | Register/login, user CRUD, password policy | `AppDbContext`, `IPasswordHasher`, `IJwtService`, `IRefreshTokenService` |
| `ProductCategoryService` | `IProductCategoryService` | `KuyumStokApi.Infrastructure/Services/ProductCategoryService/ProductCategoryService.cs` | Ürün kategorisi CRUD + uniqueness | `AppDbContext` |
| `ProductTypeService` | `IProductTypeService` | `KuyumStokApi.Infrastructure/Services/ProductTypService/ProductTypeService.cs` | Ürün türü CRUD + child guard | `AppDbContext` |
| `ProductVariantService` | `IProductVariantService` | `KuyumStokApi.Infrastructure/Services/ProductVariantService/ProductVariantService.cs` | Varyant CRUD + unique kombinasyon | `AppDbContext` |
| `StocksService` | `IStocksService` | `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs` | Stok listeleme, CRUD, favorites, publicCode + QR | `AppDbContext`, `ICurrentUserContext`, `IOptions<QrCodeOptions>`, `IPublicCodeService`, `IQrCodeService`, `ILogger` |
| `PublicCodeService` | `IPublicCodeService` | `KuyumStokApi.Infrastructure/Services/PublicCodeService/PublicCodeService.cs` | PublicCode üretim/normalize/validasyon (10 char Crockford Base32) | N/A |
| `QrCodeService` | `IQrCodeService` | `KuyumStokApi.Infrastructure/QrCode/QrCodeService.cs` | QR PNG Base64 üretimi | `IOptions<QrCodeOptions>` |
| `PurchasesService` | `IPurchasesService` | `KuyumStokApi.Infrastructure/Services/PurchasesService/PurchasesService.cs` | Alış fişi + stok artırma (barkod merge) + lifecycle | `AppDbContext` |
| `SalesService` | `ISalesService` | `KuyumStokApi.Infrastructure/Services/SalesService/SalesService.cs` | Birleşik fiş (satış+alış), stok düşme/ekleme (TotalWeightGram), ödeme | `AppDbContext`, `ICurrentUserContext`, `IStocksService`, `IDashboardNotificationService` |
| `ReportsService` | `IReportsService` | `KuyumStokApi.Infrastructure/Services/ReportsService/ReportsService.cs` | Raporlar + rol/şube scope | `AppDbContext`, `ICurrentUserContext` |
| `LimitsService` | `ILimitsService` | `KuyumStokApi.Infrastructure/Services/LimitsService/LimitsService.cs` | Stok limit CRUD | `AppDbContext` |
| `LifecycleActionsService` | `ILifecycleActionsService` | `KuyumStokApi.Infrastructure/Services/LifecycleActionsService/LifecycleActionsService.cs` | Lifecycle action CRUD | `AppDbContext` |
| `ProductLifecyclesService` | `IProductLifecyclesService` | `KuyumStokApi.Infrastructure/Services/ProductLifecycleService/ProductLifecycleService.cs` | Stok lifecycle kayıtları | `AppDbContext`, `ICurrentUserService` |
| `PaymentMethodsService` | `IPaymentMethodsService` | `KuyumStokApi.Infrastructure/Services/PaymentMethodsService/PaymentMethodsService.cs` | Ödeme yöntemi CRUD + referans kontrol | `AppDbContext` |
| `RolesService` | `IRolesService` | `KuyumStokApi.Infrastructure/Services/RolesService/RolesService.cs` | Rol CRUD + uniqueness | `AppDbContext` |
| `StoresService` | `IStoresService` | `KuyumStokApi.Infrastructure/Services/StoresService/StoresService.cs` | Mağaza CRUD + şube guard | `AppDbContext` |
| `ThermalPrintersService` | `IThermalPrintersService` | `KuyumStokApi.Infrastructure/Services/ThermalPrintersService/ThermalPrintersService.cs` | Şube bazlı termal yazıcı yönetimi | `AppDbContext` |
| `AnomalyDetectionService` | N/A | `KuyumStokApi.Infrastructure/Services/AnomalyDetectionService/AnomalyDetectionService.cs` | Z-score tabanlı anomali tespiti | `ILogger` |
| `WorkloadEstimationService` | N/A | `KuyumStokApi.Infrastructure/Services/WorkloadEstimationService/WorkloadEstimationService.cs` | İş yükü tahmini (moving avg/linear) | `ILogger` |
| `PasswordHasher` | `IPasswordHasher` | `KuyumStokApi.Infrastructure/PasswordHasher/PasswordHasher.cs` | Parola hash + verify | `PasswordOptions` |
| `PasswordPolicy` | N/A | `KuyumStokApi.Infrastructure/Security/PasswordPolicy.cs` | Parola/username kuralları | N/A |
| `CurrentUserService` | `ICurrentUserService` | `KuyumStokApi.Infrastructure/Auth/CurrentUserService.cs` | JWT claim okuma (UserId/UserName) | `IHttpContextAccessor` |
| `CurrentUserContext` | `ICurrentUserContext` | `KuyumStokApi.Application` (impl: Infra) | IsAuthenticated, UserId, BranchId, UserName | `IHttpContextAccessor` |

**API Index**

| endpoint | handler | auth | notes |
|---|---|---|---|
| `GET /api/Stocks` | `StocksController.GetPaged` | Authorize | stok listesi (direct rows) |
| `GET /api/Stocks/variant/{variantId}/detail` | `StocksController.GetVariantDetail` | Authorize | mağaza içi şube toplamları |
| `GET /api/Stocks/{id}` | `StocksController.GetById` | Authorize | stok detay (GUID) |
| `GET /api/Stocks/by-barcode/{barcode}` | `StocksController.GetByBarcode` | Authorize | barkod ile stok |
| `GET /api/Stocks/by-code/{code}` | `StocksController.GetByPublicCode` | Authorize | publicCode ile stok |
| `POST /api/Stocks` | `StocksController.Create` | Authorize | stok upsert |
| `PUT /api/Stocks/{id}` | `StocksController.Update` | Authorize | stok güncelleme |
| `POST /api/Stocks/backfill-public-codes` | `StocksController.BackfillPublicCodes` | Authorize | eksik publicCode backfill |
| `DELETE /api/Stocks/{id}` | `StocksController.Delete` | Authorize | stok silme (bağlı kayıt varsa 409) |
| `DELETE /api/Stocks/{id}/hard` | `StocksController.HardDelete` | Authorize | kalıcı silme |
| `GET /api/Stocks/favorites` | `StocksController.GetFavorites` | Authorize | en çok satılan ürünler (top/days/onlyMarked) |
| `GET /r/{code}` | `ResolveController.Resolve` | AllowAnonymous | publicCode resolve -> redirect |
| `POST /api/Sales` | `SalesController.Create` | Authorize | birleşik fiş (satış+alış) |
| `GET /api/Sales` | `SalesController.GetPaged` | Authorize | satış kalem listesi |
| `GET /api/Sales/{id}` | `SalesController.GetById` | Authorize | satış kalem detayı |
| `POST /api/Purchases` | `PurchasesController.Create` | Authorize | alış fişi |
| `GET /api/Purchases` | `PurchasesController.GetPaged` | Authorize | alış listesi |
| `GET /api/Purchases/{id}` | `PurchasesController.GetById` | Authorize | alış detayı |
| `DELETE /api/Dev/cleanup-all-data` | `DevController.CleanupAllData` | Authorize | DEV only, transactional data siler |

**Environment Variables**

| name | required? | used in | description | example |
|---|---|---|---|---|
| `SKIP_DB_MIGRATE` | optional | `DbInitExtensions.MigrateAndSeedAsync` | `true` → skip migration + seed | `true` |
| `ASPNETCORE_ENVIRONMENT` | optional | ASP.NET Core runtime | environment selection | `Development` |
| `ASPNETCORE_URLS` | optional | Dockerfile runtime | bind URL | `http://0.0.0.0:5000` |
| `ASPNETCORE_HTTP_PORTS` | optional | launch profile | container HTTP port | `8080` |
| `ASPNETCORE_HTTPS_PORTS` | optional | launch profile | container HTTPS port | `8081` |

## 2) Cross-cutting behaviors that affect ALL services

**Soft delete ve global filtre**
- `AppDbContext.Partials.OnModelCreatingPartial`: `ISoftDeletable` implement eden tüm entity'lere `IsDeleted == false` query filter'ı uygulanır.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`
- `AppDbContext.SaveChanges/SaveChangesAsync`: `EntityState.Deleted` olan `ISoftDeletable` kayıtları `Modified`'a çevirir ve `IsDeleted`, `DeletedAt`, `DeletedBy` set eder.  
  - `ApplySoftDelete` metodu, `DeletedBy` değerini `ICurrentUserService.UserId`'dan alır.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`
- **Not**: `Stocks` entity'si `ISoftDeletable` implement etmez; `StocksController.Delete` physical delete (Remove) kullanır.

**SaveChanges side-effect: Dashboard yayınları**
- `SaveChanges` ve `SaveChangesAsync` çağrısı sonrası, değişen entity tipleri toplanır ve `IDashboardNotificationService` tetiklenir.  
  - `TriggerDashboardNotifications` fire-and-forget çalışır ve hata yutar.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`
- Hangi entity değişiminin hangi dashboard yayınlarını tetiklediği, `DashboardNotificationService.EntityToDashboardMapping` ile belirlenir (Sales, Purchases, Stocks, SaleDetails, PurchaseDetails, Limits).  
  - `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardNotificationService.cs`
- `SuppressDashboardNotifications = true` ile tetikleme geçici olarak kapatılabilir (örn. SalesService transaction sırasında).

**Transaction / unit-of-work**
- `PurchasesService.CreateAsync` ve `SalesService.CreateUnifiedAsync` açık transaction kullanır (`BeginTransactionAsync`).  
  - `PurchasesService` stok, fiş, detay, lifecycle işlemlerini tek transaction'da yapar.  
  - `SalesService` birleşik fişte hem satış hem alış işlemlerini tek transaction'da yapar; commit sonrası `NotifySaleCommittedAsync` çağrılır.
- `StocksService.CreateAsync` belirli koşullarda transaction kullanır (`PurchasePrice` varsa ve `skipPurchaseCreation=false`).  

**Authorization varsayımları**
- Servisler doğrudan `[Authorize]` kontrolü yapmaz; kimlik/branch bilgisi kullanan servisler `ICurrentUserContext` veya `ICurrentUserService` üzerinden claim okur.  
  - SalesService, ReportsService, DashboardService, StocksService → ICurrentUserContext  
  - ProductLifecyclesService → ICurrentUserService

## 3) Per-service deep dive

### 3.1 `BanksService`
**Purpose**  
Basit banka CRUD işlevleri; soft delete ve active flag yönetimi.

**Public methods + callers**
- `GetPagedAsync(BankFilter, ct)` → `BanksController.GetPaged`
- `GetByIdAsync(int, ct)` → `BanksController.GetById`
- `CreateAsync(BankCreateDto, ct)` → `BanksController.Create`
- `UpdateAsync(int, BankUpdateDto, ct)` → `BanksController.Update`
- `DeleteAsync(int, ct)` → `BanksController.Delete`
- `HardDeleteAsync(int, ct)` → `BanksController.HardDelete`
- `SetActiveAsync(int, bool, ct)` → `BanksController.SetActive`

### 3.2 `BranchesService`
**Purpose**  
Şube CRUD; şube silme sırasında bağlı kayıt guard'ları.

**Method behavior**
- `DeleteAsync` guard: `Users`, `Stocks`, `Sales`, `Purchases` bağlı ise 409.
- `HardDeleteAsync` guard: yukarıdaki bağlı kayıtlar (soft-delete dahil) varsa 409.

### 3.3 `CustomersService`
**Purpose**  
Müşteri CRUD; satış/alış referansına göre silme davranışı.

**Method behavior**
- `DeleteAsync`: satış veya alış referansı varsa `IsDeleted=true` (soft delete), aksi halde `Remove`.

### 3.4 `DashboardService`
**Purpose**  
Dashboard metrikleri, raporlar, anomali ve performans çıktılarını üretir.

**Public methods + callers**
- `GetSummaryAsync` → `DashboardController.GetSummary`, `DashboardHub`
- `GetLiveCountersAsync` → `DashboardController.GetLiveCounters`, `DashboardNotificationService`, `DashboardHub`
- `GetDailySummaryAsync` → `DashboardController.GetDailySummary`, `DashboardNotificationService`, `DashboardHub`
- `GetAnomaliesAsync` → `DashboardController.GetAnomalies`, `DashboardNotificationService`, `DashboardHub`
- `GetTopProductsAsync` → `DashboardController.GetTopProducts`
- `GetDailyTopSellingTrendAsync` → `DashboardController.GetDailyTopSelling`
- `GetProfitLossAsync` → `DashboardController.GetProfitLoss`
- `GetSalesPieChartAsync` → `DashboardController.GetSalesPieChart`

**Behavior highlights**
- Role/branch scope: `ResolveScopeAsync` kullanıcı rolü ve şube bilgisine göre `AccessibleBranchIds` hesaplar.
- `GetSummaryAsync` paralel çalışır ve her bölüm kendi `AppDbContext` örneğini `IDbContextFactory<AppDbContext>` üzerinden alır.

### 3.5 `DashboardNotificationService`
**Purpose**  
`SaveChanges` sonrası değişen entity tiplerine göre dashboard yayınlarını tetikler; ayrıca `NotifySaleCommittedAsync` ile satış commit sonrası broadcast.

### 3.6 `DashboardBackgroundService`
**Purpose**  
Periyodik olarak dashboard verilerini broadcast eder: LiveCounters (30s), DailySummary (1m), Anomalies (2m).

### 3.7 `JwtService`
**Purpose**  
HS256 JWT üretimi, claim seti oluşturma.

### 3.8 `RefreshTokenService`
**Purpose**  
Refresh token üretimi, doğrulama, revoke, rotation.

### 3.9 `TokenBlacklistService`
**Purpose**  
JWT logout sonrası blacklist.

### 3.10 `UserService`
**Purpose**  
Register/login ve kullanıcı yönetimi; password policy, MustChangePassword.

### 3.11 `ProductCategoryService`
**Purpose**  
Kategori CRUD + isim benzersizliği.

### 3.12 `ProductTypeService`
**Purpose**  
Ürün türü CRUD + child (variant) guard'ları.

### 3.13 `ProductVariantService`
**Purpose**  
Ürün varyantı CRUD; unique kombinasyon (ProductTypeId, Name, Brand, Ayar, Color).

### 3.14 `StocksService`
**Purpose**  
Stok listeleme (direct rows), stok CRUD, QR üretimi, favorites, publicCode backfill.

**Behavior highlights**
- `GetPagedAsync`: `Stocks` tablosundan direkt okur, **GroupBy yoktur**; `StockDto.Id` = `Stocks.Id`, `StockDto.TotalWeight` = `Stocks.TotalWeightGram`.  
- `GetPagedAsync` filtreleri: `Query` barkod/QR/publicCode (normalize + ILIKE) ve varyant alanlarını tarar; `GramMin/Max`, `UpdatedFrom/To`, `ProductTypeId`, `ProductVariantId` uygulanır.  
- `GetVariantDetailInStoreAsync`: mağazadaki tüm şubelerde toplam adet ve toplam ağırlığı toplar (`Stocks.TotalWeightGram`).  
- `GetByPublicCodeAsync`: `PublicCodeService.Normalize` + `IsValid` ile doğrular; eşleşme yoksa 404.  
- `GetResolveRedirectUrlAsync`: public code doğrulanır; `FrontendBaseUrl` varsa `/stocks/{code}`, yoksa `/api/stocks/by-code/{code}` URL'i döndürür.  
- `CreateAsync`: upsert **yalnızca `(ProductVariantId, BranchId)`** ile yapılır; yeni kayıtta her zaman `PublicCode` atanır.  
- `StockCreateDto` zorunlu alanlar: `ProductVariantId`, `BranchId`, `Quantity`, `TotalWeightGram`, `Gram`, `Thickness`, `Width`, `Carat`, `WorkmanshipMilyem`, **`PurchasePrice`** (Required). `TotalWeightGram <= 0` ise `Gram` yedeklenir.  
- `CreateAsync` QR: `GenerateQrCode=true` ise QR Base64 üretir; QR payload **yalnızca `publicCode`**'dur.  
- `BackfillPublicCodesAsync`: `PublicCode` NULL olan kayıtlara kod atar; QR varsa yeniden üretir.  
- `AllocatePublicCodeAsync`: max 10 denemede benzersiz kod üretir; DB'de ve `reserved` setinde çakışma kontrolü yapar.  
- `GetFavoritesAsync`: En çok satılan ürünleri (ProductVariant bazında) getirir; `top`, `days`, `onlyMarked` parametreleri.  
- `DeleteAsync`: Bağlı kayıt (SaleDetails, PurchaseDetails, ProductLifecycles) varsa 409; yoksa `Remove` (physical delete).  
- `HardDeleteAsync`: ExecuteDeleteAsync ile kalıcı silme; bağlı kayıt varsa 409.

**Invariants & gotchas**
- `Stocks.Gram` sadece filtreleme/DTO görünümü için kullanılır; toplamlar `TotalWeightGram` üzerinden hesaplanır.  
- `PublicCode` 10 karakterli Crockford Base32 olup `Normalize` ile aynılaştırılır (`O->0`, `I/L->1`).  
- `Stocks` entity soft delete kullanmaz; Delete physical delete yapar.

### 3.15 `PurchasesService`
**Purpose**  
Alış fişi oluşturma; stok artırma ve lifecycle kaydı.

**Behavior highlights**
- `CreateAsync`: Barkod ile stok bulur; yoksa yeni stok oluşturur (Barcode unique). Mevcut stokta `Quantity` ve `TotalWeightGram` artırılır.  
- `PurchaseItemDto.TotalWeightGram > 0` zorunlu; `PurchaseDetails.TotalWeightGram` set edilir.  
- Stok merge kriteri: `Barcode` eşleşmesi (StocksService'ten farklı; StocksService `(ProductVariantId, BranchId)` kullanır).

### 3.16 `SalesService`
**Purpose**  
Birleşik fiş (satış+alış), stok düşümü/eklemesi, ödeme kayıtları.

**Behavior highlights**
- `CreateUnifiedAsync`: SaleItems ve/veya PurchaseItems zorunlu; ödeme toplamı > 0.  
- Validasyon: `UserId`, `BranchId`, `CustomerId` zorunlu; `Cash`, `Eft`, `Pos` (varsa > 0), `BankId` (Pos > 0 ise zorunlu), `POS_CommissionRate`, `PaymentMethodId` `PositiveNumberGuard` ile kontrol edilir (null değerler atlanır).  
- `ProcessSaleAsync`:  
  - `Quantity > 0`, `TotalWeightGram > 0`, `SoldPrice > 0` validasyonları.  
  - Stokta hem `Quantity` hem `TotalWeightGram` yeterliliği kontrol edilir ve düşülür.  
  - `SaleDetails.TotalWeightGram` set edilir.  
- `ProcessPurchaseAsync`:  
  - `TotalWeightGram > 0` zorunlu; `StocksService.CreateAsync` (skipPurchaseCreation) ile stok artırılır.  
  - `PurchaseDetails.TotalWeightGram` set edilir.  
- Commit sonrası `NotifySaleCommittedAsync` ile dashboard broadcast tetiklenir.  
- `GetPagedAsync` ve `GetLineByIdAsync`: `AgirlikGram` alanı `SaleDetails.TotalWeightGram` üzerinden gelir.

### 3.17 `ReportsService`
**Purpose**  
Şube/mağaza/kullanıcı bazlı raporlar; erişim kapsamı rol ve şube bazlı.

### 3.18 `LimitsService`
**Purpose**  
Şube + ürün varyantı bazlı min/max stok limitleri.

### 3.19 `LifecycleActionsService`
**Purpose**  
Lifecycle aksiyon sözlüğü (CRUD).

### 3.20 `ProductLifecyclesService`
**Purpose**  
Stok yaşam döngüsü kayıtları (liste, detay, create); `ICurrentUserService` ile kullanıcı bilgisi.

### 3.21 `PaymentMethodsService`
**Purpose**  
Ödeme yöntemleri CRUD; satış/alış referansı varsa soft delete.

### 3.22 `RolesService`
**Purpose**  
Rol CRUD; isim benzersizliği.

### 3.23 `StoresService`
**Purpose**  
Mağaza CRUD; bağlı şube guard'ları.

### 3.24 `ThermalPrintersService`
**Purpose**  
Şube başına tek termal yazıcı kuralı.

### 3.25 `AnomalyDetectionService`
**Purpose**  
Z-score tabanlı anomali tespiti ve stok eşiği kontrolü.

### 3.26 `WorkloadEstimationService`
**Purpose**  
Moving average ve lineer regresyon ile iş yükü tahmini.

## 4) Critical flows (end-to-end)

### 4.1 Auth: register → login → refresh → logout
- **Register**: `UserService.RegisterAsync` kullanıcıyı oluşturur; password boşsa `MustChangePassword=true`. JWT + refresh token döner.
- **Login**: `UserService.LoginAsync` parola doğrular, JWT + refresh döner.  
- **Refresh**: `RefreshTokenService.GetUserByRefreshTokenAsync` ile kullanıcı alınır; eski refresh revoke; yeni JWT+refresh üretilir.  
- **Logout**: JWT `jti` blacklist'e yazılır + kullanıcının tüm refresh token'ları revoke edilir.  

### 4.2 Stocks: upsert + QR generation
- `StocksService.CreateAsync`:  
  - `(ProductVariantId, BranchId)` eşleşmesi ile upsert.  
  - `TotalWeightGram` toplam ağırlık olarak stokta artırılır.  
  - `GenerateQrCode` true ise QR Base64 üretir; payload **yalnızca `publicCode`**'dur (URL değil).  
  - `PurchasePrice` zorunlu; varsa `CreatePurchaseRecordAsync` ile alış kaydı oluşturulur.  

### 4.3 Sales: unified receipt flow
- `SalesService.CreateUnifiedAsync`  
  - `ProcessSaleAsync`: stoktan hem `Quantity` hem `TotalWeightGram` düşer; `SaleDetails.TotalWeightGram` yazılır.  
  - `ProcessPurchaseAsync`: `StocksService.CreateAsync` ile stokta hem `Quantity` hem `TotalWeightGram` artar; `PurchaseDetails.TotalWeightGram` yazılır.  
  - `ProcessPaymentsAsync`: `SalePayments` yazılır.  
  - Transaction içinde commit; ardından `NotifySaleCommittedAsync`.  

### 4.4 Dashboard: periodic + event-based broadcasts
- **Periodic**: `DashboardBackgroundService` her 30s (LiveCounters), 1m (DailySummary), 2m (Anomalies) SignalR yayın yapar.  
- **Event-based**: `AppDbContext.SaveChanges` → `DashboardNotificationService.NotifyDashboardChangesAsync` → ilgili event yayınları.  
- **Sale commit**: `SalesService.CreateUnifiedAsync` commit sonrası `NotifySaleCommittedAsync` → LiveCounters, DailySummary, Anomalies broadcast.  

### 4.5 Resolve: public code -> redirect
- `ResolveController.Resolve` anonim erişimlidir; `GetResolveRedirectUrlAsync` ile URL üretir.  
- `/r/{code}` resolve/redirect mekanizmasıdır; **QR payload'i değildir**.  
- `PublicCode` geçersizse 400, bulunamazsa 404, aksi halde `302` redirect döndürür.

## 5) Data mutation map

| Flow | Entities/tables mutated | Trigger method | Notes |
|---|---|---|---|---|
| Register | `Users`, `RefreshTokens` | `UserService.RegisterAsync` + `RefreshTokenService.GenerateRefreshTokenAsync` | `MustChangePassword` opsiyonel |
| Login | `RefreshTokens` | `UserService.LoginAsync` | JWT sadece üretilir |
| Refresh | `RefreshTokens` | `AuthController.Refresh` | refresh rotation |
| Logout | `InvalidatedTokens`, `RefreshTokens` | `AuthController.Logout` | JWT blacklist + revoke all |
| Stock upsert | `Stocks`, `Purchases`, `PurchaseDetails`, `ProductLifecycles` | `StocksService.CreateAsync` | `Quantity` + `TotalWeightGram` artırılır |
| PublicCode backfill | `Stocks` | `StocksService.BackfillPublicCodesAsync` | Eksik kodlar atanır, QR varsa yenilenir |
| Purchase create | `Purchases`, `PurchaseDetails`, `Stocks`, `ProductLifecycles` | `PurchasesService.CreateAsync` | Barcode merge; `TotalWeightGram` set edilir |
| Sale (unified) | `Sales`, `SaleDetails`, `SalePayments`, `Stocks`, `Purchases`, `PurchaseDetails`, `ProductLifecycles` | `SalesService.CreateUnifiedAsync` | stok `Quantity` + `TotalWeightGram` güncellenir |
| Bank CRUD | `Banks` | `BanksService.*` | soft/hard delete |
| Branch CRUD | `Branches` | `BranchesService.*` | bağlı kayıt guard |
| Customer delete | `Customers` | `CustomersService.DeleteAsync` | referans varsa soft delete |
| Limits CRUD | `Limits` | `LimitsService.*` | basit CRUD |
| Roles CRUD | `Roles` | `RolesService.*` | uniqueness |
| Stores CRUD | `Stores` | `StoresService.*` | şube guard |
| Thermal printers | `ThermalPrinters` | `ThermalPrintersService.*` | branch başına tek |
| Dev cleanup | `SaleDetails`, `SalePayments`, `BankTransactions`, `PurchaseDetails`, `ProductLifecycles`, `Sales`, `Purchases`, `Stocks` | `DevController.CleanupAllData` | Sadece Development |

## 6) Open questions / Unknowns

- Bu taramada belirsiz kalan bir konu yok. Yeni sorular için ilgili servis/metotlara bakılmalı.

## Key Files Index

| file path | what it contains | why it matters |
|---|---|---|
| `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs` | stok listeleme + upsert | inventory model |
| `KuyumStokApi.Infrastructure/Services/SalesService/SalesService.cs` | satış + birleşik fiş | stok düşümü |
| `KuyumStokApi.Infrastructure/Services/PurchasesService/PurchasesService.cs` | alış fişi | stok artışı (barcode merge) |
| `KuyumStokApi.Domain/Entities/Stocks.cs` | `TotalWeightGram` alanı | toplam gram |
| `KuyumStokApi.Domain/Entities/SaleDetails.cs` | `TotalWeightGram` alanı | satış satır toplamı |
| `KuyumStokApi.Domain/Entities/PurchaseDetails.cs` | `TotalWeightGram` alanı | alış satır toplamı |
| `KuyumStokApi.Application/DTOs/Receipts/UnifiedReceiptDto.cs` | unified receipt payloadları | satış/alış kalem ağırlıkları |
| `KuyumStokApi.Application/DTOs/Stocks/StocksDto.cs` | stock DTOs | stok giriş/çıkış |
| `KuyumStokApi.Persistence/Migrations/20260119204732_StockTotalsByVariantBranch_Manual.cs` | migration + backfill | veri konsolidasyonu |
