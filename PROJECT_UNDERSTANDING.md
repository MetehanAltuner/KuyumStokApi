# Project Understanding Document

## Last Updated (UTC)
- 2026-01-27T20:54:42Z
- Aligned QR payload definition to PublicCode-only (no URL).

## 1) Quick Repo Scan (top-level map + tech stack)

**Top-level map**
- `KuyumStokApi.API/` – ASP.NET Core Web API host, controllers, `Program.cs`, SignalR hub mapping, Swagger.
- `KuyumStokApi.Application/` – DTOs, service interfaces, common result types, SignalR hub type.
- `KuyumStokApi.Domain/` – entity models (EF Core).
- `KuyumStokApi.Persistence/` – EF Core `DbContext`, migrations, seed, DB init.
- `KuyumStokApi.Infrastructure/` – service implementations, auth/JWT, password hashing, dashboard background.
- `Dockerfile` – build + runtime container for API.

**Tech stack (from `.csproj` and code)**
- .NET 8 (`TargetFramework` `net8.0`) in all projects.  
  - `KuyumStokApi.API/KuyumStokApi.API.csproj`
- ASP.NET Core Web API + SignalR (`AddSignalR`, `DashboardHub`).  
  - `KuyumStokApi.API/Program.cs`, `KuyumStokApi.Application/Hubs/DashboardHub.cs`
- EF Core 9.x + Npgsql (PostgreSQL).  
  - `KuyumStokApi.Persistence/KuyumStokApi.Persistence.csproj`
- JWT Auth (`Microsoft.AspNetCore.Authentication.JwtBearer`).  
  - `KuyumStokApi.API/Program.cs`
- Swagger (Swashbuckle).  
  - `KuyumStokApi.API/KuyumStokApi.API.csproj`, `Program.cs`
- QR code generation (QRCoder).  
  - `KuyumStokApi.Infrastructure/KuyumStokApi.Infrastructure.csproj`, `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs`
- ML packages (Microsoft.ML, TimeSeries) – used by dashboard services.  
  - `KuyumStokApi.Infrastructure/KuyumStokApi.Infrastructure.csproj`

## 2) Entry Points & Boot Sequence (how the app starts, request/event flow)

**Entry point**
- `KuyumStokApi.API/Program.cs` – top-level statements set up DI, auth, Swagger, routes, and DB init.

**Boot sequence (in order)**
1. Build `WebApplication` and load config (`builder.Configuration`).  
   - `KuyumStokApi.API/Program.cs`
2. Register MVC controllers and SignalR.  
   - `builder.Services.AddControllers()`, `AddSignalR()` in `Program.cs`
3. Register CORS policies (default + `SignalRCorsPolicy`).  
   - `Program.cs`
4. Register Persistence + Infrastructure DI.  
   - `AddPersistence`, `AddInfrastructure` in `Program.cs`
5. Register `ICurrentUserContext` + JWT bearer auth + authorization.  
   - `Program.cs`, `KuyumStokApi.Application/Interfaces/Auth/CurrentUserContext.cs`
6. Configure Swagger with JWT security definition.  
   - `Program.cs`
7. Build app and configure pipeline: Swagger (dev only), HTTPS redirection, CORS, static files, auth, controllers.  
   - `Program.cs`
8. Map SignalR hub `/api/hubs/dashboard` and apply CORS policy.  
   - `Program.cs`, `KuyumStokApi.Application/Hubs/DashboardHub.cs`
9. Run migrations + seed via `app.MigrateAndSeedAsync()`.  
   - `Program.cs`, `KuyumStokApi.Persistence/Extensions/DbInitExtensions.cs`
10. `app.Run()`.

**Request flow (HTTP)**
`Controller` → service interface (Application) → service implementation (Infrastructure) → `AppDbContext` (Persistence).  
Example:
- `KuyumStokApi.API/Controllers/SalesController.cs` → `ISalesService.CreateUnifiedAsync` → `KuyumStokApi.Infrastructure/Services/SalesService/SalesService.cs`

**Event flow (SignalR + background)**
- SignalR hub: `/api/hubs/dashboard`.  
  - `Program.cs`, `KuyumStokApi.Application/Hubs/DashboardHub.cs`
- Background broadcasts: `DashboardBackgroundService` sends periodic updates.  
  - `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardBackgroundService.cs`
- Event-based broadcasts: `AppDbContext.SaveChanges` triggers `DashboardNotificationService`.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`, `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardNotificationService.cs`

## 3) Architecture Map (modules + boundaries + communication)

**Layered architecture**
- **API layer** (`KuyumStokApi.API`): HTTP endpoints, authorization attributes, uses interfaces only.  
  - Controllers in `KuyumStokApi.API/Controllers/*.cs`
- **Application layer** (`KuyumStokApi.Application`): DTOs + service contracts + common results + hub type.  
  - `DTOs/*`, `Interfaces/*`, `Common/*`, `Hubs/DashboardHub.cs`
- **Domain layer** (`KuyumStokApi.Domain`): EF entities and common interfaces.  
  - `Domain/Entities/*.cs`, `Domain/Common/*`
- **Infrastructure layer** (`KuyumStokApi.Infrastructure`): business logic implementations, auth, hashing, background services.  
  - `Infrastructure/Services/*`, `Auth`, `PasswordHasher`, `Security`
- **Persistence layer** (`KuyumStokApi.Persistence`): EF Core DbContext, migrations, seed.  
  - `Persistence/Contexts/*`, `Migrations/*`, `Seed/SeedData.cs`

**Communication**
- API → Application interfaces → Infrastructure implementations → Persistence DbContext.
- SignalR hub is in Application but wired + used by Infrastructure background services.

## 4) Domain Model & Data (entities, relationships, schemas/migrations, DB config)

**Entities (file list)**
`KuyumStokApi.Domain/Entities/`:
- `Banks`, `BankTransactions`, `Branches`, `Customers`, `LifecycleActions`, `Limits`, `PaymentMethods`, `ProductCategories`, `ProductTypes`, `ProductVariants`, `Stocks`, `Purchases`, `PurchaseDetails`, `Sales`, `SaleDetails`, `SalePayments`, `ProductLifecycles`, `Roles`, `Users`, `Stores`, `ThermalPrinters`, `MonthlyTargets`, `RefreshTokens`, `InvalidatedTokens`.  
  - See `KuyumStokApi.Domain/Entities/*.cs`

**Inventory model (current)**
- `Stocks` now represents **one row per (BranchId, ProductVariantId)**; uniqueness is enforced by a unique index.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.cs` (`AppDbContext.OnModelCreating`)
- `Stocks.TotalWeightGram` stores **total weight on hand** (not unit weight).  
  - `KuyumStokApi.Domain/Entities/Stocks.cs`
- `Stocks.PublicCode` is an optional public identifier; a unique index enforces uniqueness when present.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.cs`, `KuyumStokApi.Persistence/Migrations/20260127120000_AddStockPublicCode.cs`
- `SaleDetails.TotalWeightGram` and `PurchaseDetails.TotalWeightGram` store **line total weight**.  
  - `KuyumStokApi.Domain/Entities/SaleDetails.cs`  
  - `KuyumStokApi.Domain/Entities/PurchaseDetails.cs`

**Schema + relationships (from EF Core mapping)**
- `AppDbContext` defines all `DbSet<>`s and FK relations.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.cs`
- Examples (non-exhaustive):
  - `Sales` ↔ `SaleDetails` via `SaleDetails.SaleId`.  
  - `Sales` ↔ `SalePayments` via `SalePayments.SaleId`.  
  - `Purchases` ↔ `PurchaseDetails` via `PurchaseDetails.PurchaseId`.  
  - `Stocks` ↔ `ProductVariants`, `Branches`.  
  - `ProductVariants` ↔ `ProductTypes` ↔ `ProductCategories`.  
  - `Users` ↔ `Roles`, `Branches`.  
  - `MonthlyTargets` ↔ `Stores`.  
  - `RefreshTokens` ↔ `Users` (cascade delete).  
  - `InvalidatedTokens` is standalone (JTI list).

**Soft delete + query filters**
- Global query filter for `ISoftDeletable` entities.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`
- `SaveChanges` overrides convert `EntityState.Deleted` → soft delete and set `DeletedAt`/`DeletedBy`.  
  - `AppDbContext.Partials.cs`

**Database config**
- PostgreSQL via Npgsql; connection string in `appsettings.json`.  
  - `KuyumStokApi.API/appsettings.json`
- DbContext registration uses `UseNpgsql`.  
  - `KuyumStokApi.Persistence/DependencyInjection.cs`

**Migrations**
- EF Core migrations in `KuyumStokApi.Persistence/Migrations/*`.  
  - Use `DbInitExtensions` to apply pending migrations on startup.  
  - `KuyumStokApi.Persistence/Extensions/DbInitExtensions.cs`
- Inventory evolution migration:  
  - `KuyumStokApi.Persistence/Migrations/20260119120000_StockTotalsByVariantBranch.cs`  
  - Adds `total_weight_gram` columns, consolidates duplicate stocks by `(branch_id, product_variant_id)`, repoints foreign keys, backfills line totals, and adds `ux_stocks_branch_variant`.

## 5) APIs / Interfaces (endpoints/topics/commands: method/path, purpose, req/res, auth, handler)

**API Index**

| endpoint | handler | auth | notes |
|---|---|---|---|
| `POST /api/Auth/register` | `AuthController.Register` | AllowAnonymous | register + JWT + refresh token |
| `POST /api/Auth/login` | `AuthController.Login` | AllowAnonymous | login + JWT + refresh token |
| `POST /api/Auth/refresh` | `AuthController.Refresh` | AllowAnonymous | refresh token rotation |
| `POST /api/Auth/logout` | `AuthController.Logout` | Authorize | blacklist JWT + revoke refresh tokens |
| `POST /api/Auth/validate-password` | `AuthController.ValidatePassword` | AllowAnonymous | password policy check |
| `POST /api/Auth/validate-register` | `AuthController.ValidateRegister` | AllowAnonymous | register validation |
| `GET /api/Banks` | `BanksController.GetPaged` | Authorize | list banks |
| `GET /api/Banks/{id}` | `BanksController.GetById` | Authorize | get bank |
| `POST /api/Banks` | `BanksController.Create` | Authorize | create bank |
| `PUT /api/Banks/{id}` | `BanksController.Update` | Authorize | update bank |
| `DELETE /api/Banks/{id}` | `BanksController.Delete` | Authorize | soft delete |
| `PUT /api/Banks/{id}/active` | `BanksController.SetActive` | Authorize | activate/deactivate |
| `DELETE /api/Banks/{id}/hard` | `BanksController.HardDelete` | Authorize | hard delete |
| `GET /api/Branches` | `BranchesController.GetPaged` | Authorize | list branches |
| `GET /api/Branches/{id}` | `BranchesController.GetById` | Authorize | get branch |
| `POST /api/Branches` | `BranchesController.Create` | Authorize | create branch |
| `PUT /api/Branches/{id}` | `BranchesController.Update` | Authorize | update branch |
| `DELETE /api/Branches/{id}` | `BranchesController.Delete` | Authorize | soft delete |
| `DELETE /api/Branches/{id}/hard` | `BranchesController.HardDelete` | Authorize | hard delete |
| `PUT /api/Branches/{id}/active` | `BranchesController.SetActive` | Authorize | activate/deactivate |
| `GET /api/Stores` | `StoresController.GetPaged` | Authorize | list stores |
| `GET /api/Stores/{id}` | `StoresController.GetById` | Authorize | get store |
| `POST /api/Stores` | `StoresController.Create` | Authorize | create store |
| `PUT /api/Stores/{id}` | `StoresController.Update` | Authorize | update store |
| `DELETE /api/Stores/{id}` | `StoresController.Delete` | Authorize | soft delete |
| `DELETE /api/Stores/{id}/hard` | `StoresController.HardDelete` | Authorize | hard delete |
| `PUT /api/Stores/{id}/active` | `StoresController.SetActive` | Authorize | activate/deactivate |
| `GET /api/ProductCategories` | `ProductCategoriesController.GetPaged` | Authorize | list categories |
| `GET /api/ProductCategories/{id}` | `ProductCategoriesController.GetById` | Authorize | get category |
| `POST /api/ProductCategories` | `ProductCategoriesController.Create` | Authorize | create category |
| `PUT /api/ProductCategories/{id}` | `ProductCategoriesController.Update` | Authorize | update category |
| `DELETE /api/ProductCategories/{id}` | `ProductCategoriesController.Delete` | Authorize | soft delete |
| `DELETE /api/ProductCategories/{id}/hard` | `ProductCategoriesController.HardDelete` | Authorize | hard delete |
| `PUT /api/ProductCategories/{id}/active` | `ProductCategoriesController.SetActive` | Authorize | activate/deactivate |
| `GET /api/ProductTypes` | `ProductTypesController.GetPaged` | Authorize | list product types |
| `GET /api/ProductTypes/{id}` | `ProductTypesController.GetById` | Authorize | get product type |
| `POST /api/ProductTypes` | `ProductTypesController.Create` | Authorize | create product type |
| `PUT /api/ProductTypes/{id}` | `ProductTypesController.Update` | Authorize | update product type |
| `DELETE /api/ProductTypes/{id}` | `ProductTypesController.Delete` | Authorize | soft delete |
| `DELETE /api/ProductTypes/{id}/hard` | `ProductTypesController.HardDelete` | Authorize | hard delete |
| `PUT /api/ProductTypes/{id}/active` | `ProductTypesController.SetActive` | Authorize | activate/deactivate |
| `GET /api/ProductVariants` | `ProductVariantsController.GetPaged` | Authorize | list variants |
| `GET /api/ProductVariants/{id}` | `ProductVariantsController.GetById` | Authorize | get variant |
| `POST /api/ProductVariants` | `ProductVariantsController.Create` | Authorize | create variant |
| `PUT /api/ProductVariants/{id}` | `ProductVariantsController.Update` | Authorize | update variant |
| `DELETE /api/ProductVariants/{id}` | `ProductVariantsController.Delete` | Authorize | soft delete |
| `DELETE /api/ProductVariants/{id}/hard` | `ProductVariantsController.HardDelete` | Authorize | hard delete |
| `PUT /api/ProductVariants/{id}/active` | `ProductVariantsController.SetActive` | Authorize | activate/deactivate |
| `GET /api/Stocks` | `StocksController.GetPaged` | Authorize | list stocks (direct rows, no GroupBy) |
| `GET /api/Stocks/variant/{variantId}/detail` | `StocksController.GetVariantDetail` | Authorize | variant detail across store branches |
| `GET /api/Stocks/{id}` | `StocksController.GetById` | Authorize | get stock by GUID |
| `GET /api/Stocks/by-barcode/{barcode}` | `StocksController.GetByBarcode` | Authorize | get stock by barcode |
| `GET /api/Stocks/by-code/{code}` | `StocksController.GetByPublicCode` | Authorize | get stock by public code |
| `POST /api/Stocks` | `StocksController.Create` | Authorize | create/upsert stock |
| `PUT /api/Stocks/{id}` | `StocksController.Update` | Authorize | update stock |
| `POST /api/Stocks/backfill-public-codes` | `StocksController.BackfillPublicCodes` | Authorize | assign missing public codes |
| `DELETE /api/Stocks/{id}` | `StocksController.Delete` | Authorize | soft delete |
| `DELETE /api/Stocks/{id}/hard` | `StocksController.HardDelete` | Authorize | hard delete |
| `GET /api/Stocks/favorites` | `StocksController.GetFavorites` | Authorize | top sold products |
| `GET /r/{code}` | `ResolveController.Resolve` | AllowAnonymous | resolve public code -> redirect |
| `POST /api/Purchases` | `PurchasesController.Create` | Authorize | create purchase receipt |
| `GET /api/Purchases` | `PurchasesController.GetPaged` | Authorize | list purchases |
| `GET /api/Purchases/{id}` | `PurchasesController.GetById` | Authorize | purchase detail |
| `POST /api/Sales` | `SalesController.Create` | Authorize | unified receipt (sale + purchase) |
| `GET /api/Sales` | `SalesController.GetPaged` | Authorize | list sales |
| `GET /api/Sales/{id}` | `SalesController.GetById` | Authorize | sale line detail |
| `GET /api/Customers` | `CustomersController.GetPaged` | Authorize | list customers |
| `GET /api/Customers/{id}` | `CustomersController.GetById` | Authorize | get customer |
| `POST /api/Customers` | `CustomersController.Create` | Authorize | create customer |
| `PUT /api/Customers/{id}` | `CustomersController.Update` | Authorize | update customer |
| `DELETE /api/Customers/{id}` | `CustomersController.Delete` | Authorize | soft delete |
| `GET /api/PaymentMethods` | `PaymentMethodsController.GetPaged` | Authorize | list payment methods |
| `GET /api/PaymentMethods/{id}` | `PaymentMethodsController.GetById` | Authorize | get payment method |
| `POST /api/PaymentMethods` | `PaymentMethodsController.Create` | Authorize | create payment method |
| `PUT /api/PaymentMethods/{id}` | `PaymentMethodsController.Update` | Authorize | update payment method |
| `DELETE /api/PaymentMethods/{id}` | `PaymentMethodsController.Delete` | Authorize | soft delete |
| `GET /api/Limits` | `LimitsController.GetPaged` | Authorize | list limits |
| `GET /api/Limits/{id}` | `LimitsController.GetById` | Authorize | get limit |
| `POST /api/Limits` | `LimitsController.Create` | Authorize | create limit |
| `PUT /api/Limits/{id}` | `LimitsController.Update` | Authorize | update limit |
| `DELETE /api/Limits/{id}` | `LimitsController.Delete` | Authorize | delete |
| `GET /api/LifecycleActions` | `LifecycleActionsController.GetPaged` | Authorize | list lifecycle actions |
| `GET /api/LifecycleActions/{id}` | `LifecycleActionsController.GetById` | Authorize | get action |
| `POST /api/LifecycleActions` | `LifecycleActionsController.Create` | Authorize | create action |
| `PUT /api/LifecycleActions/{id}` | `LifecycleActionsController.Update` | Authorize | update action |
| `DELETE /api/LifecycleActions/{id}` | `LifecycleActionsController.Delete` | Authorize | delete action |
| `GET /api/ProductLifecycles` | `ProductLifecyclesController.GetPaged` | Authorize | list lifecycles |
| `GET /api/ProductLifecycles/{id}` | `ProductLifecyclesController.GetById` | Authorize | get lifecycle |
| `POST /api/ProductLifecycles` | `ProductLifecyclesController.Create` | Authorize | create lifecycle |
| `GET /api/Roles` | `RolesController.GetAll` | Authorize | list roles |
| `GET /api/Roles/{id}` | `RolesController.GetById` | Authorize | get role |
| `POST /api/Roles` | `RolesController.Create` | Authorize | create role |
| `PUT /api/Roles/{id}` | `RolesController.Update` | Authorize | update role |
| `DELETE /api/Roles/{id}` | `RolesController.Delete` | Authorize | delete role |
| `GET /api/Users` | `UsersController.GetPaged` | Authorize | list users |
| `GET /api/Users/{id}` | `UsersController.GetById` | Authorize | get user |
| `PUT /api/Users/{id}` | `UsersController.Update` | Authorize | update user |
| `DELETE /api/Users/{id}` | `UsersController.Delete` | Authorize | soft delete |
| `DELETE /api/Users/{id}/hard` | `UsersController.HardDelete` | Authorize | hard delete |
| `GET /api/ThermalPrinters` | `ThermalPrintersController.GetPaged` | Authorize | list printers |
| `GET /api/ThermalPrinters/{id}` | `ThermalPrintersController.GetById` | Authorize | get printer |
| `POST /api/ThermalPrinters` | `ThermalPrintersController.Create` | Authorize | create printer |
| `PUT /api/ThermalPrinters/{id}` | `ThermalPrintersController.Update` | Authorize | update printer |
| `DELETE /api/ThermalPrinters/{id}` | `ThermalPrintersController.Delete` | Authorize | soft delete |
| `DELETE /api/ThermalPrinters/{id}/hard` | `ThermalPrintersController.HardDelete` | Authorize | hard delete |
| `GET /api/Dashboard/summary` | `DashboardController.GetSummary` | Authorize | summary (no broadcast) |
| `GET /api/Dashboard/live-counters` | `DashboardController.GetLiveCounters` | Authorize | live counters + broadcast |
| `GET /api/Dashboard/daily-summary` | `DashboardController.GetDailySummary` | Authorize | daily summary + broadcast |
| `GET /api/Dashboard/anomalies` | `DashboardController.GetAnomalies` | Authorize | anomalies + broadcast |
| `GET /api/Dashboard/top-products` | `DashboardController.GetTopProducts` | Authorize | top products |
| `GET /api/Dashboard/profit-loss` | `DashboardController.GetProfitLoss` | Authorize | profit/loss report |
| `DELETE /api/Dev/cleanup-all-data` | `DevController.CleanupAllData` | Authorize | DEV only, deletes transactional data |

**Request/response shape changes (inventory + receipts)**
- Stock create payload includes `StockCreateDto.TotalWeightGram`.  
  - `KuyumStokApi.Application/DTOs/Stocks/StocksDto.cs` (`StockCreateDto`)
- Unified receipt sale/purchase items include `TotalWeightGram`.  
  - `KuyumStokApi.Application/DTOs/Receipts/UnifiedReceiptDto.cs` (`UnifiedReceiptSaleItem`, `UnifiedReceiptPurchaseItem`)
- Sale/purchase item DTOs include `TotalWeightGram`.  
  - `KuyumStokApi.Application/DTOs/Sales/SaleItemDto.cs`  
  - `KuyumStokApi.Application/DTOs/Purchase/PurchaseItemDto.cs`
- Sale list/detail `AgirlikGram` values are sourced from line totals (`SaleDetails.TotalWeightGram`).  
  - `KuyumStokApi.Infrastructure/Services/SalesService/SalesService.cs` (`GetPagedAsync`, `GetLineByIdAsync`)
- Stock list `StockDto.Id` is a real `Stocks.Id`, and `StockDto.TotalWeight` is `Stocks.TotalWeightGram`.  
  - `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs` (`GetPagedAsync`)

## 6) AuthN/AuthZ (flow, tokens, roles/scopes, enforcement points)

**AuthN (JWT)**
- JWT 생성: `JwtService.GenerateToken(Users)`  
  - `KuyumStokApi.Infrastructure/Services/JwtService/JwtService.cs`
- Claims include: `sub`, `jti`, `unique_name`, `given_name`, `surname`, `role_id`, `branch_id`, `is_active`, `must_change_password`.  
  - `JwtService.BuildClaims`
- JWT validation + blacklist check:  
  - `Program.cs` `AddJwtBearer` + `OnTokenValidated` → `ITokenBlacklistService.IsTokenInvalidatedAsync`

**Refresh tokens**
- Refresh token generation & rotation: `RefreshTokenService.GenerateRefreshTokenAsync`, `RevokeRefreshTokenAsync`.  
  - `KuyumStokApi.Infrastructure/Services/RefreshTokenService/RefreshTokenService.cs`
- Auth endpoints:  
  - `AuthController.Refresh`, `AuthController.Logout`  
  - `KuyumStokApi.API/Controllers/AuthController.cs`

**Password policy + hashing**
- Policy (length, character classes, blocklist, repetition/sequential checks).  
  - `KuyumStokApi.Infrastructure/Security/PasswordPolicy.cs`
- Hashing: SHA-256 + salt + pepper + iterations.  
  - `KuyumStokApi.Infrastructure/PasswordHasher/PasswordHasher.cs`

**AuthZ**
- `[Authorize]` used on most controllers and SignalR hub.  
  - Controllers in `KuyumStokApi.API/Controllers/*.cs`, `KuyumStokApi.Application/Hubs/DashboardHub.cs`
- No role-based policy attributes found in controllers (only simple `[Authorize]`).  
  - Example: `BanksController`, `UsersController`

## 7) Configuration & Environments (sources + env var table + where used)

**Config sources**
- `appsettings.json` (base)  
  - `KuyumStokApi.API/appsettings.json`
- `appsettings.Development.json` (dev overrides)  
  - `KuyumStokApi.API/appsettings.Development.json`
- `launchSettings.json` sets `ASPNETCORE_ENVIRONMENT=Development`.  
  - `KuyumStokApi.API/Properties/launchSettings.json`

**Options binding**
- `Jwt`, `Password`, `QrCode` bound in DI with validation.  
  - `KuyumStokApi.Infrastructure/DependencyInjection.cs`

**Environment Variables**

| name | required? | used in | description | example |
|---|---|---|---|---|
| `SKIP_DB_MIGRATE` | optional | `DbInitExtensions.MigrateAndSeedAsync` | `true` → skip migration + seed | `true` |
| `ASPNETCORE_ENVIRONMENT` | optional | ASP.NET Core runtime | environment selection | `Development` |
| `ASPNETCORE_URLS` | optional | Dockerfile runtime | bind URL | `http://0.0.0.0:5000` |
| `ASPNETCORE_HTTP_PORTS` | optional | launch profile | container HTTP port | `8080` |
| `ASPNETCORE_HTTPS_PORTS` | optional | launch profile | container HTTPS port | `8081` |

**Other config values (non-env)**
- `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, `Jwt:ExpiryMinutes`, `Jwt:KeyId`.  
  - `KuyumStokApi.API/appsettings.json`
- `Password:Iterations`, `Password:Pepper`.  
  - `KuyumStokApi.API/appsettings.json`
- `ConnectionStrings:DefaultConnection` (PostgreSQL).  
  - `KuyumStokApi.API/appsettings.json`
- `QrCode:BaseUrl`, `QrCode:ResolvePath`, `QrCode:FrontendBaseUrl`, `QrCode:ErrorCorrection`, `QrCode:TargetPixelSize`, `QrCode:MinPixelsPerModule`, `QrCode:MaxPixelsPerModule`.  
  - Resolve/yonlendirme ve UI routing icin kullanilir; **QR payload'i degildir**.  
  - `KuyumStokApi.API/appsettings.json`, `KuyumStokApi.Infrastructure/QrCode/QrCodeOptions.cs`

## 8) External Integrations (purpose, endpoints/topics, credentials, retries, key code locations)

**PostgreSQL (Npgsql)**
- Purpose: primary data store.  
  - `KuyumStokApi.Persistence/DependencyInjection.cs`, `appsettings.json`

**SignalR**
- Purpose: real-time dashboard updates.  
  - Hub: `/api/hubs/dashboard` in `Program.cs`  
  - Hub class: `KuyumStokApi.Application/Hubs/DashboardHub.cs`

**QR Code generation**
- Purpose: generate QR code image as Base64 for stock items.  
  - `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs`, `KuyumStokApi.Infrastructure/QrCode/QrCodeService.cs`
- QR payload = PublicCode only (raw short code string).  
  - `/r/{code}` resolve/redirect ayri bir mekanizmadir; QR icerigine eklenmez.
  - QR tarama sonucu frontend tarafinda link/yonlendirmeye cevrilir.
- `StockDto.PublicCode` tarama sonucu payload degeridir; `StockDto.QrCode` bu payload'in Base64 PNG goruntusudur; URL embed edilmez.

**ML (Microsoft.ML)**
- Purpose: anomaly detection + workload estimation (internal computation).  
  - Packages referenced in `KuyumStokApi.Infrastructure/KuyumStokApi.Infrastructure.csproj`  
  - Services: `KuyumStokApi.Infrastructure/Services/AnomalyDetectionService/*`, `KuyumStokApi.Infrastructure/Services/WorkloadEstimationService/*`

**Unknown**
- Any external HTTP APIs or message brokers are not found in this scan. If needed, inspect `KuyumStokApi.Infrastructure/Services/*` for additional integrations.

## 9) Background Jobs / Schedulers (cron/workers/listeners)

- `DashboardBackgroundService` (`BackgroundService`) runs in-process.  
  - Periodic broadcasts: live counters (30s), daily summary (1m), anomalies (2m).  
  - `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardBackgroundService.cs`

## 10) Observability (logs, tracing, metrics, health checks)

- Logging via `ILogger` in services and hubs.  
  - `DashboardBackgroundService`, `DashboardService`, `DashboardHub`
- JWT events log to console.  
  - `Program.cs` (`OnAuthenticationFailed`, `OnChallenge`, `OnTokenValidated`)
- No explicit metrics or health checks found.  
  - Unknown: add `AspNetCore.HealthChecks` if required.

## 11) Build/Run/Test/Deploy (exact commands, docker/k8s/CI if exists)

**Runbook (copy/paste)**

| task | command |
|---|---|
| Restore | `dotnet restore KuyumStokApi.sln` |
| Build | `dotnet build KuyumStokApi.sln` |
| Run API (dev) | `dotnet run --project KuyumStokApi.API/KuyumStokApi.API.csproj` |
| Run with HTTPS profile | `dotnet run --project KuyumStokApi.API/KuyumStokApi.API.csproj --launch-profile https` |
| Test | `dotnet test KuyumStokApi.Tests/KuyumStokApi.Tests.csproj` |
| EF migration add | `dotnet ef migrations add <Name> --project KuyumStokApi.Persistence --startup-project KuyumStokApi.API` |
| EF database update | `dotnet ef database update --project KuyumStokApi.Persistence --startup-project KuyumStokApi.API` |
| Docker build | `docker build -t kuyumstokapi .` |
| Docker run | `docker run -p 5000:5000 kuyumstokapi` |

**Deploy**
- Dockerfile publishes `KuyumStokApi.API` and runs `dotnet KuyumStokApi.API.dll` on port 5000.  
  - `Dockerfile`
- CI/CD scripts not found.  
  - Unknown: inspect repository root for pipeline definitions if needed.

**Tests**
- Test projesi: `KuyumStokApi.Tests`  
  - `PublicCodeServiceTests` (public code length/alfabe/normalize/validation)  
  - `StocksServiceTests` (public code + QR payload/backfill)
  - `KuyumStokApi.Tests/PublicCodeServiceTests.cs`, `KuyumStokApi.Tests/StocksServiceTests.cs`

## 12) How to Safely Change This Project (invariants, gotchas, what to regression test)

**Key invariants / gotchas**
- **Soft delete**: many entities use `ISoftDeletable` and are filtered globally. Use `IgnoreQueryFilters()` when needed.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`
- **Dashboard notifications**: `SaveChanges` triggers dashboard broadcasts; ensure new entity changes are mapped if needed.  
  - `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs`, `KuyumStokApi.Infrastructure/Services/DashboardService/DashboardNotificationService.cs`
- **Inventory uniqueness**: `Stocks` is unique per `(BranchId, ProductVariantId)` and enforced by a unique index.  
  - `AppDbContext.OnModelCreating` in `KuyumStokApi.Persistence/Contexts/AppDbContext.cs`
- **PublicCode**: `Stocks.PublicCode` is a 10-char Crockford Base32 code (normalized) and unique when present.  
  - `KuyumStokApi.Infrastructure/Services/PublicCodeService/PublicCodeService.cs`, `KuyumStokApi.Persistence/Migrations/20260127120000_AddStockPublicCode.cs`
- **QR payload**: QR payload = PublicCode only (raw short code string).  
  - Resolve mekanizmasi `/r/{code}` ile ayridir.
- **TotalWeightGram semantics**:  
  - `Stocks.TotalWeightGram` is total on-hand weight; `SaleDetails.TotalWeightGram` and `PurchaseDetails.TotalWeightGram` are line totals.  
  - `KuyumStokApi.Domain/Entities/Stocks.cs`, `SaleDetails.cs`, `PurchaseDetails.cs`
- **Validation of totals**:  
  - `StocksService.CreateAsync` requires `Quantity > 0` and `TotalWeightGram > 0` (fallbacks to `StockCreateDto.Gram` only if `TotalWeightGram` is missing).  
  - `SalesService.ProcessSaleAsync` requires `Quantity > 0`, `TotalWeightGram > 0`, `SoldPrice > 0`, and checks stock totals before decrement.  
  - `SalesService.ProcessPurchaseAsync` and `PurchasesService.CreateAsync` require `TotalWeightGram > 0`.  
  - `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs`, `SalesService.cs`, `PurchasesService.cs`

**Regression tests to run after changes**
- Auth flow: register → login → refresh → logout.  
  - `AuthController`, `JwtService`, `RefreshTokenService`
- Stock creation/upsert + QR generation.  
  - `StocksService.CreateAsync`
- Stock listing + variant detail totals (ensure `TotalWeightGram` mapping).  
  - `StocksService.GetPagedAsync`, `StocksService.GetVariantDetailInStoreAsync`
- Unified receipt sales/purchases update both quantity and total weight.  
  - `SalesController`, `SalesService`
- Standalone purchases flow updates `Stocks.TotalWeightGram`.  
  - `PurchasesController`, `PurchasesService`
- Soft delete filtering (list endpoints vs. hard delete).  
  - `AppDbContext.Partials.cs`, CRUD services
- SignalR dashboard: initial connection + periodic broadcasts.  
  - `DashboardHub`, `DashboardBackgroundService`

## 13) Backlog / TODO extraction (TODO/FIXME + docs notes)

- No `TODO`/`FIXME` markers found in repository source.  
  - `rg` scan across repo found none.

## 14) Final Summary (10 bullets)

- API is a .NET 8 layered architecture with separate Application/Domain/Infrastructure/Persistence projects.
- Startup is in `KuyumStokApi.API/Program.cs` and applies migrations + seed on boot.
- EF Core (PostgreSQL) is the data layer with global soft-delete filters.
- JWT authentication uses HS256 with refresh token rotation and blacklist support.
- Most endpoints are `[Authorize]`, with explicit anonymous auth endpoints in `AuthController`.
- SignalR hub `/api/hubs/dashboard` provides live updates and initial data payloads.
- Dashboard updates are sent both periodically (background service) and on data changes (SaveChanges hooks).
- Inventory model is one `Stocks` row per `(BranchId, ProductVariantId)` with total gram tracking.
- Dockerfile builds and runs the API on port 5000 in container.
- Test projesi mevcut (`KuyumStokApi.Tests`); CI/CD scriptleri bulunamadi.  

---

## Key Files Index

| file path | what it contains | why it matters |
|---|---|---|
| `KuyumStokApi.API/Program.cs` | DI, middleware, auth, Swagger, SignalR, migration boot | app entry + pipeline |
| `KuyumStokApi.API/Controllers/*.cs` | HTTP endpoints | API surface |
| `KuyumStokApi.Application/Interfaces/*` | service contracts | boundary between API and Infra |
| `KuyumStokApi.Application/Common/ApiResult.cs` | standard API response wrapper | shared response format |
| `KuyumStokApi.Application/DTOs/Receipts/UnifiedReceiptDto.cs` | unified receipt DTOs | sale/purchase payloads |
| `KuyumStokApi.Application/DTOs/Stocks/StocksDto.cs` | stock DTOs + `TotalWeightGram` | inventory payloads |
| `KuyumStokApi.Domain/Entities/*.cs` | EF entities | domain model |
| `KuyumStokApi.Persistence/Contexts/AppDbContext.cs` | DbSet + mapping + unique index | DB schema config |
| `KuyumStokApi.Persistence/Contexts/AppDbContext.Partials.cs` | soft delete + dashboard notifications | global behavior |
| `KuyumStokApi.Persistence/Migrations/20260119120000_StockTotalsByVariantBranch.cs` | inventory migration + consolidation | schema + data alignment |
| `KuyumStokApi.Persistence/Extensions/DbInitExtensions.cs` | migration + seed on startup | DB initialization |
| `KuyumStokApi.Infrastructure/DependencyInjection.cs` | service wiring | runtime dependencies |
| `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs` | stock listing/upsert | inventory correctness |
| `KuyumStokApi.Infrastructure/Services/SalesService/SalesService.cs` | unified receipt flow | stock decrement/increment |
| `KuyumStokApi.Infrastructure/Services/PurchasesService/PurchasesService.cs` | purchase flow | stock increment |
| `KuyumStokApi.Infrastructure/Services/DashboardService/*` | dashboard logic + background worker | real-time analytics |
| `Dockerfile` | container build/runtime | deployment |
| `KuyumStokApi.API/appsettings.json` | core config | runtime settings |
