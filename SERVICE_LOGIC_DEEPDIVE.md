# Service Logic Deep Dive

Bu doküman, `KuyumStokApi.Infrastructure/Services/**` ve destekleyici katmanlardaki davranışı kod tabanlı olarak açıklar. Her iddia doğrudan koddan çıkarılmıştır; belirsiz alanlar “Unknown” olarak işaretlenmiştir.

## 1) Service Inventory (Index)

| Service | Interface | File path | Main responsibilities | Depends on |
|---|---|---|---|---|
| `BanksService` | `IBanksService` | `KuyumStokApi.Infrastructure/Services/BanksService/BanksService.cs` | Banka CRUD + soft/hard delete + aktif/pasif | `AppDbContext` |
| `BranchesService` | `IBranchesService` | `KuyumStokApi.Infrastructure/Services/BranchesService/BranchesService.cs` | Şube CRUD + guard’lar | `AppDbContext` |
| `CustomersService` | `ICustomersService` | `KuyumStokApi.Infrastructure/Services/CustomersService/CustomersService.cs` | Müşteri CRUD + satış/alış referans kontrolü | `AppDbContext` |
| `DashboardService` | `IDashboardService` | `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardService.cs` | Dashboard metrikleri + SignalR broadcast | `AppDbContext`, `ICurrentUserContext`, `IReportsService`, `AnomalyDetectionService`, `WorkloadEstimationService`, `IHubContext<DashboardHub>` |
| `DashboardNotificationService` | `IDashboardNotificationService` | `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardNotificationService.cs` | SaveChanges sonrası dashboard yayınları | `IHubContext<DashboardHub>`, `IDashboardService` |
| `DashboardBackgroundService` | `BackgroundService` | `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardBackgroundService.cs` | Periyodik dashboard broadcast | `IServiceProvider`, `IHubContext<DashboardHub>` |
| `JwtService` | `IJwtService` | `KuyumStokApi.Infrastructure/Services/JwtService/JwtService.cs` | JWT üretimi | `IOptions<JwtOptions>` |
| `RefreshTokenService` | `IRefreshTokenService` | `KuyumStokApi.Infrastructure/Services/RefreshTokenService/RefreshTokenService.cs` | Refresh token üretimi/rotate/revoke | `AppDbContext` |
| `TokenBlacklistService` | `ITokenBlacklistService` | `KuyumStokApi.Infrastructure/Services/TokenBlacklistService/TokenBlacklistService.cs` | JWT blacklist yönetimi | `AppDbContext` |
| `UserService` | `IUserService` | `KuyumStokApi.Infrastructure/Services/UserService/UserService.cs` | Register/login, user CRUD, password policy | `AppDbContext`, `IPasswordHasher`, `IJwtService`, `IRefreshTokenService` |
| `ProductCategoryService` | `IProductCategoryService` | `KuyumStokApi.Infrastructure/Services/ProductCategoryService/ProductCategoryService.cs` | Ürün kategorisi CRUD + uniqueness | `AppDbContext` |
| `ProductTypeService` | `IProductTypeService` | `KuyumStokApi.Infrastructure/Services/ProductTypService/ProductTypeService.cs` | Ürün türü CRUD + child guard | `AppDbContext` |
| `ProductVariantService` | `IProductVariantService` | `KuyumStokApi.Infrastructure/Services/ProductVariantService/ProductVariantService.cs` | Varyant CRUD + unique kombinasyon | `AppDbContext` |
| `StocksService` | `IStocksService` | `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs` | Stok listeleme (direct rows), upsert (branch+variant), QR | `AppDbContext`, `ICurrentUserContext`, `QrCodeOptions` |
| `PurchasesService` | `IPurchasesService` | `KuyumStokApi.Infrastructure/Services/PurchasesService/PurchasesService.cs` | Alış fişi + stok artırma (TotalWeightGram) + lifecycle | `AppDbContext` |
| `SalesService` | `ISalesService` | `KuyumStokApi.Infrastructure/Services/SalesService/SalesService.cs` | Birleşik fiş (satış+alış), stok düşme/ekleme (TotalWeightGram), ödeme | `AppDbContext`, `ICurrentUserContext`, `IStocksService` |
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
| `CurrentUserService` | `ICurrentUserService` | `KuyumStokApi.Infrastructure/Auth/CurrentUserService.cs` | JWT claim okuma (userId/username) | `IHttpContextAccessor` |

**API Index**

| endpoint | handler | auth | notes |
|---|---|---|---|
| `GET /api/Stocks` | `StocksController.GetPaged` | Authorize | stok listesi (direct rows) |
| `GET /api/Stocks/variant/{variantId}/detail` | `StocksController.GetVariantDetail` | Authorize | mağaza içi şube toplamlari |
| `POST /api/Stocks` | `StocksController.Create` | Authorize | stok upsert |
| `POST /api/Sales` | `SalesController.Create` | Authorize | birleşik fiş (satış+alış) |
| `GET /api/Sales` | `SalesController.GetPaged` | Authorize | satış kalem listesi |
| `GET /api/Sales/{id}` | `SalesController.GetById` | Authorize | satış kalem detayı |
| `POST /api/Purchases` | `PurchasesController.Create` | Authorize | alış fişi |
| `GET /api/Purchases` | `PurchasesController.GetPaged` | Authorize | alış listesi |
| `GET /api/Purchases/{id}` | `PurchasesController.GetById` | Authorize | alış detayı |

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
- `AppDbContext.Partials.OnModelCreatingPartial`: `ISoftDeletable` implement eden tüm entity’lere `IsDeleted == false` query filter’ı uygulanır.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`
- `AppDbContext.SaveChanges/SaveChangesAsync`: `EntityState.Deleted` olan `ISoftDeletable` kayıtları `Modified`’a çevirir ve `IsDeleted`, `DeletedAt`, `DeletedBy` set eder.  
  - `ApplySoftDelete` metodu, `DeletedBy` değerini `ICurrentUserService.UserId`’dan alır.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`

**SaveChanges side-effect: Dashboard yayınları**
- `SaveChanges` ve `SaveChangesAsync` çağrısı sonrası, değişen entity tipleri toplanır ve `IDashboardNotificationService` tetiklenir.  
  - `TriggerDashboardNotifications` fire-and-forget çalışır ve hata yutar.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`
- Hangi entity değişiminin hangi dashboard yayınlarını tetiklediği, `DashboardNotificationService.EntityToDashboardMapping` ile belirlenir.  
  - `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardNotificationService.cs`

**Transaction / unit-of-work**
- `PurchasesService.CreateAsync` ve `SalesService.CreateUnifiedAsync` açık transaction kullanır (`BeginTransactionAsync`).  
  - `PurchasesService` stok, fiş, detay, lifecycle işlemlerini tek transaction’da yapar.  
  - `SalesService` birleşik fişte hem satış hem alış işlemlerini tek transaction’da yapar.  
  - `KuyumStokApi.Infrastructure/Services/PurchasesService/PurchasesService.cs`, `KuyumStokApi.Infrastructure/Services/SalesService/SalesService.cs`
- `StocksService.CreateAsync` belirli koşullarda transaction kullanır (`PurchasePrice` varsa).  
  - `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs`

**Authorization varsayımları**
- Servisler doğrudan `[Authorize]` kontrolü yapmaz; kimlik/branch bilgisi kullanan servisler `ICurrentUserContext` veya `ICurrentUserService` üzerinden claim okur.  
  - `SalesService`, `ReportsService`, `DashboardService`, `StocksService`, `ProductLifecyclesService`

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

**Method behavior**
- `GetPagedAsync`: `Banks` üzerinde filtre (query, isActive, updated range); `IncludeDeleted` varsa `IgnoreQueryFilters`.  
  - DB: `Banks` (read).
- `GetByIdAsync`: soft-deleted dahil (`IgnoreQueryFilters`).  
  - DB: `Banks` (read).
- `CreateAsync`: `Name` trim, `IsActive=true`, `UpdatedAt=UtcNow`.  
  - DB: `Banks` (insert).
- `UpdateAsync`: `Name`, `Description`, `UpdatedAt`.  
  - DB: `Banks` (update).
- `DeleteAsync`: `Remove` çağrısı → soft delete hook devreye girer.  
  - DB: `Banks` (soft delete).
- `HardDeleteAsync`: `ExecuteDeleteAsync` ile kalıcı silme.  
  - DB: `Banks` (delete).
- `SetActiveAsync`: `IsActive`, `UpdatedAt`.  
  - DB: `Banks` (update).

**Invariants & gotchas**
- Soft delete aktif, `DeleteAsync` kalıcı silmez.  
  - `AppDbContext.Partials.cs`

### 3.2 `BranchesService`
**Purpose**  
Şube CRUD; şube silme sırasında bağlı kayıt guard’ları.

**Public methods + callers**
- `GetPagedAsync(BranchFilter, ct)` → `BranchesController.GetPaged`
- `GetByIdAsync(int, ct)` → `BranchesController.GetById`
- `CreateAsync(BranchCreateDto, ct)` → `BranchesController.Create`
- `UpdateAsync(int, BranchUpdateDto, ct)` → `BranchesController.Update`
- `DeleteAsync(int, ct)` → `BranchesController.Delete`
- `HardDeleteAsync(int, ct)` → `BranchesController.HardDelete`
- `SetActiveAsync(int, bool, ct)` → `BranchesController.SetActive`

**Method behavior**
- `DeleteAsync` guard: `Users`, `Stocks`, `Sales`, `Purchases` bağlı ise 409.  
  - DB: `Branches`, `Users`, `Stocks`, `Sales`, `Purchases`.
- `HardDeleteAsync` guard: yukarıdaki bağlı kayıtlar (soft-delete dahil) varsa 409.  
  - `Users` için `IgnoreQueryFilters`.

**Invariants & gotchas**
- Bağlı kayıt varken soft delete bile izin vermez.  
  - `BranchesService.DeleteAsync`

### 3.3 `CustomersService`
**Purpose**  
Müşteri CRUD; satış/alış referansına göre silme davranışı.

**Public methods + callers**
- `GetPagedAsync(CustomerFilter, ct)` → `CustomersController.GetPaged`
- `GetByIdAsync(int, ct)` → `CustomersController.GetById`
- `CreateAsync(CustomerCreateDto, ct)` → `CustomersController.Create`
- `UpdateAsync(int, CustomerUpdateDto, ct)` → `CustomersController.Update`
- `DeleteAsync(int, ct)` → `CustomersController.Delete`

**Method behavior**
- `DeleteAsync`: satış veya alış referansı varsa `IsDeleted=true` ve `DeletedAt` set (soft delete), aksi halde `Remove`.  
  - DB: `Customers`, `Sales`, `Purchases`.

### 3.4 `DashboardService`
**Purpose**  
Dashboard metrikleri, raporlar, anomali ve performans çıktılarını üretir.

**Public methods + callers**
- `GetSummaryAsync` → `DashboardController.GetSummary`, `DashboardHub.OnConnectedAsync`, `DashboardHub.RequestSummary`
- `GetLiveCountersAsync` → `DashboardController.GetLiveCounters`, `DashboardNotificationService`, `DashboardHub.OnConnectedAsync`
- `GetDailySummaryAsync` → `DashboardController.GetDailySummary`, `DashboardNotificationService`, `DashboardHub.OnConnectedAsync`
- `GetAnomaliesAsync` → `DashboardController.GetAnomalies`, `DashboardNotificationService`, `DashboardHub.OnConnectedAsync`
- `GetTopProductsAsync` → `DashboardController.GetTopProducts`
- `GetProfitLossAsync` → `DashboardController.GetProfitLoss`

**Behavior highlights**
- Role/branch scope: `ResolveScopeAsync` kullanıcı rolü ve şube bilgisine göre `AccessibleBranchIds` hesaplar.  
  - `DashboardService.ResolveScopeAsync`
- `GetSummaryAsync` paralel çalışır ve her bölüm kendi `AppDbContext` örneğini `IDbContextFactory<AppDbContext>` üzerinden alır (eşzamanlı EF Core çakışmasını önlemek için).
- `GetDailySummaryAsync`: satış/alım toplamı, kar, en çok satan ürün, kritik stok sayısı hesaplar.  
  - DB: `SaleDetails`, `Sales`, `PurchaseDetails`, `Purchases`, `Stocks`, `ProductVariants`, `Limits`.
- SignalR broadcast: `BroadcastLiveCountersAsync`, `BroadcastDailySummaryAsync`, `BroadcastAnomaliesAsync` opsiyonel (`_hubContext` varsa).

### 3.5 `DashboardNotificationService`
**Purpose**  
`SaveChanges` sonrası değişen entity tiplerine göre dashboard yayınlarını tetikler.

**Public methods + callers**
- `NotifyDashboardChangesAsync(IEnumerable<string>, ct)` → `AppDbContext.TriggerDashboardNotifications`

### 3.6 `DashboardBackgroundService`
**Purpose**  
Periyodik olarak dashboard verilerini broadcast eder.

### 3.7 `JwtService`
**Purpose**  
HS256 JWT üretimi, claim seti oluşturma.

### 3.8 `RefreshTokenService`
**Purpose**  
Refresh token üretimi, doğrulama, revoke.

### 3.9 `TokenBlacklistService`
**Purpose**  
JWT logout sonrası blacklist.

### 3.10 `UserService`
**Purpose**  
Register/login ve kullanıcı yönetimi.

### 3.11 `ProductCategoryService`
**Purpose**  
Kategori CRUD + isim benzersizliği.

### 3.12 `ProductTypeService`
**Purpose**  
Ürün türü CRUD + child (variant) guard’ları.

### 3.13 `ProductVariantService`
**Purpose**  
Ürün varyantı CRUD; unique kombinasyon.

### 3.14 `StocksService`
**Purpose**  
Stok listeleme (direct rows), stok CRUD, QR üretimi.

**Behavior highlights**
- `GetPagedAsync`: `Stocks` tablosundan direkt okur, **GroupBy yoktur**; `StockDto.Id` gerçek `Stocks.Id` ve `StockDto.TotalWeight` `Stocks.TotalWeightGram` üzerinden gelir.  
  - `StocksService.GetPagedAsync`
- `GetVariantDetailInStoreAsync`: mağaza içi şube toplamlarını `Stocks.TotalWeightGram` üzerinden toplar.  
  - `StocksService.GetVariantDetailInStoreAsync`
- `CreateAsync`: upsert **yalnızca `(ProductVariantId, BranchId)`** ile yapılır.  
  - `StocksService.CreateAsync`
- `CreateAsync` validasyon: `Quantity > 0`, `TotalWeightGram > 0`.  
  - `StockCreateDto.TotalWeightGram` yoksa geçici uyumluluk için `StockCreateDto.Gram` (toplam gram) kullanılır.  
  - `StocksService.CreateAsync`
- `CreateAsync` stok artışı: `Quantity` ve `TotalWeightGram` eklenir; `PurchasePrice` varsa `CreatePurchaseRecordAsync` çalışır ve `PurchaseDetails.TotalWeightGram` set edilir.  
  - `StocksService.CreateAsync`, `StocksService.CreatePurchaseRecordAsync`
- `MapToDto`: `StockDto.TotalWeight` doğrudan `Stocks.TotalWeightGram` değeridir.  
  - `StocksService.MapToDto`

**Invariants & gotchas**
- `Stocks.Gram` sadece filtreleme ve DTO görünümü için kullanılır; toplam hesaplarda kullanılmaz.  
  - `StocksService.GetPagedAsync`, `StocksService.MapToDto`

### 3.15 `PurchasesService`
**Purpose**  
Alış fişi oluşturma; stok artırma ve lifecycle kaydı.

**Behavior highlights**
- `CreateAsync`: `PurchaseItemDto.TotalWeightGram > 0` zorunlu; stokta hem `Quantity` hem `TotalWeightGram` artırılır.  
  - `PurchasesService.CreateAsync`
- `PurchaseDetails.TotalWeightGram` set edilir.  
  - `PurchasesService.CreateAsync`

### 3.16 `SalesService`
**Purpose**  
Birleşik fiş (satış+alış), stok düşümü/eklemesi, ödeme kayıtları.

**Behavior highlights**
- `CreateUnifiedAsync`: SaleItems ve/veya PurchaseItems zorunlu; ödeme toplamı > 0.  
  - `SalesService.CreateUnifiedAsync`
- `ProcessSaleAsync`:  
  - `Quantity > 0`, `TotalWeightGram > 0`, `SoldPrice > 0` validasyonları.  
  - Stokta hem `Quantity` hem `TotalWeightGram` yeterliliği kontrol edilir ve düşülür.  
  - `SaleDetails.TotalWeightGram` set edilir.  
  - `SalesService.ProcessSaleAsync`
- `ProcessPurchaseAsync`:  
  - `TotalWeightGram > 0` zorunlu; `StockCreateDto.TotalWeightGram` ile stok artırılır.  
  - `PurchaseDetails.TotalWeightGram` set edilir.  
  - `SalesService.ProcessPurchaseAsync`
- `GetPagedAsync` ve `GetLineByIdAsync`: `AgirlikGram` alanı `SaleDetails.TotalWeightGram` üzerinden gelir.  
  - `SalesService.GetPagedAsync`, `SalesService.GetLineByIdAsync`

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
Stok yaşam döngüsü kayıtları (liste, detay, create).

### 3.21 `PaymentMethodsService`
**Purpose**  
Ödeme yöntemleri CRUD; satış/alış referansı varsa soft delete.

### 3.22 `RolesService`
**Purpose**  
Rol CRUD; isim benzersizliği.

### 3.23 `StoresService`
**Purpose**  
Mağaza CRUD; bağlı şube guard’ları.

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
- **Register**: `UserService.RegisterAsync` kullanıcıyı oluşturur; password boşsa `MustChangePassword=true`.  
  - `AuthController.Register` JWT + refresh token döner.  
  - `JwtService.GenerateToken`, `RefreshTokenService.GenerateRefreshTokenAsync`
- **Login**: `UserService.LoginAsync` parola doğrular, JWT + refresh döner.  
  - `PasswordHasher.Verify`
- **Refresh**: `RefreshTokenService.GetUserByRefreshTokenAsync` ile kullanıcı alınır; eski refresh revoke; yeni JWT+refresh üretilir.  
  - `AuthController.Refresh`
- **Logout**: JWT `jti` blacklist’e yazılır + kullanıcının tüm refresh token’ları revoke edilir.  
  - `TokenBlacklistService.InvalidateTokenAsync`, `RefreshTokenService.RevokeAllUserTokensAsync`

### 4.2 Stocks: upsert + QR generation
- `StocksService.CreateAsync`:  
  - `(ProductVariantId, BranchId)` eşleşmesi ile upsert.  
  - `TotalWeightGram` toplam ağırlık olarak stokta artırılır.  
  - `GenerateQrCode` true ise QR Base64 üretilir (`QrCodeOptions.BaseUrl/{StockId}`).

### 4.3 Sales: unified receipt flow
- `SalesService.CreateUnifiedAsync`  
  - `ProcessSaleAsync`: stoktan hem `Quantity` hem `TotalWeightGram` düşer; `SaleDetails.TotalWeightGram` yazılır.  
  - `ProcessPurchaseAsync`: stokta hem `Quantity` hem `TotalWeightGram` artar; `PurchaseDetails.TotalWeightGram` yazılır.  
  - `ProcessPaymentsAsync`: `SalePayments` yazılır (`Nakit`, `Havale/EFT`, `Kredi Kartı`).  
  - Transaction içinde commit edilir.

### 4.4 Dashboard: periodic + event-based broadcasts
- **Periodic**: `DashboardBackgroundService` belirli aralıklarla SignalR yayın yapar.  
  - `LiveCountersUpdated`, `DailySummaryUpdated`, `AnomaliesUpdated`
- **Event-based**: `AppDbContext.SaveChanges` → `DashboardNotificationService.NotifyDashboardChangesAsync` → ilgili event yayınları.

## 5) Data mutation map

| Flow | Entities/tables mutated | Trigger method | Notes |
|---|---|---|---|
| Register | `Users`, `RefreshTokens` | `UserService.RegisterAsync` + `RefreshTokenService.GenerateRefreshTokenAsync` | `MustChangePassword` opsiyonel |
| Login | `RefreshTokens` | `UserService.LoginAsync` | JWT sadece üretilir |
| Refresh | `RefreshTokens` | `AuthController.Refresh` | refresh rotation |
| Logout | `InvalidatedTokens`, `RefreshTokens` | `AuthController.Logout` | JWT blacklist + revoke all |
| Stock upsert | `Stocks`, `Purchases`, `PurchaseDetails`, `ProductLifecycles` | `StocksService.CreateAsync` | `Quantity` + `TotalWeightGram` artırılır |
| Purchase create | `Purchases`, `PurchaseDetails`, `Stocks`, `ProductLifecycles` | `PurchasesService.CreateAsync` | `TotalWeightGram` set edilir |
| Sale (unified) | `Sales`, `SaleDetails`, `SalePayments`, `Stocks`, `Purchases`, `PurchaseDetails`, `ProductLifecycles` | `SalesService.CreateUnifiedAsync` | stok `Quantity` + `TotalWeightGram` güncellenir |
| Bank CRUD | `Banks` | `BanksService.*` | soft/hard delete |
| Branch CRUD | `Branches` | `BranchesService.*` | bağlı kayıt guard |
| Customer delete | `Customers` | `CustomersService.DeleteAsync` | referans varsa soft delete |
| Limits CRUD | `Limits` | `LimitsService.*` | basit CRUD |
| Roles CRUD | `Roles` | `RolesService.*` | uniqueness |
| Stores CRUD | `Stores` | `StoresService.*` | şube guard |
| Thermal printers | `ThermalPrinters` | `ThermalPrintersService.*` | branch başına tek |

## 6) Open questions / Unknowns

- Bu taramada belirsiz kalan bir konu yok. Yeni sorular için ilgili servis/metotlara bakılmalı.  
  - Örn: `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs`, `SalesService.cs`, `PurchasesService.cs`

## Key Files Index

| file path | what it contains | why it matters |
|---|---|---|
| `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs` | stok listeleme + upsert | inventory model |
| `KuyumStokApi.Infrastructure/Services/SalesService/SalesService.cs` | satış + birleşik fiş | stok düşümü |
| `KuyumStokApi.Infrastructure/Services/PurchasesService/PurchasesService.cs` | alış fişi | stok artışı |
| `KuyumStokApi.Domain/Entities/Stocks.cs` | `TotalWeightGram` alanı | toplam gram |
| `KuyumStokApi.Domain/Entities/SaleDetails.cs` | `TotalWeightGram` alanı | satış satır toplamı |
| `KuyumStokApi.Domain/Entities/PurchaseDetails.cs` | `TotalWeightGram` alanı | alış satır toplamı |
| `KuyumStokApi.Application/DTOs/Receipts/UnifiedReceiptDto.cs` | unified receipt payloadları | satış/alış kalem ağırlıkları |
| `KuyumStokApi.Application/DTOs/Stocks/StocksDto.cs` | stock DTOs | stok giriş/çıkış |
| `KuyumStokApi.Persistence/Migrations/20260119120000_StockTotalsByVariantBranch.cs` | migration + backfill | veri konsolidasyonu |
