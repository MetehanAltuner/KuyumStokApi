# KUYUMSTOKAPI - EKSİKSİZ DETAYLI PROJE DOKÜMANTASYONU

> **Güncellenme Tarihi**: 8 Aralık 2025  
> **Proje**: Kuyum (Kuyumcu) Stok Yönetim Sistemi API  
> **Teknoloji**: ASP.NET Core 8.0 Web API, Entity Framework Core, PostgreSQL  
> **Mimari**: Clean Architecture (Layered Architecture)  
> **Kapsam**: Tüm entity'ler, nested class'lar, servisler, DTO'lar ve ilişkiler

---

## 📋 İÇİNDEKİLER

1. [Proje Genel Bakış](#proje-genel-bakış)
2. [Mimari Yapı](#mimari-yapı)
3. [Katmanlar ve Sorumlulukları](#katmanlar-ve-sorumlulukları)
4. [Veritabanı Entity'leri - Detaylı Açıklama](#veritabanı-entityleri)
5. [Servisler ve İş Mantığı](#servisler-ve-iş-mantığı)
6. [Controller'lar ve API Endpoint'leri](#controllerlar-ve-api-endpointleri)
7. [DTO'lar (Data Transfer Objects)](#dtolar)
8. [Güvenlik ve Kimlik Doğrulama](#güvenlik-ve-kimlik-doğrulama)
9. [Önemli Özellikler ve Desenler](#önemli-özellikler-ve-desenler)
10. [İlişkiler ve Bağımlılıklar](#ilişkiler-ve-bağımlılıklar)

---

## 1. PROJE GENEL BAKIŞ

### 1.1 Amaç
**KuyumStokApi**, kuyumculuk sektörüne özel bir stok yönetim sistemidir. Sistem, kuyumcu mağazalarının ve şubelerinin:
- **Ürün stok takibi** (altın, gümüş, pırlanta vb.)
- **Alış-satış işlemleri**
- **Müşteri yönetimi**
- **Ödeme yöntemleri**
- **Banka işlemleri** (POS komisyon takibi)
- **Ürün yaşam döngüsü takibi**
- **Kullanıcı ve rol yönetimi**

gibi tüm operasyonlarını dijital ortamda yönetmesini sağlar.

### 1.2 Temel İş Akışları

#### Alış (Purchase) Akışı:
1. Kullanıcı, tedarikçiden veya müşteriden ürün alır
2. Her ürün için stok kaydı oluşturulur (barcode ile benzersiz)
3. Alış detayları (fiyat, adet, vb.) kaydedilir
4. Stok miktarı artırılır
5. Ürün yaşam döngüsü kaydı oluşturulur

#### Satış (Sales) Akışı:
1. Müşteriye satış yapılır
2. Stoktan ürünler düşülür
3. Satış detayları kaydedilir
4. Ödeme yöntemi belirlenir
5. Opsiyonel: Banka işlemi (POS komisyonu) kaydedilir
6. Ürün yaşam döngüsü güncellenir

---

## 2. MİMARİ YAPI

Proje, **Clean Architecture** prensiplerine göre katmanlara ayrılmıştır:

```
KuyumStokApi/
├── KuyumStokApi.API/              # Presentation Layer (API Endpoints)
├── KuyumStokApi.Application/      # Application Layer (DTOs, Interfaces)
├── KuyumStokApi.Domain/           # Domain Layer (Entities, Business Models)
├── KuyumStokApi.Infrastructure/   # Infrastructure Layer (Services Implementation)
└── KuyumStokApi.Persistence/      # Persistence Layer (DbContext, Data Access)
```

### 2.1 Bağımlılık Yönü
```
API → Application → Infrastructure → Persistence
                 ↓
              Domain (Core - Hiçbir bağımlılığı yok)
```

---

## 3. KATMANLAR VE SORUMLULUKLARI

### 3.1 Domain Layer (KuyumStokApi.Domain)

**Amaç**: İş kurallarının ve veri modellerinin tanımlandığı, hiçbir dış bağımlılığı olmayan çekirdek katman.

**İçerik**:
- **Entities/**: Veritabanı tablolarını temsil eden C# sınıfları
- **Common/**: Ortak interface'ler (ISoftDeletable, IActivatable)

**Özellikler**:
- Partial class yapısı kullanılarak genişletilebilir
- Soft Delete desteği (IsDeleted, DeletedAt, DeletedBy)
- Aktiflik durumu yönetimi (IsActive)
- Navigation property'ler ile ilişkiler

### 3.2 Application Layer (KuyumStokApi.Application)

**Amaç**: İş mantığı interface'lerinin ve veri transfer objelerinin tanımlandığı katman.

**İçerik**:
- **DTOs/**: API ile iletişimde kullanılan veri modelleri
- **Interfaces/Services/**: Servis kontratları (interface'ler)
- **Interfaces/Auth/**: Kimlik doğrulama interface'leri
- **Common/**: Ortak sınıflar (ApiResult, PagedResult, Extensions)

**Özellikler**:
- DTO'lar ile entity'leri dış dünyadan izole eder
- Generic ApiResult<T> yapısı ile standart API yanıtları
- Sayfalama desteği (PagedResult)

### 3.3 Infrastructure Layer (KuyumStokApi.Infrastructure)

**Amaç**: İş mantığının somut implementasyonlarının bulunduğu katman.

**İçerik**:
- **Services/**: Her entity için CRUD ve özel iş mantığı servisleri
- **Auth/**: Kullanıcı kimlik doğrulama (CurrentUserService)
- **PasswordHasher/**: Güvenli parola hash'leme
- **Security/**: Parola politikaları
- **DependencyInjection.cs**: Dependency Injection yapılandırması

**Servisler**:
- BanksService
- BranchesService
- CustomersService
- DashboardService (Dashboard verileri ve analitik)
- JwtService (Token üretimi)
- LifecycleActionsService
- LimitsService
- PaymentMethodsService
- ProductCategoryService
- ProductLifecycleService
- ProductTypeService
- ProductVariantService
- PurchasesService
- RefreshTokenService (Refresh token yönetimi)
- RolesService
- SalesService
- StocksService
- StoresService
- ThermalPrintersService
- TokenBlacklistService (JWT token blacklist yönetimi)
- UserService

### 3.4 Persistence Layer (KuyumStokApi.Persistence)

**Amaç**: Veritabanı erişiminin yönetildiği katman.

**İçerik**:
- **Contexts/AppDbContext.cs**: Entity Framework DbContext
- **DependencyInjection.cs**: Veritabanı bağlantısı yapılandırması

**Özellikler**:
- PostgreSQL veritabanı desteği
- Fluent API ile tablo ve kolon yapılandırmaları
- Foreign Key ilişkileri
- Default değerler ve constraint'ler
- Partial class desteği (AppDbContext.CurrentUser.cs, AppDbContext.Partials.cs)

### 3.5 API Layer (KuyumStokApi.API)

**Amaç**: HTTP endpoint'lerinin ve middleware'lerin bulunduğu presentation katmanı.

**İçerik**:
- **Controllers/**: RESTful API controller'ları
- **Program.cs**: Uygulama başlatma ve middleware yapılandırması
- **appsettings.json**: Yapılandırma dosyası

**Controller'lar**:
- AuthController
- BanksController
- BranchesController
- CustomersController
- LifecycleActionsController
- LimitsController
- PaymentMethodsController
- ProductCategoriesController
- ProductLifecyclesController
- ProductTypeController
- ProductVariantController
- PurchaseController
- RolesController
- SalesController
- StocksController
- StoresController

---

## 4. VERİTABANI ENTITY'LERİ

### 4.1 Users (Kullanıcılar)

**Tablo**: `users`  
**Amaç**: Sistemdeki kullanıcıların kimlik bilgilerini ve yetki seviyelerini saklar.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar (Primary Key) |
| **Username** | string | Kullanıcı adı (Unique - Benzersiz) |
| **PasswordHash** | string | Hash'lenmiş parola (SHA-256 + Salt + Pepper) |
| **PasswordSalt** | string | Parola için kullanılan salt değeri (Base64) |
| **FirstName** | string? | Kullanıcının adı |
| **LastName** | string? | Kullanıcının soyadı |
| **RoleId** | int? | Kullanıcının rolü (Foreign Key → Roles) |
| **BranchId** | int? | Kullanıcının atandığı şube (Foreign Key → Branches) |
| **IsActive** | bool? | Hesap aktif mi? |
| **IsDeleted** | bool | Soft delete - Silinmiş mi? |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Kim tarafından silindi? |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |
| **MustChangePassword** | bool | İlk girişte parola değiştirme zorunluluğu (default: false) |

**İlişkiler**:
- **Role** (Many-to-One): Bir kullanıcının bir rolü vardır
- **Branch** (Many-to-One): Bir kullanıcı bir şubeye atanır
- **Purchases** (One-to-Many): Kullanıcı birden fazla alış işlemi yapabilir
- **Sales** (One-to-Many): Kullanıcı birden fazla satış işlemi yapabilir
- **ProductLifecycles** (One-to-Many): Kullanıcı ürün hareketleri kaydeder
- **RefreshTokens** (One-to-Many): Kullanıcının refresh token'ları

**İş Mantığı**:
- Kullanıcı adı benzersiz olmalıdır
- Parola güvenliği: SHA-256 hash + iterasyon + salt + pepper
- Bir kullanıcı sadece kendi şubesindeki stokları görebilir (genelde)
- Soft delete ile kullanıcı kalıcı silinmez

---

### 4.2 Roles (Roller)

**Tablo**: `roles`  
**Amaç**: Kullanıcı yetki seviyelerini tanımlar (Admin, Manager, Cashier, vb.)

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **Name** | string | Rol adı (Admin, Manager, Cashier) |
| **IsActive** | bool | Rol aktif mi? |
| **IsDeleted** | bool | Soft delete |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Users** (One-to-Many): Bir rol birden fazla kullanıcıya atanabilir

**Örnek Roller**:
- **Admin**: Tam yetki
- **Manager**: Şube yönetimi
- **Cashier**: Satış işlemleri
- **Viewer**: Sadece görüntüleme

---

### 4.3 Stores (Mağazalar)

**Tablo**: `stores`  
**Amaç**: Ana mağaza/işletme bilgilerini tutar. Bir işletmenin birden fazla şubesi olabilir.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **Name** | string | Mağaza adı |
| **IsActive** | bool | Mağaza aktif mi? |
| **IsDeleted** | bool | Soft delete |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Branches** (One-to-Many): Bir mağazanın birden fazla şubesi olabilir

**Örnek Senaryo**:
- Mağaza: "Altın Dünyası A.Ş."
  - Şube 1: "Altın Dünyası - Ankara"
  - Şube 2: "Altın Dünyası - İstanbul"
  - Şube 3: "Altın Dünyası - İzmir"

---

### 4.4 Branches (Şubeler)

**Tablo**: `branches`  
**Amaç**: Mağazaların fiziksel şubelerini temsil eder. Her şube ayrı stok tutabilir.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **StoreId** | int? | Bağlı olduğu mağaza (Foreign Key → Stores) |
| **Name** | string | Şube adı |
| **Address** | string? | Şube adresi |
| **IsActive** | bool | Şube aktif mi? |
| **IsDeleted** | bool | Soft delete |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Store** (Many-to-One): Bir şube bir mağazaya bağlıdır
- **Users** (One-to-Many): Şubede çalışan kullanıcılar
- **Stocks** (One-to-Many): Şubedeki stok kalemleri
- **Purchases** (One-to-Many): Şubenin alış işlemleri
- **Sales** (One-to-Many): Şubenin satış işlemleri
- **Limits** (One-to-Many): Şube için belirlenen stok limitleri

**İş Mantığı**:
- Her kullanıcı bir şubeye atanır
- Stoklar şube bazlı yönetilir
- Şubeler arası stok transferi yapılabilir (ProductLifecycles ile)

---

### 4.5 ProductCategories (Ürün Kategorileri)

**Tablo**: `product_categories`  
**Amaç**: Ürünlerin genel kategorilerini tanımlar (Yüzük, Kolye, Bilezik, vb.)

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **Name** | string | Kategori adı (Yüzük, Kolye, Bilezik) |
| **IsActive** | bool | Kategori aktif mi? |
| **IsDeleted** | bool | Soft delete |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **ProductTypes** (One-to-Many): Bir kategorinin birden fazla tipi olabilir

**Hiyerarşi**:
```
ProductCategories (Yüzük)
  └── ProductTypes (Nişan Yüzüğü, Alyans, Taşlı Yüzük)
      └── ProductVariants (14 ayar, Beyaz Altın, Cartier marka, vb.)
```

**Örnek Kategoriler**:
- Yüzük
- Kolye
- Bilezik
- Küpe
- Set
- Saat

---

### 4.6 ProductTypes (Ürün Tipleri)

**Tablo**: `product_types`  
**Amaç**: Kategorilerin alt tiplerini tanımlar (Nişan Yüzüğü, Alyans, vb.)

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **Name** | string | Tip adı (Nişan Yüzüğü, Alyans) |
| **CategoryId** | int? | Bağlı olduğu kategori (Foreign Key → ProductCategories) |
| **IsActive** | bool | Tip aktif mi? |
| **IsDeleted** | bool | Soft delete |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Category** (Many-to-One): Bir tip bir kategoriye bağlıdır
- **ProductVariants** (One-to-Many): Bir tipin birden fazla varyantı olabilir

**Örnek**:
- Kategori: Yüzük
  - Tip: Nişan Yüzüğü
  - Tip: Alyans
  - Tip: Taşlı Yüzük
  - Tip: Şövalye Yüzüğü

---

### 4.7 ProductVariants (Ürün Varyantları)

**Tablo**: `product_variants`  
**Amaç**: Ürünlerin detaylı özelliklerini tanımlar (ayar, renk, marka, vb.)

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **ProductTypeId** | int? | Bağlı olduğu tip (Foreign Key → ProductTypes) |
| **Name** | string | Varyant adı |
| **Ayar** | string? | Altın ayarı (8, 10, 14, 18, 22, 24) |
| **Brand** | string? | Marka (Cartier, Tiffany, vs.) |
| **Color** | string? | Renk (Sarı, Beyaz, Rose) |
| **IsActive** | bool | Varyant aktif mi? |
| **IsDeleted** | bool | Soft delete |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**Unique Constraint**:
```sql
UNIQUE (ProductTypeId, Name, Brand, Ayar, Color)
```
Aynı özelliklerle iki varyant oluşturulamaz.

**İlişkiler**:
- **ProductType** (Many-to-One): Bir varyant bir tipe bağlıdır
- **Stocks** (One-to-Many): Bir varyantın birden fazla stok kalemi olabilir
- **Limits** (One-to-Many): Varyant için stok limitleri

**Örnek**:
```
Kategori: Yüzük
  └── Tip: Nişan Yüzüğü
      └── Varyant: 14 Ayar Beyaz Altın Cartier Nişan Yüzüğü
```

**Kuyumculukta "Ayar" Nedir?**
Ayar, altının saflık derecesidir:
- **24 ayar**: %100 saf altın
- **22 ayar**: %91.67 altın
- **18 ayar**: %75 altın
- **14 ayar**: %58.5 altın
- **10 ayar**: %41.67 altın
- **8 ayar**: %33.3 altın

---

### 4.8 Stocks (Stok Kalemleri)

**Tablo**: `stocks`  
**Amaç**: Fiziksel ürün stoklarını takip eder. Her stok kalemi bir barkod ile benzersizdir.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **ProductVariantId** | int? | Hangi varyant (Foreign Key → ProductVariants) |
| **BranchId** | int? | Hangi şubede (Foreign Key → Branches) |
| **Quantity** | int? | Adet (kaç tane) |
| **Barcode** | string | Benzersiz barkod (Unique) |
| **QrCode** | string? | Opsiyonel QR kod |
| **Gram** | decimal? | Ürünün gram ağırlığı |
| **Thickness** | decimal? | Kalınlık (mm) |
| **Width** | decimal? | Genişlik (mm) |
| **StoneType** | string? | Taş tipi (Pırlanta, Yakut, vb.) |
| **Carat** | decimal? | Taş ağırlığı (karat) |
| **Milyem** | int? | Altın saflık değeri (1000'de kaç) |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**Unique Constraint**:
```sql
UNIQUE (Barcode)
```

**İlişkiler**:
- **ProductVariant** (Many-to-One): Bir stok kalemi bir varyanta aittir
- **Branch** (Many-to-One): Stok bir şubede bulunur
- **PurchaseDetails** (One-to-Many): Alış kayıtları
- **SaleDetails** (One-to-Many): Satış kayıtları
- **ProductLifecycles** (One-to-Many): Ürün hareketleri

**İş Mantığı**:
- Barcode benzersiz olmalıdır
- Quantity negatif olamaz
- Satışta stok düşer, alışta artar
- Stok hareketi her zaman lifecycle'a kaydedilir

**"Milyem" Nedir?**
Milyem, altının 1000 üzerinden saflığını gösteren birimdir:
- 24 ayar = 1000 milyem
- 18 ayar = 750 milyem
- 14 ayar = 585 milyem

---

### 4.9 Customers (Müşteriler)

**Tablo**: `customers`  
**Amaç**: Alış ve satış yapılan müşterileri kaydeder.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **Name** | string | Müşteri adı |
| **Phone** | string? | Telefon numarası |
| **Note** | string? | Notlar (TC kimlik, vs.) |
| **IsActive** | bool | Müşteri aktif mi? |
| **IsDeleted** | bool | Soft delete |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Purchases** (One-to-Many): Müşteriden yapılan alışlar
- **Sales** (One-to-Many): Müşteriye yapılan satışlar

**İş Mantığı**:
- Satış sırasında müşteri yoksa inline olarak oluşturulabilir
- TC kimlik bilgisi Note alanına kaydedilebilir
- Aynı ad-telefon ile müşteri aranır, yoksa yeni oluşturulur

---

### 4.10 PaymentMethods (Ödeme Yöntemleri)

**Tablo**: `payment_methods`  
**Amaç**: Ödeme türlerini tanımlar (Nakit, Kredi Kartı, Havale, vb.)

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **Name** | string | Ödeme yöntemi adı (Nakit, Kredi Kartı) |
| **IsActive** | bool | Aktif mi? (default: true) |
| **IsDeleted** | bool | Soft delete (default: false) |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |

**NOT**: Bu entity'de `CreatedAt` ve `UpdatedAt` property'leri YOKTUR. Bu bir tutarsızlıktır, diğer entity'lerle uyumlu değildir.

**İlişkiler**:
- **Purchases** (One-to-Many): Alışlarda kullanılan ödeme yöntemleri
- **Sales** (One-to-Many): Satışlarda kullanılan ödeme yöntemleri
- **SalePayments** (One-to-Many): Çoklu ödeme desteği

**Örnek Ödeme Yöntemleri**:
- Nakit
- Kredi Kartı (POS)
- Banka Havalesi
- Çek
- Altın Takası

---

### 4.11 Banks (Bankalar)

**Tablo**: `banks`  
**Amaç**: POS cihazlarının bağlı olduğu bankaları tanımlar.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **Name** | string | Banka adı (Ziraat Bankası, İş Bankası) |
| **Description** | string? | Açıklama |
| **IsActive** | bool | Aktif mi? (default: true) |
| **IsDeleted** | bool | Soft delete (default: false) |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **UpdatedAt** | DateTime? | Güncellenme tarihi (default: CURRENT_TIMESTAMP) |

**NOT**: Bu entity'de `CreatedAt` property'si YOKTUR. Sadece `UpdatedAt` mevcuttur. Bu bir tutarsızlıktır, diğer entity'lerle uyumlu değildir.

**İlişkiler**:
- **BankTransactions** (One-to-Many): Banka işlemleri
- **SalePayments** (One-to-Many): POS ödemeleri

**Amaç**:
POS ile yapılan ödemelerde komisyon takibi için kullanılır.

---

### 4.12 BankTransactions (Banka İşlemleri)

**Tablo**: `bank_transactions`  
**Amaç**: POS ile yapılan satışlarda komisyon ve banka bilgilerini kaydeder. **NOT**: Artık `SalePayments` tablosu kullanılıyor, bu tablo eski yapı için kalabilir.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **SaleId** | int? | Hangi satış (Foreign Key → Sales) |
| **BankId** | int? | Hangi banka (Foreign Key → Banks) |
| **CommissionRate** | decimal? | Komisyon oranı (%2.5 gibi) |
| **ExpectedAmount** | decimal? | Beklenen tutar (komisyon sonrası) |
| **Status** | string? | Durum (pending, completed, failed) |
| **CreatedAt** | DateTime? | Oluşturulma tarihi (default: CURRENT_TIMESTAMP) |
| **UpdatedAt** | DateTime? | Güncellenme tarihi (default: CURRENT_TIMESTAMP) |

**NOT**: Bu entity'de soft delete YOKTUR. Bu mantıklıdır çünkü audit trail (denetim izi) için kalıcı tutulmalıdır.

**İlişkiler**:
- **Sale** (Many-to-One): Bir satışa bağlıdır
- **Bank** (Many-to-One): Bir bankaya bağlıdır

**İş Mantığı**:
- Satış yapılırken POS seçilirse banka işlemi oluşturulur
- Komisyon oranı kaydedilir
- Beklenen tutar = Satış Tutarı × (1 - Komisyon Oranı)
- **Yeni sistem**: `SalePayments` tablosu çoklu ödeme desteği sunar

---

### 4.13 Purchases (Alış İşlemleri)

**Tablo**: `purchases`  
**Amaç**: Tedarikçi veya müşteriden yapılan alış işlemlerinin başlık kaydı.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar (Fiş numarası) |
| **UserId** | int? | İşlemi yapan kullanıcı (Foreign Key → Users) |
| **BranchId** | int? | İşlemin yapıldığı şube (Foreign Key → Branches) |
| **CustomerId** | int? | Alış yapılan müşteri (Foreign Key → Customers) |
| **PaymentMethodId** | int? | Ödeme yöntemi (Foreign Key → PaymentMethods) |
| **CreatedAt** | DateTime? | İşlem tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **User** (Many-to-One): İşlemi yapan kullanıcı
- **Branch** (Many-to-One): İşlemin yapıldığı şube
- **Customer** (Many-to-One): Alış yapılan müşteri
- **PaymentMethod** (Many-to-One): Ödeme yöntemi
- **PurchaseDetails** (One-to-Many): Alış kalemleri

---

### 4.14 PurchaseDetails (Alış Detayları)

**Tablo**: `purchase_details`  
**Amaç**: Alış işleminin kalemlerini (satırlarını) tutar.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **PurchaseId** | int? | Hangi alış fişi (Foreign Key → Purchases) |
| **StockId** | int? | Hangi stok kalemi (Foreign Key → Stocks) |
| **Quantity** | int? | Alınan adet |
| **PurchasePrice** | decimal? | Alış fiyatı (birim) |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Purchase** (Many-to-One): Bir fişe bağlıdır
- **Stock** (Many-to-One): Bir stok kalemine bağlıdır

**İş Mantığı**:
```
Toplam Maliyet = Quantity × PurchasePrice
```

---

### 4.15 Sales (Satış İşlemleri)

**Tablo**: `sales`  
**Amaç**: Müşteriye yapılan satış işlemlerinin başlık kaydı.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar (Fiş numarası) |
| **UserId** | int? | İşlemi yapan kullanıcı (Foreign Key → Users) |
| **BranchId** | int? | İşlemin yapıldığı şube (Foreign Key → Branches) |
| **CustomerId** | int? | Satış yapılan müşteri (Foreign Key → Customers) |
| **PaymentMethodId** | int? | Ödeme yöntemi (Foreign Key → PaymentMethods) |
| **CreatedAt** | DateTime? | İşlem tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **User** (Many-to-One): İşlemi yapan kullanıcı
- **Branch** (Many-to-One): İşlemin yapıldığı şube
- **Customer** (Many-to-One): Satış yapılan müşteri
- **PaymentMethod** (Many-to-One): Ödeme yöntemi
- **SaleDetails** (One-to-Many): Satış kalemleri
- **BankTransactions** (One-to-Many): Banka işlemleri (POS)

---

### 4.16 SaleDetails (Satış Detayları)

**Tablo**: `sale_details`  
**Amaç**: Satış işleminin kalemlerini (satırlarını) tutar.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **SaleId** | int? | Hangi satış fişi (Foreign Key → Sales) |
| **StockId** | int? | Hangi stok kalemi (Foreign Key → Stocks) |
| **Quantity** | int? | Satılan adet |
| **SoldPrice** | decimal? | Satış fiyatı (birim) |
| **UpdatedAt** | DateTime? | Güncellenme tarihi (default: CURRENT_TIMESTAMP) |

**İlişkiler**:
- **Sale** (Many-to-One): Bir fişe bağlıdır
- **Stock** (Many-to-One): Bir stok kalemine bağlıdır

**İş Mantığı**:
```
Toplam Gelir = Quantity × SoldPrice
Kâr = (SoldPrice - PurchasePrice) × Quantity
```

---

### 4.16.1 SalePayments (Satış Ödemeleri) - Çoklu Ödeme Desteği

**Tablo**: `sale_payments`  
**Amaç**: Bir satış fişinin çoklu ödeme yöntemleriyle ödenmesini destekler. Bir satış fişi birden fazla ödeme yöntemiyle ödenebilir (Nakit + EFT + POS).

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **SaleId** | int? | Hangi satış fişine ait (Foreign Key → Sales) |
| **PaymentMethodId** | int? | Ödeme yöntemi (Foreign Key → PaymentMethods) |
| **Amount** | decimal | Bu ödeme yöntemiyle ödenen tutar |
| **BankId** | int? | Banka (POS ise) (Foreign Key → Banks) |
| **CommissionRate** | decimal? | POS komisyon oranı (örn: 0.025 = %2.5) |
| **NetAmount** | decimal? | Komisyon düşüldükten sonra net tutar |
| **CreatedAt** | DateTime? | Ödeme tarihi (default: CURRENT_TIMESTAMP) |
| **UpdatedAt** | DateTime? | Güncelleme tarihi (default: CURRENT_TIMESTAMP) |

**İlişkiler**:
- **Sale** (Many-to-One): Bağlı olduğu satış fişi
- **PaymentMethod** (Many-to-One): Ödeme yöntemi
- **Bank** (Many-to-One): Banka (POS ödemesi ise)

**İş Mantığı**:
- Bir satış fişi için birden fazla `SalePayments` kaydı olabilir
- Örnek: 1000 TL'lik satış → 600 TL Nakit + 400 TL POS (komisyon %2.5 = 10 TL, net 390 TL)
- `NetAmount = Amount × (1 - CommissionRate)` (POS ise)
- `UnifiedReceiptCreateDto` ile çoklu ödeme desteği (`Cash`, `Eft`, `Pos` alanları)

---

### 4.17 LifecycleActions (Yaşam Döngüsü Aksiyonları)

**Tablo**: `lifecycle_actions`  
**Amaç**: Ürünlerin geçebileceği durum değişikliklerini tanımlar.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **Name** | string | Aksiyon adı (Giriş, Çıkış, Transfer, Sayım) |
| **Description** | string? | Açıklama |

**İlişkiler**:
- **ProductLifecycles** (One-to-Many): Bu aksiyona ait kayıtlar

**Örnek Aksiyonlar**:
- **Purchase (Giriş)**: Alış yapıldı
- **Sale (Çıkış)**: Satış yapıldı
- **Transfer**: Şubeler arası transfer
- **Count**: Sayım yapıldı
- **Adjustment**: Düzeltme
- **Damage**: Hasarlı
- **Lost**: Kayıp

---

### 4.18 ProductLifecycles (Ürün Yaşam Döngüsü)

**Tablo**: `product_lifecycles`  
**Amaç**: Her stok kaleminin tüm hareketlerini (tarihçe) kaydeder.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **StockId** | int? | Hangi stok (Foreign Key → Stocks) |
| **UserId** | int? | İşlemi yapan (Foreign Key → Users) |
| **ActionId** | int? | Hangi aksiyon (Foreign Key → LifecycleActions) |
| **Notes** | string? | Notlar (Sale, Purchase, vb.) |
| **Timestamp** | DateTime? | İşlem zamanı |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Stock** (Many-to-One): Bir stok kalemine bağlıdır
- **User** (Many-to-One): İşlemi yapan kullanıcı
- **Action** (Many-to-One): İşlem tipi

**İş Mantığı**:
- Her alış/satış/transfer işleminde otomatik kayıt oluşturulur
- Stok hareketlerinin tam geçmişi tutulur
- Denetim (audit trail) için kullanılır

---

### 4.19 Limits (Stok Limitleri)

**Tablo**: `limits`  
**Amaç**: Şube bazlı varyant için minimum ve maksimum stok eşiklerini tanımlar.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **BranchId** | int? | Hangi şube (Foreign Key → Branches) |
| **ProductVariantId** | int? | Hangi varyant (Foreign Key → ProductVariants) |
| **MinThreshold** | decimal? | Minimum eşik (altına düşerse uyarı) |
| **MaxThreshold** | decimal? | Maksimum eşik (üstüne çıkarse uyarı) |
| **CreatedAt** | DateTime? | Oluşturulma tarihi (default: CURRENT_TIMESTAMP) |
| **UpdatedAt** | DateTime? | Güncellenme tarihi (default: CURRENT_TIMESTAMP) |

**NOT**: Bu entity'de soft delete YOKTUR. Bu mantıklıdır çünkü limitler dinamik olarak eklenip silinebilir ve audit gerektirmez.

**İlişkiler**:
- **Branch** (Many-to-One): Bir şubeye bağlıdır
- **ProductVariant** (Many-to-One): Bir varyanta bağlıdır

**İş Mantığı**:
- Şube yöneticisi varyant bazında limit belirler
- Stok minimum eşiğin altına düşerse sistem uyarı verebilir
- Maksimum eşik aşılırsa aşırı stok uyarısı

---

### 4.20 ThermalPrinters (Termal Yazıcılar)

**Tablo**: `thermal_printers`  
**Amaç**: Şubelerdeki termal yazıcıların yapılandırmasını saklar. Her şube için bir yazıcı yapılandırması tanımlanabilir.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **BranchId** | int? | Hangi şubeye ait (Foreign Key → Branches) |
| **Name** | string | Yazıcı adı (max 128 karakter) |
| **IpAddress** | string | Yazıcı IP adresi (max 64 karakter) |
| **Port** | int | Yazıcı port numarası |
| **Description** | string? | Açıklama |
| **IsActive** | bool | Yazıcı aktif mi? (default: true) |
| **IsDeleted** | bool | Soft delete (default: false) |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **CreatedAt** | DateTime? | Oluşturulma tarihi (default: CURRENT_TIMESTAMP) |
| **UpdatedAt** | DateTime? | Güncellenme tarihi (default: CURRENT_TIMESTAMP) |

**Unique Constraint**:
```sql
UNIQUE (BranchId) WHERE branch_id IS NOT NULL
```
Bir şube için yalnızca bir yazıcı yapılandırması olabilir.

**İlişkiler**:
- **Branch** (Many-to-One): Bir yazıcı bir şubeye aittir

**İş Mantığı**:
- Branch başına tek yazıcı yapılandırması (unique constraint)
- IP/Port doğrulaması servis katmanında yapılır
- Soft delete desteği mevcuttur

---

### 4.21 MonthlyTargets (Aylık Satış Hedefleri)

**Tablo**: `monthly_targets`  
**Amaç**: Mağaza bazlı aylık satış hedeflerini yönetir. Her mağaza için her ay/yıl kombinasyonu için bir hedef tutarı tanımlanabilir.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **StoreId** | int? | Hangi mağaza (Foreign Key → Stores) |
| **Year** | int | Hedef yılı (örn: 2025) |
| **Month** | int | Hedef ayı (1-12) |
| **TargetAmount** | decimal | Hedef tutar (TL) |
| **CreatedAt** | DateTime? | Oluşturulma tarihi (default: CURRENT_TIMESTAMP) |
| **UpdatedAt** | DateTime? | Güncellenme tarihi (default: CURRENT_TIMESTAMP) |
| **IsDeleted** | bool | Soft delete (default: false) |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |

---

### 4.22 RefreshTokens (Yenileme Token'ları)

**Tablo**: `refresh_tokens`  
**Amaç**: JWT refresh token'larını saklar. Kullanıcıların oturumlarını yenilemek için kullanılır.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **UserId** | int | Kullanıcı ID (Foreign Key → Users) |
| **Token** | string | Refresh token değeri (Unique) |
| **ExpiresAt** | DateTime | Token sona erme tarihi |
| **IsRevoked** | bool | Token iptal edildi mi? (default: false) |
| **RevokedAt** | DateTime? | İptal edilme tarihi |
| **CreatedAt** | DateTime | Oluşturulma tarihi (default: CURRENT_TIMESTAMP) |

**Unique Constraint**:
- `token` kolonu unique (her token benzersiz)

**İlişkiler**:
- **User** (Many-to-One): Bir kullanıcının birden fazla refresh token'ı olabilir

**İş Mantığı**:
- Refresh token'lar kullanıcı oturumlarını yenilemek için kullanılır
- Token iptal edildiğinde `IsRevoked = true` ve `RevokedAt` set edilir
- Expire olan token'lar otomatik olarak geçersiz sayılır

---

### 4.23 InvalidatedTokens (Geçersiz Kılınmış Token'lar)

**Tablo**: `invalidated_tokens`  
**Amaç**: Logout yapılan veya geçersiz kılınan JWT token'larını (JTI - JWT ID) saklar. Token blacklist mekanizması için kullanılır.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **Jti** | string | JWT ID (JWT'nin benzersiz kimliği, Unique) |
| **ExpiresAt** | DateTime | Token sona erme tarihi |
| **InvalidatedAt** | DateTime | Geçersiz kılınma tarihi (default: CURRENT_TIMESTAMP) |

**Unique Constraint**:
- `jti` kolonu unique (her JTI benzersiz)

**NOT**: Bu entity'de soft delete YOKTUR. Bu mantıklıdır çünkü geçersiz kılınan token'lar kalıcı olarak saklanmalıdır (güvenlik için).

**İş Mantığı**:
- Kullanıcı logout yaptığında JWT'nin JTI değeri bu tabloya eklenir
- JWT doğrulama sırasında JTI bu tabloda kontrol edilir
- Token expire olduğunda otomatik olarak geçersiz sayılır
- Token blacklist servisi (`ITokenBlacklistService`) bu tabloyu kullanır
| **IsActive** | bool | Aktif mi? (default: true) |

**Unique Constraint**:
```sql
UNIQUE (StoreId, Year, Month) WHERE StoreId IS NOT NULL AND IsDeleted = false
```
Aynı mağaza için aynı ay/yıl kombinasyonu tekrar edemez.

**İlişkiler**:
- **Store** (Many-to-One): Bağlı olduğu mağaza

**İş Mantığı**:
- `DashboardService.GetMonthlyTargetAsync` metodu mevcut ay için hedef tutarı veritabanından okur
- Eğer kayıt yoksa default 100.000 TL kullanılır (geriye dönük uyumluluk)
- Seed data ile mevcut mağazalar için mevcut ay için default hedef eklenir
- Dashboard'da hedef tutar, mevcut satış, ilerleme yüzdesi ve kalan tutar gösterilir

---

## 5. SERVİSLER VE İŞ MANTIĞI

### 5.1 Genel Servis Yapısı

Tüm servisler **Application** katmanında interface olarak tanımlanır ve **Infrastructure** katmanında implemente edilir.

**Temel Pattern**:
```csharp
// Application Layer
public interface IStocksService
{
    Task<ApiResult<StockDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<ApiResult<StockDto>> CreateAsync(StockCreateDto dto, CancellationToken ct);
    // ... diğer metodlar
}

// Infrastructure Layer
public sealed class StocksService : IStocksService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _user;
    
    public StocksService(AppDbContext db, ICurrentUserContext user)
    {
        _db = db;
        _user = user;
    }
    
    // Implementasyon...
}
```

### 5.2 Stok Servisi (StocksService)

**Dosya**: `KuyumStokApi.Infrastructure/Services/StocksService/StocksService.cs`

**Sorumluluklar**:
1. **Stok Listeleme**: Sayfalama, filtreleme, arama desteği
2. **Stok Detay**: ID veya barcode ile tek stok getirme
3. **Varyant Detayı**: Bir varyantın tüm şubelerdeki durumu
4. **CRUD İşlemleri**: Create, Update, Delete, Hard Delete

**Önemli Metodlar**:

#### `GetPagedAsync(StockFilter filter)`
```csharp
// Şube bazlı stok listesi
// Filtreleme: Query (barkod/ad arama), ProductType, Variant, Gram aralığı, tarih
// Join: Stocks → ProductVariants → ProductTypes → ProductCategories
// Sayfalama: Page, PageSize
// Sıralama: UpdatedAt DESC
```

#### `GetVariantDetailInStoreAsync(int variantId)`
```csharp
// Kullanıcının mağazasındaki TÜM şubelerde bu varyantın durumu
// Şube bazında gruplama:
//   - ToplamAdet
//   - ToplamAgirlik
//   - Her stok kalemi (Barcode, Gram, Color)
```

#### `CreateAsync(StockCreateDto dto)`
```csharp
// Yeni stok oluştur
// - Barcode benzersizlik kontrolü
// - BranchId kullanıcıdan alınır (CurrentUser)
// - Timestamp: CreatedAt, UpdatedAt
```

#### `DeleteAsync(int id)`
```csharp
// Soft delete (IsDeleted = true)
// Kontrol: Stok satış/alış/lifecycle'da kullanılıyorsa 409 Conflict
```

#### `HardDeleteAsync(int id)`
```csharp
// Fiziksel silme (veritabanından tamamen kaldır)
// Sadece hiç kullanılmamış stoklar silinebilir
```

**Güvenlik**:
- Kullanıcı sadece kendi şubesini görür (filter.BranchId ?? _user.BranchId)
- Mağaza seviyesinde görünüm için StoreId kontrolü

---

### 5.3 Satış Servisi (SalesService)

**Dosya**: `KuyumStokApi.Infrastructure/Services/SalesService/SalesService.cs`

**Sorumluluklar**:
1. Satış kaydı oluşturma
2. Stok düşürme
3. Müşteri inline upsert (yoksa oluştur)
4. Banka işlemi (opsiyonel POS komisyonu)
5. Lifecycle kaydı

**Önemli Metodlar**:

#### `CreateAsync(SaleCreateDto dto)`
```csharp
// TRANSACTION ile:
// 1. Müşteri kontrolü/oluşturma
//    - CustomerId varsa direkt kullan
//    - Yoksa Name+Phone ile ara, yoksa oluştur
// 2. Sales kaydı oluştur
// 3. Her kalem için:
//    - Stok kontrolü (yeterli var mı?)
//    - Quantity düşür
//    - SaleDetails ekle
//    - ProductLifecycles ekle (Notes: "Sale")
// 4. Opsiyonel: BankTransactions ekle (POS varsa)
// 5. Commit
```

**İş Kuralları**:
- Yetersiz stok → 409 Conflict
- Aynı stoğu iki işlem aynı anda düşmesin → Lock mekanizması (isteğe bağlı)
- Müşteri bilgisi inline oluşturulabilir

#### `GetPagedAsync(SaleFilter filter)`
```csharp
// Satış kalemleri listesi (her satır bir SaleDetail)
// Join: SaleDetails → Sales → Stocks → ProductVariants → Branches → Users
// Filtreleme: BranchId, UserId, CustomerId, PaymentMethodId, Tarih aralığı
// Sayfalama destekli
```

#### `GetLineByIdAsync(int lineId)`
```csharp
// Tek bir satış kaleminin detayı
// Fiyat, ödeme yöntemi, ürün özellikleri
```

---

### 5.4 Alış Servisi (PurchasesService)

**Dosya**: `KuyumStokApi.Infrastructure/Services/PurchasesService/PurchasesService.cs`

**Sorumluluklar**:
1. Alış kaydı oluşturma
2. Stok oluşturma/artırma
3. Barcode kontrolü (varsa birleştir, yoksa yeni)
4. Lifecycle kaydı

**Önemli Metodlar**:

#### `CreateAsync(PurchaseCreateDto dto)`
```csharp
// TRANSACTION ile:
// 1. Purchases kaydı oluştur
// 2. Her kalem için:
//    - Barcode ile stok ara
//    - Varsa: Quantity artır (aynı branch/variant kontrolü)
//    - Yoksa: Yeni Stocks kaydı oluştur
//    - PurchaseDetails ekle
//    - ProductLifecycles ekle (Notes: "Purchase")
// 3. Commit
```

**İş Kuralları**:
- Barcode UNIQUE constraint
- Barcode çakışması varsa branch/variant uyumlu olmalı
- Stok yoksa otomatik oluşturulur

#### `GetPagedAsync(PurchaseFilter filter)`
```csharp
// Alış listesi (başlık bazlı)
// Join: Purchases → Branches → Users → Customers → PaymentMethods
// Her fiş için:
//   - ItemCount (kaç kalem)
//   - TotalAmount (toplam maliyet)
```

#### `GetByIdAsync(int id)`
```csharp
// Alış fişi detayı
// Başlık bilgileri + tüm kalemler (Lines)
// Her kalem: Barcode, Quantity, PurchasePrice, Variant bilgisi
```

---

### 5.5 Kullanıcı Servisi (UserService)

**Sorumluluklar**:
1. Kullanıcı kaydı (Register)
2. Giriş (Login) ve JWT token üretimi
3. Parola doğrulama
4. Parola politika kontrolü

**Önemli Metodlar**:

#### `RegisterAsync(RegisterDto dto)`
```csharp
// 1. Username benzersizlik kontrolü
// 2. Parola politika kontrolü (uzunluk, karmaşıklık)
// 3. Salt üret (16 byte random)
// 4. Hash hesapla (SHA-256 + Salt + Pepper + Iterations)
// 5. Users kaydı oluştur
// 6. Return user entity
```

#### `LoginAsync(LoginDto dto)`
```csharp
// 1. Username ile kullanıcı bul
// 2. Parola hash'ini doğrula
// 3. IsActive kontrolü
// 4. JWT token üret
// 5. Return token
```

#### `ValidatePasswordAsync(PasswordCheckRequestDto dto)`
```csharp
// Granüler parola doğrulama:
// - Minimum uzunluk
// - Büyük harf var mı?
// - Küçük harf var mı?
// - Rakam var mı?
// - Özel karakter var mı?
// Her kriter için ayrı hata mesajı
```

---

### 5.6 JWT Servisi (JwtService)

**Sorumluluklar**:
1. JWT token oluşturma
2. Claims ekleme (UserId, Username, Role, BranchId)

**Token İçeriği**:
```json
{
  "sub": "123",                    // UserId
  "unique_name": "admin",          // Username
  "role": "Admin",                 // Role.Name
  "branch": "5",                   // BranchId
  "iss": "KuyumStokApi",          // Issuer
  "aud": "KuyumStokApiClients",   // Audience
  "exp": 1699564800               // Expiration (Unix timestamp)
}
```

**Metodlar**:
```csharp
public string GenerateToken(Users user)
{
    // 1. Claims oluştur
    // 2. SymmetricSecurityKey ile sign et
    // 3. Token string olarak return
}
```

---

### 5.7 Tüm Servis Interface'leri ve Metod İmzaları

#### 5.7.1 IBanksService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(BankFilter, CancellationToken)` | `Task<ApiResult<PagedResult<BankDto>>>` | Bankaları filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<BankDto>>` | ID ile banka detayı |
| `CreateAsync(BankCreateDto, CancellationToken)` | `Task<ApiResult<BankDto>>` | Yeni banka oluşturur |
| `UpdateAsync(int, BankUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Bankayı günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Soft delete yapar |
| `HardDeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Hard delete yapar |
| `SetActiveAsync(int, bool, CancellationToken)` | `Task<ApiResult<bool>>` | Aktif/pasif yapar |

#### 5.7.2 IBranchesService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(BranchFilter, CancellationToken)` | `Task<ApiResult<PagedResult<BranchDto>>>` | Şubeleri filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<BranchDto>>` | ID ile şube detayı |
| `CreateAsync(BranchCreateDto, CancellationToken)` | `Task<ApiResult<BranchDto>>` | Yeni şube oluşturur |
| `UpdateAsync(int, BranchUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Şubeyi günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Soft delete yapar |
| `HardDeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Hard delete yapar |
| `SetActiveAsync(int, bool, CancellationToken)` | `Task<ApiResult<bool>>` | Aktif/pasif yapar |

#### 5.7.3 IStoresService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(StoreFilter, CancellationToken)` | `Task<ApiResult<PagedResult<StoreDto>>>` | Mağazaları filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<StoreDto>>` | ID ile mağaza detayı |
| `CreateAsync(StoreCreateDto, CancellationToken)` | `Task<ApiResult<StoreDto>>` | Yeni mağaza oluşturur |
| `UpdateAsync(int, StoreUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Mağazayı günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Soft delete yapar |
| `HardDeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Hard delete yapar |
| `SetActiveAsync(int, bool, CancellationToken)` | `Task<ApiResult<bool>>` | Aktif/pasif yapar |

#### 5.7.4 ICustomersService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(CustomerFilter, CancellationToken)` | `Task<ApiResult<PagedResult<CustomerDto>>>` | Müşterileri filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<CustomerDto>>` | ID ile müşteri detayı |
| `CreateAsync(CustomerCreateDto, CancellationToken)` | `Task<ApiResult<CustomerDto>>` | Yeni müşteri oluşturur |
| `UpdateAsync(int, CustomerUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Müşteriyi günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Silme (ilişkisi varsa soft-delete) |

#### 5.7.5 IProductCategoryService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(ProductCategoryFilter, CancellationToken)` | `Task<ApiResult<PagedResult<ProductCategoryDto>>>` | Kategorileri filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<ProductCategoryDto>>` | ID ile kategori detayı |
| `CreateAsync(ProductCategoryCreateDto, CancellationToken)` | `Task<ApiResult<ProductCategoryDto>>` | Yeni kategori oluşturur (duplicate guard) |
| `UpdateAsync(int, ProductCategoryUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Kategoriyi günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Soft delete yapar |
| `HardDeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Hard delete yapar |
| `SetActiveAsync(int, bool, CancellationToken)` | `Task<ApiResult<bool>>` | Aktif/pasif yapar |

#### 5.7.6 IProductTypeService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(ProductTypeFilter, CancellationToken)` | `Task<ApiResult<PagedResult<ProductTypeDto>>>` | Ürün türlerini filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<ProductTypeDto>>` | ID ile ürün türü detayı |
| `CreateAsync(ProductTypeCreateDto, CancellationToken)` | `Task<ApiResult<ProductTypeDto>>` | Yeni ürün türü oluşturur (duplicate guard) |
| `UpdateAsync(int, ProductTypeUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Ürün türünü günceller (duplicate guard) |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Soft delete yapar (variant kontrolü) |
| `HardDeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Hard delete yapar |
| `SetActiveAsync(int, bool, CancellationToken)` | `Task<ApiResult<bool>>` | Aktif/pasif yapar |

#### 5.7.7 IProductVariantService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(ProductVariantFilter, CancellationToken)` | `Task<ApiResult<PagedResult<ProductVariantDto>>>` | Varyantları filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<ProductVariantDto>>` | ID ile varyant detayı |
| `CreateAsync(ProductVariantCreateDto, CancellationToken)` | `Task<ApiResult<ProductVariantDto>>` | Yeni varyant oluşturur (unique composite guard) |
| `UpdateAsync(int, ProductVariantUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Varyantı günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Soft delete yapar (stocks/limits kontrolü) |
| `HardDeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Hard delete yapar |
| `SetActiveAsync(int, bool, CancellationToken)` | `Task<ApiResult<bool>>` | Aktif/pasif yapar |

#### 5.7.8 IStocksService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(StockFilter, CancellationToken)` | `Task<ApiResult<PagedResult<StockDto>>>` | Aynı `ProductVariantId + BranchId`'ye sahip stokları gruplayarak toplam adet (`TotalQuantity`) ve toplam ağırlık (`TotalWeight`) ile tek kayıt olarak döndürür. Gruplama yapıldığı için `StockDto.Id = 0`, `Barcode = string.Empty`, `QrCode = null`, `Gram = null` olarak döner. Branch filtresi (filter veya current user). |
| `GetVariantDetailInStoreAsync(int, CancellationToken)` | `Task<ApiResult<StockVariantDetailByStoreDto>>` | Varyantın tüm şubelerdeki durumunu getirir |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<StockDto>>` | ID ile stok detayı |
| `GetByBarcodeAsync(string, CancellationToken)` | `Task<ApiResult<StockDto>>` | Barcode ile stok |
| `CreateAsync(StockCreateDto, CancellationToken)` | `Task<ApiResult<StockDto>>` | Yeni stok oluşturur (merge logic, otomatik barcode) |
| `UpdateAsync(int, StockUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Stoku günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Soft delete yapar (referenced records kontrolü) |
| `HardDeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Hard delete yapar |
| `GetFavoritesAsync(int, int, bool, CancellationToken)` | `Task<ApiResult<List<FavoriteProductDto>>>` | En çok satılan ürünleri getirir |

#### 5.7.9 ISalesService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `CreateUnifiedAsync(UnifiedReceiptCreateDto, CancellationToken)` | `Task<ApiResult<UnifiedReceiptResultDto>>` | Birleşik fiş oluşturur (satış + opsiyonel alış, çoklu ödeme) |
| `GetPagedAsync(SaleFilter, CancellationToken)` | `Task<ApiResult<PagedResult<SaleListDto>>>` | Satışları filtreleyerek sayfalı listeler (satır bazlı) |
| `GetLineByIdAsync(int, CancellationToken)` | `Task<ApiResult<SaleLineDetailDto>>` | Satış satırı detayını getirir |

#### 5.7.10 IPurchasesService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `CreateAsync(PurchaseCreateDto, CancellationToken)` | `Task<ApiResult<PurchaseResultDto>>` | Alış fişi oluşturur ve stoğu artırır (transaction) |
| `GetPagedAsync(PurchaseFilter, CancellationToken)` | `Task<ApiResult<PagedResult<PurchaseListDto>>>` | Alışları filtreleyerek sayfalı listeler (başlık bazlı) |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<PurchaseDetailDto>>` | Alış fişi detayını getirir (başlık + satırlar) |

#### 5.7.11 IRolesService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetAllAsync(CancellationToken)` | `Task<ApiResult<List<RoleDto>>>` | Tüm rolleri listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<RoleDto>>` | ID ile rol detayı |
| `CreateAsync(RoleCreateDto, CancellationToken)` | `Task<ApiResult<RoleDto>>` | Yeni rol oluşturur (duplicate guard) |
| `UpdateAsync(int, RoleUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Rolü günceller (duplicate guard) |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Rolü siler |

#### 5.7.12 ILimitsService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(LimitFilter, CancellationToken)` | `Task<ApiResult<PagedResult<LimitDto>>>` | Limitleri filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<LimitDto>>` | ID ile limit detayı |
| `CreateAsync(LimitCreateDto, CancellationToken)` | `Task<ApiResult<LimitDto>>` | Yeni limit kaydı oluşturur |
| `UpdateAsync(int, LimitUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Limit kaydını günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Limit kaydını siler |

#### 5.7.13 ILifecycleActionsService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(LifecycleActionFilter, CancellationToken)` | `Task<ApiResult<PagedResult<LifecycleActionDto>>>` | Yaşam döngüsü aksiyonlarını filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<LifecycleActionDto>>` | ID ile aksiyon detayı |
| `CreateAsync(LifecycleActionCreateDto, CancellationToken)` | `Task<ApiResult<LifecycleActionDto>>` | Yeni aksiyon oluşturur |
| `UpdateAsync(int, LifecycleActionUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Aksiyonu günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Aksiyonu siler |

#### 5.7.14 IProductLifecyclesService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(ProductLifecycleFilter, CancellationToken)` | `Task<ApiResult<PagedResult<ProductLifecycleDto>>>` | Ürün yaşam döngüsü kayıtlarını filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<ProductLifecycleDto>>` | ID ile yaşam döngüsü kaydı detayı |
| `CreateAsync(ProductLifecycleCreateDto, CancellationToken)` | `Task<ApiResult<ProductLifecycleDto>>` | Yeni yaşam döngüsü kaydı oluşturur (stock & action required, uses ICurrentUserService) |

#### 5.7.15 IPaymentMethodsService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(PaymentMethodFilter, CancellationToken)` | `Task<ApiResult<PagedResult<PaymentMethodDto>>>` | Ödeme yöntemlerini filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<PaymentMethodDto>>` | ID ile ödeme yöntemi detayı |
| `CreateAsync(PaymentMethodCreateDto, CancellationToken)` | `Task<ApiResult<PaymentMethodDto>>` | Yeni ödeme yöntemi oluşturur |
| `UpdateAsync(int, PaymentMethodUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Ödeme yöntemini günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Silme (kullanımda ise soft-delete) |

#### 5.7.16 IThermalPrintersService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetPagedAsync(ThermalPrinterFilter, CancellationToken)` | `Task<ApiResult<PagedResult<ThermalPrinterDto>>>` | Termal yazıcıları filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<ThermalPrinterDto>>` | ID ile termal yazıcı detayı |
| `CreateAsync(ThermalPrinterCreateDto, CancellationToken)` | `Task<ApiResult<ThermalPrinterDto>>` | Yeni termal yazıcı oluşturur (branch başına tek yazıcı kuralı) |
| `UpdateAsync(int, ThermalPrinterUpdateDto, CancellationToken)` | `Task<ApiResult<bool>>` | Termal yazıcı bilgilerini günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Soft delete yapar |
| `HardDeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Hard delete yapar |

#### 5.7.17 IUserService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `LoginAsync(LoginDto)` | `Task<AuthResponseDto?>` | Kullanıcı girişi (JWT döner) |
| `RegisterAsync(RegisterDto)` | `Task<Users>` | Yeni kullanıcı kaydı (username normalization, password hash) |
| `UserExistsAsync(string)` | `Task<bool>` | Username kontrolü |
| `ValidatePasswordAsync(PasswordCheckRequestDto, CancellationToken)` | `Task<ApiResult<PasswordCheckResultDto>>` | Parola gücü granüler doğrulama |
| `ValidateRegisterAsync(RegisterDto, CancellationToken)` | `Task<ApiResult<RegisterValidationResultDto>>` | Register öncesi tüm alanların doğrulaması |
| `GetPagedAsync(UserFilter, CancellationToken)` | `Task<ApiResult<PagedResult<UserDto>>>` | Kullanıcıları filtreleyerek sayfalı listeler |
| `GetByIdAsync(int, CancellationToken)` | `Task<ApiResult<UserDto>>` | ID ile kullanıcı detayı |
| `UpdateAsync(int, UserUpdateDto, CancellationToken)` | `Task<ApiResult<UserDto>>` | Kullanıcıyı günceller |
| `DeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Soft delete yapar |
| `HardDeleteAsync(int, CancellationToken)` | `Task<ApiResult<bool>>` | Hard delete yapar |

#### 5.7.18 IReportsService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GetStoreOverviewAsync(ReportDateRange, CancellationToken)` | `Task<ApiResult<StoreOverviewReportDto>>` | Mağaza genel bakış raporu (rol bazlı erişim) |
| `GetBranchOverviewAsync(int?, ReportDateRange, CancellationToken)` | `Task<ApiResult<BranchOverviewReportDto>>` | Şube bazlı performans raporu |
| `GetUserPerformanceAsync(int?, ReportDateRange, CancellationToken)` | `Task<ApiResult<UserPerformanceReportDto>>` | Kullanıcı bazlı satış performans raporu |
| `GetSalesTrendAsync(ReportTrendGranularity, ReportDateRange, CancellationToken)` | `Task<ApiResult<SalesTrendReportDto>>` | Satış trendi (Daily/Weekly/Monthly) |

#### 5.7.19 IJwtService

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GenerateToken(Users)` | `AuthResponseDto` | JWT token oluşturur (HS256, nested JwtOptions kullanır) |

#### 5.7.20 IPasswordHasher

| Metod | Dönüş Tipi | Açıklama |
|-------|-----------|----------|
| `GenerateSalt(int)` | `string` | Base64 formatında salt üretir (default size: 16) |
| `Hash(string, string)` | `string` | SHA-256 + Salt + Pepper + Iterations ile hash üretir |
| `Verify(string, string, string)` | `bool` | Parola doğrulama (constant-time comparison) |

---

---

## 6. CONTROLLER'LAR VE API ENDPOINT'LERİ

### 6.1 RESTful API Yapısı

Tüm controller'lar:
- `[ApiController]` attribute'u ile işaretlidir
- `[Route("api/[controller]")]` ile route tanımı
- `[Authorize]` ile JWT authentication zorunluluğu (AuthController hariç)

### 6.2 StocksController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | `/api/stocks` | Stok listesi (sayfalı, filtreli) |
| GET | `/api/stocks/{id}` | ID ile stok detayı |
| GET | `/api/stocks/by-barcode/{barcode}` | Barcode ile stok |
| GET | `/api/stocks/variant/{variantId}/detail` | Varyant detayı (şubeler bazında) |
| POST | `/api/stocks` | Yeni stok oluştur |
| PUT | `/api/stocks/{id}` | Stok güncelle |
| DELETE | `/api/stocks/{id}` | Stok sil (soft) |
| DELETE | `/api/stocks/{id}/hard` | Stok sil (hard) |

**Örnek İstek**:
```http
GET /api/stocks?Page=1&PageSize=20&Query=14%20ayar&BranchId=1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

### 6.3 SalesController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | `/api/sales` | Satış listesi (sayfalı) |
| GET | `/api/sales/{lineId}` | Satış kalemi detayı |
| POST | `/api/sales` | Yeni satış oluştur |

**Örnek POST Body**:
```json
{
  "branchId": 1,
  "userId": 5,
  "customerId": null,
  "customerName": "Ahmet Yılmaz",
  "customerPhone": "05551234567",
  "customerNationalId": "12345678901",
  "paymentMethodId": 2,
  "bankId": 1,
  "commissionRate": 0.022,
  "expectedAmount": 24500,
  "items": [
    {
      "stockId": 3,
      "quantity": 1,
      "soldPrice": 12000
    },
    {
      "stockId": 6,
      "quantity": 1,
      "soldPrice": 12500
    }
  ]
}
```

---

### 6.4 PurchaseController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/purchase` | Alış listesi (sayfalı) | Evet |
| GET | `/api/purchase/{id}` | Alış fişi detayı | Evet |
| POST | `/api/purchase` | Yeni alış oluştur | Evet |

---

### 6.5 AuthController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| POST | `/api/auth/register` | Yeni kullanıcı kaydet (başarılıysa JWT döner) | Hayır |
| POST | `/api/auth/login` | Kullanıcı girişi (JWT döner) | Hayır |
| POST | `/api/auth/validate-password` | Parola gücü ve kuralları için granüler doğrulama | Hayır |
| POST | `/api/auth/validate-register` | Register öncesi tüm alanların granüler doğrulaması | Hayır |

**Login Örneği**:
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin123!"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-10T12:00:00Z"
}
```

---

### 6.6 BanksController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/banks` | Bankaları filtreleyerek sayfalı listeler | Evet |
| GET | `/api/banks/{id}` | Id'ye göre banka detayını getirir | Evet |
| POST | `/api/banks` | Yeni bir banka kaydı oluşturur | Evet |
| PUT | `/api/banks/{id}` | Var olan banka kaydını günceller | Evet |
| DELETE | `/api/banks/{id}` | Belirtilen id'deki banka kaydını siler (soft) | Evet |
| PUT | `/api/banks/{id}/active` | Bankayı aktif/pasif yapar | Evet |
| DELETE | `/api/banks/{id}/hard` | Bankayı kalıcı olarak siler (hard delete) | Evet |

---

### 6.7 BranchesController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/branches` | Şubeleri filtreleyerek sayfalı listeler (mağaza bilgisiyle) | Evet |
| GET | `/api/branches/{id}` | Id'ye göre şube detayını getirir (mağaza bilgisiyle) | Evet |
| POST | `/api/branches` | Yeni bir şube oluşturur | Evet |
| PUT | `/api/branches/{id}` | Şubeyi günceller | Evet |
| DELETE | `/api/branches/{id}` | Şubeyi soft delete yapar | Evet |
| DELETE | `/api/branches/{id}/hard` | Şubeyi kalıcı olarak siler (hard delete) | Evet |
| PUT | `/api/branches/{id}/active` | Şubeyi aktif/pasif yapar | Evet |

---

### 6.8 StoresController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/stores` | Mağazaları filtreleyerek sayfalı listeler (şube sayısı özet bilgiyle) | Evet |
| GET | `/api/stores/{id}` | Id'ye göre mağaza detayını getirir | Evet |
| POST | `/api/stores` | Yeni bir mağaza oluşturur | Evet |
| PUT | `/api/stores/{id}` | Mağazayı günceller | Evet |
| DELETE | `/api/stores/{id}` | Mağazayı soft delete yapar | Evet |
| DELETE | `/api/stores/{id}/hard` | Mağazayı kalıcı olarak siler (hard delete) | Evet |
| PUT | `/api/stores/{id}/active` | Mağazayı aktif/pasif yapar | Evet |

---

### 6.9 CustomersController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/customers` | Müşterileri filtreleyerek sayfalı listeler | Evet |
| GET | `/api/customers/{id}` | Id'ye göre müşteri detayını getirir | Evet |
| POST | `/api/customers` | Yeni müşteri oluşturur | Evet |
| PUT | `/api/customers/{id}` | Mevcut müşteriyi günceller | Evet |
| DELETE | `/api/customers/{id}` | Müşteriyi siler (ilişkisi varsa soft-delete) | Evet |

---

### 6.10 ProductCategoriesController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/productcategories` | Kategorileri filtreleyerek sayfalı listeler | Evet |
| GET | `/api/productcategories/{id}` | Id'ye göre kategori detayını getirir | Evet |
| POST | `/api/productcategories` | Yeni bir kategori oluşturur | Evet |
| PUT | `/api/productcategories/{id}` | Kategoriyi günceller | Evet |
| DELETE | `/api/productcategories/{id}` | Kategoriyi soft delete yapar | Evet |
| DELETE | `/api/productcategories/{id}/hard` | Kategoriyi kalıcı olarak siler (hard delete) | Evet |
| PUT | `/api/productcategories/{id}/active` | Kategoriyi aktif/pasif yapar | Evet |

---

### 6.11 ProductTypeController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/producttype` | Ürün türlerini filtreleyerek sayfalı listeler (kategori bilgisiyle) | Evet |
| GET | `/api/producttype/{id}` | Id'ye göre ürün türü detayını getirir (kategori bilgisiyle) | Evet |
| POST | `/api/producttype` | Yeni bir ürün türü oluşturur | Evet |
| PUT | `/api/producttype/{id}` | Ürün türünü günceller | Evet |
| DELETE | `/api/producttype/{id}` | Ürün türünü soft delete yapar | Evet |
| DELETE | `/api/producttype/{id}/hard` | Ürün türünü kalıcı olarak siler (hard delete) | Evet |
| PUT | `/api/producttype/{id}/active` | Ürün türünü aktif/pasif yapar | Evet |

---

### 6.12 ProductVariantController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/productvariant` | Varyantları filtreleyerek sayfalı listeler (bağlı ürün türü bilgisiyle) | Evet |
| GET | `/api/productvariant/{id}` | Id'ye göre varyant detayını getirir (bağlı ürün türü bilgisiyle) | Evet |
| POST | `/api/productvariant` | Yeni bir varyant oluşturur | Evet |
| PUT | `/api/productvariant/{id}` | Varyantı günceller | Evet |
| DELETE | `/api/productvariant/{id}` | Varyantı soft delete yapar | Evet |
| DELETE | `/api/productvariant/{id}/hard` | Varyantı kalıcı olarak siler (hard delete) | Evet |
| PUT | `/api/productvariant/{id}/active` | Varyantı aktif/pasif yapar | Evet |

---

### 6.13 PaymentMethodsController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/paymentmethods` | Ödeme yöntemlerini filtreleyerek sayfalı listeler | Evet |
| GET | `/api/paymentmethods/{id}` | Id'ye göre ödeme yöntemi detayını getirir | Evet |
| POST | `/api/paymentmethods` | Yeni ödeme yöntemi oluşturur | Evet |
| PUT | `/api/paymentmethods/{id}` | Ödeme yöntemini günceller | Evet |
| DELETE | `/api/paymentmethods/{id}` | Ödeme yöntemini siler (kullanımda ise soft-delete) | Evet |

---

### 6.14 RolesController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/roles` | Rolleri listeler | Evet |
| GET | `/api/roles/{id}` | Rol detayını getirir | Evet |
| POST | `/api/roles` | Yeni rol oluşturur | Evet |
| PUT | `/api/roles/{id}` | Rol günceller | Evet |
| DELETE | `/api/roles/{id}` | Rol siler | Evet |

---

### 6.15 LimitsController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/limits` | Limitleri filtreleyerek sayfalı listeler | Evet |
| GET | `/api/limits/{id}` | Id'ye göre limit detayını getirir | Evet |
| POST | `/api/limits` | Yeni bir limit kaydı oluşturur | Evet |
| PUT | `/api/limits/{id}` | Var olan limit kaydını günceller | Evet |
| DELETE | `/api/limits/{id}` | Belirtilen id'deki limit kaydını siler | Evet |

---

### 6.16 LifecycleActionsController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/lifecycleactions` | Yaşam döngüsü aksiyonlarını filtreleyerek sayfalı listeler | Evet |
| GET | `/api/lifecycleactions/{id}` | Id'ye göre aksiyon detayını getirir | Evet |
| POST | `/api/lifecycleactions` | Yeni aksiyon oluşturur | Evet |
| PUT | `/api/lifecycleactions/{id}` | Aksiyonu günceller | Evet |
| DELETE | `/api/lifecycleactions/{id}` | Aksiyonu siler | Evet |

---

### 6.17 ProductLifecyclesController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/productlifecycles` | Ürün yaşam döngüsü kayıtlarını filtreleyerek sayfalı listeler | Evet |
| GET | `/api/productlifecycles/{id}` | Id'ye göre yaşam döngüsü kaydı detayını getirir | Evet |
| POST | `/api/productlifecycles` | Yeni yaşam döngüsü kaydı oluşturur | Evet |

---

### 6.18 ThermalPrintersController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/thermalprinters` | Termal yazıcıları filtreleyerek sayfalı listeler | Evet |
| GET | `/api/thermalprinters/{id}` | Id'ye göre termal yazıcı detayını getirir | Evet |
| POST | `/api/thermalprinters` | Yeni termal yazıcı oluşturur | Evet |
| PUT | `/api/thermalprinters/{id}` | Termal yazıcı bilgilerini günceller | Evet |
| DELETE | `/api/thermalprinters/{id}` | Termal yazıcıyı soft-delete yapar | Evet |
| DELETE | `/api/thermalprinters/{id}/hard` | Termal yazıcıyı kalıcı olarak siler | Evet |

---

### 6.19 UsersController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/users` | Kullanıcıları filtreleyerek sayfalı listeler | Evet |
| GET | `/api/users/{id}` | Kimliğe göre kullanıcı detayını getirir | Evet |
| PUT | `/api/users/{id}` | Kullanıcıyı günceller | Evet |
| DELETE | `/api/users/{id}` | Kullanıcıyı soft delete yapar | Evet |
| DELETE | `/api/users/{id}/hard` | Kullanıcıyı kalıcı olarak siler | Evet |

---

### 6.20 DashboardController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/dashboard/live-counters` | Gerçek zamanlı canlı sayaçlar (son satış zamanı, bugünkü işlem sayısı, stok senkronizasyonu) | Evet |
| GET | `/api/dashboard/weekly-trend` | Haftalık trend grafik verisi | Evet |
| GET | `/api/dashboard/daily-summary` | Gün sonu raporu (satış, kâr, en çok satan ürün, kritik stok) | Evet |
| GET | `/api/dashboard/anomalies` | Anomali algılama (satış düşüşü, stok seviyesi, risk skorları) | Evet |
| GET | `/api/dashboard/monthly-target` | Aylık satış hedefi (hedef tutar, mevcut satış, ilerleme yüzdesi) | Evet |
| GET | `/api/dashboard/reminders` | Hatırlatıcılar ve ajanda (kritik stok, uzun süre satılmayan ürünler, stok tükenme tahmini) | Evet |
| GET | `/api/dashboard/top-products` | En çok satan ürünler | Evet |
| GET | `/api/dashboard/workload-estimate` | Günlük iş yükü tahmini (yoğunluk seviyesi, tahmini işlem sayısı) | Evet |
| GET | `/api/dashboard/branch-comparison` | Şube karşılaştırması (satış, kâr, fiş sayısı, POS oranı, kritik stok, trend) | Evet |
| GET | `/api/dashboard/profit-loss` | Kar-Zarar tablosu (dönemsel kar-zarar analizi) | Evet |
| GET | `/api/dashboard/risk-score-legend` | Risk skor sözlüğü (0-100 aralığında risk seviyeleri ve açıklamaları) | Evet |
| GET | `/api/dashboard/summary` | Tüm parametresiz dashboard verilerini tek seferde döndürür (birleşik endpoint - broadcast yapmaz) | Evet |

**Önemli Notlar**:
- `summary` endpoint'i tüm parametresiz dashboard metodlarını paralel olarak çağırır (weekly-trend, monthly-target, reminders, workload-estimate, branch-comparison, risk-score-legend)
- `summary` endpoint'i broadcast yapmaz, sadece tek seferlik veri döndürür
- Broadcast yapan endpoint'ler: `live-counters`, `daily-summary`, `anomalies`
- `monthly-target` endpoint'i `MonthlyTargets` tablosundan hedef tutarı okur (mağaza bazlı)
- `workload-estimate` hibrit yaklaşım kullanır: mutlak eşikler (0-5=Düşük, 6-15=Orta, 16+=Yüksek) + yüzde bazlı kontrol
- `reminders` endpoint'i satış verisi yoksa uygun mesaj döndürür
- `risk-score-legend` endpoint'i frontend için risk seviyelerini ve renk kodlarını döndürür

---

### 6.21 StocksController (Güncellenmiş - Favori Endpoint Eklendi)

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| GET | `/api/stocks` | Stok listesi (sayfalı, filtreli) | Evet |
| GET | `/api/stocks/variant/{variantId}/detail` | Varyant detayı (şubeler bazında) | Evet |
| GET | `/api/stocks/{id}` | ID ile stok detayı | Evet |
| GET | `/api/stocks/by-barcode/{barcode}` | Barcode ile stok | Evet |
| GET | `/api/stocks/favorites` | En çok satılan ürünleri (favori ürünler) getirir | Evet |
| POST | `/api/stocks` | Yeni stok oluştur | Evet |
| PUT | `/api/stocks/{id}` | Stok güncelle | Evet |
| DELETE | `/api/stocks/{id}` | Stok sil (soft) | Evet |
| DELETE | `/api/stocks/{id}/hard` | Stok sil (hard) | Evet |

**Favoriler Endpoint Query Parametreleri**:
- `top` (default: 10): Kaç adet gösterilsin
- `days` (default: 30): Son kaç günün satışları
- `onlyMarked` (default: false): Sadece IsFavorite=true olanlar mı

---

## 6.22 API Endpoint Referans Tablosu (Tüm Controller'lar)

| Controller | Toplam Endpoint | CRUD | Özel Endpoint'ler |
|------------|----------------|------|-------------------|
| **AuthController** | 4 | - | register, login, validate-password, validate-register |
| **BanksController** | 7 | ✅ | /active, /hard |
| **BranchesController** | 7 | ✅ | /active, /hard |
| **CustomersController** | 5 | ✅ | - |
| **LifecycleActionsController** | 5 | ✅ | - |
| **LimitsController** | 5 | ✅ | - |
| **PaymentMethodsController** | 5 | ✅ | - |
| **ProductCategoriesController** | 7 | ✅ | /active, /hard |
| **ProductLifecyclesController** | 3 | ⚠️ | - (sadece GET, POST) |
| **ProductTypeController** | 7 | ✅ | /active, /hard |
| **ProductVariantController** | 7 | ✅ | /active, /hard |
| **PurchaseController** | 3 | ⚠️ | - (sadece GET, POST) |
| **DashboardController** | 12 | ❌ | summary, live-counters, weekly-trend, daily-summary, anomalies, monthly-target, reminders, top-products, workload-estimate, branch-comparison, profit-loss, risk-score-legend |
| **RolesController** | 5 | ✅ | - |
| **SalesController** | 3 | ⚠️ | - (CreateUnifiedAsync kullanır) |
| **StocksController** | 9 | ✅ | /variant/{id}/detail, /by-barcode/{barcode}, /favorites, /hard |
| **StoresController** | 7 | ✅ | /active, /hard |
| **ThermalPrintersController** | 6 | ✅ | /hard |
| **UsersController** | 5 | ⚠️ | - (Create yok, Register AuthController'da) |

**Toplam**: 19 Controller, 113+ Endpoint (ReportsController kaldırıldı, DashboardController eklendi, summary endpoint eklendi)

---

### 6.23 Known Issues ve Notlar

#### 6.23.1 Double Await Pattern (Düzeltildi)

**Önceki Durum**: Bazı controller'larda aynı servis metodu iki kez çağrılıyordu:
- `SalesController.GetPagedAsync` ve `GetById`
- `PurchaseController.GetPagedAsync` ve `GetById`
- `CustomersController.GetPagedAsync`
- `ProductLifecyclesController` (3 metod)
- `LifecycleActionsController` (5 metod)

**Çözüm**: Tüm controller'larda değişkene atayıp tekrar kullanma pattern'ine geçildi:
```csharp
// Önceki (YANLIŞ):
=> StatusCode((await _svc.GetPagedAsync(filter, ct)).StatusCode, await _svc.GetPagedAsync(filter, ct));

// Sonraki (DOĞRU):
var r = await _svc.GetPagedAsync(filter, ct);
return StatusCode(r.StatusCode, r);
```

**Performans İyileştirmesi**: Her endpoint çağrısında gereksiz veritabanı sorgusu ve işlem yükü ortadan kaldırıldı.

---

## 7. DTO'LAR (Data Transfer Objects)

### 7.1 DTO Amacı

DTO'lar, API ile client arasında veri alışverişi için kullanılan hafif veri modelleridir:
- Entity'lerin tüm özelliklerini expose etmez (güvenlik)
- Validation attribute'ları içerir
- Nested objeler ile ilişkili verileri birleştirir

### 7.2 Tüm DTO'lar - Nested Class'lar Dahil

#### 7.2.1 Stocks DTO Ailesi

**Dosya**: `Application/DTOs/Stocks/StocksDto.cs`

**İçerik**: 4 ana class + 2 nested class + 1 record

1. **StockDto** (Liste görünümü)
   - **Nested Class: VariantBrief** (Ürün varyant özet bilgisi)
   - **Nested Class: BranchBrief** (Şube özet bilgisi)
2. **StockCreateDto** (Yeni stok oluşturma)
3. **StockUpdateDto** (Stok güncelleme)
4. **StockFilter** (record - Filtreleme parametreleri)

```csharp
// Application/DTOs/Stocks/StocksDto.cs

public sealed class StockDto
{
    public int Id { get; set; }
    public VariantBrief? ProductVariant { get; set; }
    public BranchBrief? Branch { get; set; }
    public int? Quantity { get; set; }
    public string Barcode { get; set; } = null!;
    public string? QrCode { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal TotalWeight { get; set; } // Hesaplanan: Gram × Adet
    
    // NESTED CLASS 1
    public sealed class VariantBrief
    {
        public int? Id { get; set; }
        public string? Name { get; set; }        // Model (Ajda Bilezik)
        public string? Ayar { get; set; }
        public string? Color { get; set; }
        public string? Brand { get; set; }
        public decimal? Gram { get; set; }       // Stocks.Gram
        public int? ProductTypeId { get; set; }
        public string? ProductTypeName { get; set; }  // Tür (bilezik, yüzük)
        public string? CategoryName { get; set; }     // Kategori (Altın, Gümüş)
    }
    
    // NESTED CLASS 2
    public sealed class BranchBrief
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
    }
}

public sealed class StockCreateDto
{
    public int? ProductVariantId { get; set; }
    public int? BranchId { get; set; }
    public int Quantity { get; set; }
    public decimal Weight { get; set; }
    public string Barcode { get; set; } = null!;
    public string? QrCode { get; set; }
}

public sealed class StockUpdateDto
{
    public int? ProductVariantId { get; set; }
    public int? BranchId { get; set; }
    public int? Quantity { get; set; }
    public string? Barcode { get; set; }
    public string? QrCode { get; set; }
}

public sealed record StockFilter(
    int Page = 1,
    int PageSize = 20,
    string? Query = null,           // barcode/qr/variant/brand/ayar/renk
    int? BranchId = null,
    int? ProductTypeId = null,
    int? ProductVariantId = null,
    decimal? GramMin = null,
    decimal? GramMax = null,
    DateTime? UpdatedFromUtc = null,
    DateTime? UpdatedToUtc = null
);
```

---

**Dosya**: `Application/DTOs/Stocks/StockVariantDetailByStoreDto.cs`

**İçerik**: 1 ana class + 2 nested class

1. **StockVariantDetailByStoreDto** (Varyant bazlı, tüm şubelerde stok özeti)
   - **Nested Class: BranchBlock** (Şube bazlı toplam)
   - **Nested Class: StockChip** (Tek stok kartı)

```csharp
// Application/DTOs/Stocks/StockVariantDetailByStoreDto.cs

public sealed class StockVariantDetailByStoreDto
{
    public int VariantId { get; set; }
    public string VariantName { get; set; } = default!;
    public string? Ayar { get; set; }
    public string? Color { get; set; }
    public List<BranchBlock> Branches { get; set; } = new();
    
    // NESTED CLASS 1
    public sealed class BranchBlock
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = default!;
        public int ToplamAdet { get; set; }
        public decimal ToplamAgirlik { get; set; }
        public List<StockChip> Items { get; set; } = new();
    }
    
    // NESTED CLASS 2
    public sealed class StockChip
    {
        public int StockId { get; set; }
        public string Barcode { get; set; } = default!;
        public decimal Gram { get; set; }
        public string? Color { get; set; }
    }
}
```

---

#### 7.2.2 ProductVariant DTO Ailesi

**Dosya**: `Application/DTOs/ProductVariant/ProductVariantDto.cs`

**İçerik**: 4 ana class + 1 nested class + 1 record

1. **ProductVariantDto** (Detay görünümü)
   - **Nested Class: ProductTypeBrief** (Ürün türü ve kategori özeti)
2. **ProductVariantCreateDto** (Yeni varyant oluşturma)
3. **ProductVariantUpdateDto** (Varyant güncelleme)
4. **ProductVariantFilter** (record - Filtreleme)

```csharp
// Application/DTOs/ProductVariant/ProductVariantDto.cs

public sealed class ProductVariantDto
{
    public int Id { get; set; }
    public ProductTypeBrief? ProductType { get; set; }
    public string Name { get; set; } = default!;
    public string? Ayar { get; set; }
    public string? Color { get; set; }
    public string? Brand { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    
    // NESTED CLASS
    public sealed class ProductTypeBrief
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }  // Altın, Gümüş, Platin...
    }
}

public sealed class ProductVariantCreateDto
{
    public int? ProductTypeId { get; set; }
    public string Name { get; set; } = default!;
    public string? Ayar { get; set; }
    public string? Color { get; set; }
    public string? Brand { get; set; }
}

public sealed class ProductVariantUpdateDto
{
    public int? ProductTypeId { get; set; }
    public string Name { get; set; } = default!;
    public string? Ayar { get; set; }
    public string? Color { get; set; }
    public string? Brand { get; set; }
}

public sealed record ProductVariantFilter(
    int Page = 1,
    int PageSize = 20,
    string? Query = null,           // Name/Brand/Ayar/Color
    int? ProductTypeId = null,
    bool? IsActive = null,
    bool IncludeDeleted = false,
    DateTime? UpdatedFromUtc = null,
    DateTime? UpdatedToUtc = null
);
```

---

#### 7.2.3 ProductType DTO Ailesi

**Dosya**: `Application/DTOs/ProductType/ProductTypeDto.cs`

**İçerik**: 4 ana class + 1 nested class + 1 record

1. **ProductTypeDto** (Detay görünümü)
   - **Nested Class: CategoryBrief** (Kategori özeti)
2. **ProductTypeCreateDto**
3. **ProductTypeUpdateDto**
4. **ProductTypeFilter** (record)

```csharp
// Application/DTOs/ProductType/ProductTypeDto.cs

public sealed class ProductTypeDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public CategoryBrief? Category { get; set; }
    
    // NESTED CLASS
    public sealed class CategoryBrief
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
    }
}

public sealed class ProductTypeCreateDto
{
    public string Name { get; set; } = null!;
    public int? CategoryId { get; set; }
}

public sealed class ProductTypeUpdateDto
{
    public string Name { get; set; } = null!;
    public int? CategoryId { get; set; }
}

public sealed record ProductTypeFilter(
    int Page = 1,
    int PageSize = 20,
    string? Query = null,
    int? CategoryId = null,
    bool? IsActive = null,
    bool IncludeDeleted = false,
    DateTime? UpdatedFromUtc = null,
    DateTime? UpdatedToUtc = null
);
```

---

#### 7.2.4 Branches DTO Ailesi

**Dosya**: `Application/DTOs/Branches/BranchDto.cs`

**İçerik**: 4 ana class + 1 nested class + 1 record

1. **BranchDto** (Detay görünümü)
   - **Nested Class: StoreBrief** (Mağaza özeti)
2. **BranchCreateDto**
3. **BranchUpdateDto**
4. **BranchFilter** (record)

```csharp
// Application/DTOs/Branches/BranchDto.cs

public sealed class BranchDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public StoreBrief? Store { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    
    // NESTED CLASS
    public sealed class StoreBrief
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
    }
}

public sealed class BranchCreateDto
{
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public int? StoreId { get; set; }
}

public sealed class BranchUpdateDto
{
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public int? StoreId { get; set; }
}

public sealed record BranchFilter(
    int Page = 1,
    int PageSize = 20,
    string? Query = null,        // name/address
    int? StoreId = null,
    bool? IsActive = null,
    bool IncludeDeleted = false,
    DateTime? UpdatedFromUtc = null,
    DateTime? UpdatedToUtc = null
);
```

---

#### 7.2.5 Sales DTO Ailesi

**Dosya**: `Application/DTOs/Sales/SaleItemDto.cs`

**İçerik**: 6 ana class (nested yok)

1. **SaleItemDto** (Satış kalemi)
2. **SaleCreateDto** (Satış oluşturma)
3. **SaleResultDto** (Oluşum sonucu)
4. **SaleFilter** (Filtreleme)
5. **SaleListDto** (Liste görünümü)
6. **SaleLineDetailDto** (Detay görünümü)

```csharp
// Application/DTOs/Sales/SaleItemDto.cs

public sealed class SaleItemDto
{
    public int StockId { get; set; }
    public int Quantity { get; set; }
    public decimal SoldPrice { get; set; }
}

public sealed class SaleCreateDto
{
    public int? UserId { get; set; }          // yoksa CurrentUser
    public int BranchId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerNationalId { get; set; }
    public int? PaymentMethodId { get; set; }
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
    public int SaleId { get; init; }
    public int LineId { get; init; }          // sale_details.id
    public DateTime? CreatedAt { get; init; }
    public int? BranchId { get; init; }
    public string? BranchName { get; init; }
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public int StockId { get; init; }
    public string? ProductName { get; init; }
    public string? Ayar { get; init; }
    public string? Renk { get; init; }
    public decimal? AgirlikGram { get; init; }
    public int Quantity { get; init; }
    public decimal? SoldPrice { get; init; }
}

public sealed class SaleLineDetailDto
{
    public int SaleId { get; init; }
    public int LineId { get; init; }
    public DateTime? CreatedAt { get; init; }
    public string? PaymentMethod { get; init; }
    public int StockId { get; init; }
    public string? ProductName { get; init; }
    public string? Ayar { get; init; }
    public string? Renk { get; init; }
    public decimal? AgirlikGram { get; init; }
    public decimal? ListeFiyati { get; init; }
    public decimal? SatisFiyati { get; init; }
}
```

---

#### 7.2.6 Purchase DTO Ailesi

**Dosya**: `Application/DTOs/Purchase/PurchaseItemDto.cs`

**İçerik**: 7 ana class (nested yok)

1. **PurchaseItemDto** (Alış kalemi)
2. **PurchaseCreateDto** (Alış oluşturma)
3. **PurchaseResultDto** (Oluşum sonucu)
4. **PurchaseFilter** (Filtreleme)
5. **PurchaseListDto** (Liste görünümü)
6. **PurchaseDetailLineDto** (Detay satırı)
7. **PurchaseDetailDto** (Detay görünümü)

```csharp
// Application/DTOs/Purchase/PurchaseItemDto.cs

public sealed class PurchaseItemDto
{
    public int ProductVariantId { get; set; }
    public int BranchId { get; set; }
    public string Barcode { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
}

public sealed class PurchaseCreateDto
{
    public int UserId { get; set; }
    public int BranchId { get; set; }
    public int? CustomerId { get; set; }
    public int? PaymentMethodId { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = new();
}

public sealed class PurchaseResultDto
{
    public int Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public IReadOnlyList<int> StockIds { get; set; } = Array.Empty<int>();
}

public sealed class PurchaseFilter
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

public sealed class PurchaseListDto
{
    public int Id { get; init; }
    public DateTime? CreatedAt { get; init; }
    public int? BranchId { get; init; }
    public string? BranchName { get; init; }
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public int? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public int? PaymentMethodId { get; init; }
    public string? PaymentMethod { get; init; }
    public decimal TotalAmount { get; init; }
    public int ItemCount { get; init; }
}

public sealed class PurchaseDetailLineDto
{
    public int Id { get; init; }
    public int StockId { get; init; }
    public string? Barcode { get; init; }
    public int Quantity { get; init; }
    public decimal? PurchasePrice { get; init; }
    public int? ProductVariantId { get; init; }
    public string? VariantDisplay { get; init; }
}

public sealed class PurchaseDetailDto
{
    public int Id { get; init; }
    public DateTime? CreatedAt { get; init; }
    public int? BranchId { get; init; }
    public string? BranchName { get; init; }
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public int? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public int? PaymentMethodId { get; init; }
    public string? PaymentMethod { get; init; }
    public decimal TotalAmount { get; init; }
    public int ItemCount { get; init; }
    public IReadOnlyList<PurchaseDetailLineDto> Lines { get; init; } = Array.Empty<PurchaseDetailLineDto>();
}
```

---

#### 7.2.7 Auth DTO Ailesi

**Dosya 1**: `Application/DTOs/Auth/LoginDto.cs`

```csharp
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

**Dosya 2**: `Application/DTOs/Auth/RegisterDto.cs`

```csharp
public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int? RoleId { get; set; }
    public int? BranchId { get; set; }
}
```

**Dosya 3**: `Application/DTOs/Auth/AuthResponseDto.cs`

```csharp
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}
```

**Dosya 4**: `Application/DTOs/Auth/PasswordCheckRequestDto.cs`

**ÖNEMLİ**: Bu dosyada **3 ayrı class** var!

```csharp
// Application/DTOs/Auth/PasswordCheckRequestDto.cs

// CLASS 1
public sealed class PasswordCheckRequestDto
{
    public string Password { get; set; } = default!;
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

// CLASS 2
public sealed class PasswordCheckResultDto
{
    public bool IsValid { get; set; }
    public int Score { get; set; }
    public List<string> Errors { get; set; } = new();
}

// CLASS 3
public sealed class RegisterValidationResultDto
{
    public bool IsValid { get; set; }
    public int PasswordScore { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

---

#### 7.2.8 Diğer Basit DTO Aileleri

**ProductCategory, Customers, PaymentMethods, Roles, Limits, LifecycleActions, ProductLifecycles, Banks, Stores** gibi entity'ler için DTO'lar standart CRUD deseni izler:
- `{Entity}Dto` (Detay/Liste)
- `{Entity}CreateDto`
- `{Entity}UpdateDto`
- `{Entity}Filter` (opsiyonel)

**Nested class içermezler**, doğrudan property'lerden oluşurlar.

---

## 8. GÜVENLİK VE KİMLİK DOĞRULAMA - EKSİKSİZ DETAYLAR

### 8.1 Parola Güvenliği - PasswordHasher ve PasswordOptions

#### 8.1.1 PasswordHasher Class

**Dosya**: `Infrastructure/PasswordHasher/PasswordHasher.cs`

**İçerik**: 1 ana class + 2 private helper metod

```csharp
// Infrastructure/PasswordHasher/PasswordHasher.cs

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordOptions _opt;
    
    public PasswordHasher(IOptions<PasswordOptions> opt)
    {
        _opt = opt.Value;
    }
    
    // Base64 formatında 16 byte salt üretir
    public string GenerateSalt(int size = 16)
    {
        var bytes = new byte[size];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
    
    // SHA-256 + (salt || password || pepper) + iterasyon
    public string Hash(string password, string saltBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);
        var pepperBytes = Encoding.UTF8.GetBytes(_opt.Pepper ?? string.Empty);
        
        // İlk birleşim: salt + password + pepper
        var input = Combine(salt, Encoding.UTF8.GetBytes(password), pepperBytes);
        
        // Iterative hashing (brute-force zorlaştırma)
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(input);
        for (int i = 1; i < _opt.Iterations; i++)
            hash = sha.ComputeHash(hash);
        
        return Convert.ToBase64String(hash);
    }
    
    // Timing attack'a karşı constant-time karşılaştırma
    public bool Verify(string password, string saltBase64, string expectedHashBase64)
    {
        var computed = Hash(password, saltBase64);
        var a = Convert.FromBase64String(computed);
        var b = Convert.FromBase64String(expectedHashBase64);
        return FixedTimeEquals(a, b);
    }
    
    // HELPER: Byte dizilerini birleştir
    private static byte[] Combine(params byte[][] arrays)
    {
        var len = arrays.Sum(a => a.Length);
        var result = new byte[len];
        int pos = 0;
        foreach (var arr in arrays)
        {
            Buffer.BlockCopy(arr, 0, result, pos, arr.Length);
            pos += arr.Length;
        }
        return result;
    }
    
    // HELPER: Sabit zamanlı karşılaştırma (timing attack prevention)
    private static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
```

---

#### 8.1.2 PasswordOptions Class

**Dosya**: `Infrastructure/Auth/PasswordOptions.cs`

**Amaç**: Parola hashleme ayarlarını `appsettings.json` ile bind etmek

```csharp
// Infrastructure/Auth/PasswordOptions.cs

public sealed class PasswordOptions
{
    [Range(1_000, 1_000_000)]
    public int Iterations { get; init; } = 100_000;  // Varsayılan 100k iterasyon
    
    // Opsiyonel ama önerilir: uygulama seviyesinde "pepper"
    [MinLength(0)]
    public string Pepper { get; init; } = string.Empty;
}
```

**appsettings.json Örneği**:
```json
{
  "Password": {
    "Iterations": 100000,
    "Pepper": "MySecretPepper2024!"
  }
}
```

**DI Registration** (`Infrastructure/DependencyInjection.cs`):
```csharp
services.AddOptions<PasswordOptions>()
    .Bind(configuration.GetSection("Password"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

---

### 8.2 JWT Authentication - JwtService ve JwtOptions (NESTED CLASS!)

#### 8.2.1 JwtService + JwtOptions Class

**Dosya**: `Infrastructure/Services/JwtService/JwtService.cs`

**ÖNEMLİ**: Bu dosyada **2 class** var:
1. **JwtService** (Ana servis)
2. **JwtOptions** (NESTED CLASS - aynı dosya içinde, satır 103-120)

```csharp
// Infrastructure/Services/JwtService/JwtService.cs

public sealed class JwtService : IJwtService
{
    private static byte[] DecodeKey(string? b64)
    {
        if (string.IsNullOrWhiteSpace(b64))
            throw new InvalidOperationException("Jwt:Key boş!");
        var clean = b64.Trim();
        return Convert.FromBase64String(clean);
    }
    
    private readonly JwtOptions _opt;
    private readonly SigningCredentials _creds;
    private readonly JwtHeader _headerTemplate;
    
    public JwtService(IOptions<JwtOptions> options)
    {
        _opt = options.Value ?? throw new ArgumentNullException(nameof(options));
        
        var keyBytes = DecodeKey(_opt.Key);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        _creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        _headerTemplate = new JwtHeader(_creds);
        if (!string.IsNullOrWhiteSpace(_opt.KeyId))
        {
            // kid header anahtar rotasyonu için
            _headerTemplate["kid"] = _opt.KeyId;
        }
    }
    
    public AuthResponseDto GenerateToken(Users user)
    {
        ArgumentNullException.ThrowIfNull(user);
        
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_opt.ExpiryMinutes);
        
        var claims = BuildClaims(user);
        
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _creds
        );
        
        // Header template'teki kid vs. değerlerini uygula
        foreach (var kv in _headerTemplate)
            token.Header[kv.Key] = kv.Value;
        
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        return new AuthResponseDto
        {
            Token = tokenString,
            Expiration = expires
        };
    }
    
    private static IEnumerable<Claim> BuildClaims(Users user)
    {
        // PII içermeyen, minimal claim seti
        yield return new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString());
        yield return new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());
        yield return new Claim(JwtRegisteredClaimNames.UniqueName, user.Username);
        
        // İsim-soyisim (varsa)
        if (!string.IsNullOrWhiteSpace(user.FirstName))
            yield return new Claim("given_name", user.FirstName);
        if (!string.IsNullOrWhiteSpace(user.LastName))
            yield return new Claim("surname", user.LastName);
        
        // Özel (custom) claim'ler – veritabanı alanlarına 1:1
        if (user.RoleId.HasValue)
            yield return new Claim("role_id", user.RoleId.Value.ToString());
        
        if (user.BranchId.HasValue)
            yield return new Claim("branch_id", user.BranchId.Value.ToString());
        
        yield return new Claim("is_active", user.IsActive ?? false ? "true" : "false");
        yield return new Claim("must_change_password", user.MustChangePassword ? "true" : "false");
    }
}

// ====================================================================
// NESTED CLASS (AYNI DOSYA İÇİNDE!) - Satır 103-120
// ====================================================================

public sealed class JwtOptions
{
    [Required, MinLength(1)]
    public string Issuer { get; init; } = default!;
    
    [Required, MinLength(1)]
    public string Audience { get; init; } = default!;
    
    // HS256 için en az 32 byte önerilir
    [Required, MinLength(32, ErrorMessage = "Jwt Key en az 32 karakter olmalıdır.")]
    public string Key { get; init; } = default!;
    
    [Range(5, 1440)]
    public int ExpiryMinutes { get; init; } = 60;
    
    // İsteğe bağlı: key rotation için header'a yazılır
    public string? KeyId { get; init; }
}
```

**appsettings.json Örneği**:
```json
{
  "Jwt": {
    "Issuer": "KuyumStokApi",
    "Audience": "KuyumStokApiClients",
    "Key": "Base64EncodedSecretKeyHere==",
    "ExpiryMinutes": 1440,
    "KeyId": "v1"
  }
}
```

**DI Registration**:
```csharp
services.AddOptions<JwtOptions>()
    .Bind(configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "Jwt Key boş olamaz.")
    .ValidateOnStart();

services.AddSingleton<IJwtService, JwtService>();
```

---

### 8.3 CurrentUser Context - ICurrentUserContext ve Implementation'ları

#### 8.3.1 ICurrentUserContext Interface

**Dosya**: `Application/Interfaces/Auth/ICurrentUserContext.cs`

```csharp
// Application/Interfaces/Auth/ICurrentUserContext.cs

public interface ICurrentUserContext
{
    int? UserId { get; }
    int? BranchId { get; }
}
```

---

#### 8.3.2 CurrentUserContext Implementation

**Dosya**: `Application/Interfaces/Auth/CurrentUserContext.cs`

**ÖNEMLİ**: Bu implementation **Application katmanında** (normalde Infrastructure'da olmalı ama mevcut proje böyle)

```csharp
// Application/Interfaces/Auth/CurrentUserContext.cs

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _http;
    
    public CurrentUserContext(IHttpContextAccessor http) => _http = http;
    
    public int? UserId => ReadInt("userId", "sub", ClaimTypes.NameIdentifier);
    public int? BranchId => ReadInt("branchId", "branch_id", "branch");
    
    // HELPER: JWT claim'lerinden int oku (fallback destekli)
    private int? ReadInt(params string[] keys)
    {
        var claims = _http.HttpContext?.User?.Claims;
        if (claims is null) return null;
        
        foreach (var k in keys)
        {
            var v = claims.FirstOrDefault(c => c.Type.Equals(k, StringComparison.OrdinalIgnoreCase))?.Value;
            if (int.TryParse(v, out var num)) return num;
        }
        return null;
    }
}
```

**Kullanım**:
```csharp
public class StocksService : IStocksService
{
    private readonly ICurrentUserContext _user;
    
    public async Task<ApiResult<PagedResult<StockDto>>> GetPagedAsync(StockFilter filter)
    {
        // Kullanıcı branch'ı JWT'den otomatik al
        var branchId = filter.BranchId ?? _user.BranchId;
        
        // Aynı ProductVariantId + BranchId'ye sahip stokları grupla
        var grouped = from s in _db.Stocks
                      where s.BranchId == branchId
                      group s by new { s.ProductVariantId, s.BranchId } into g
                      select new {
                          ProductVariantId = g.Key.ProductVariantId,
                          BranchId = g.Key.BranchId,
                          TotalQuantity = g.Sum(x => x.Quantity ?? 0),
                          TotalWeight = g.Sum(x => (x.Gram ?? 0) * (decimal)(x.Quantity ?? 0))
                          // ...
                      };
        // ...
    }
}
```

---

#### 8.3.3 ICurrentUserService Interface (Backward Compatibility)

**Dosya**: `Application/Common/ICurrenUserService.cs` (TYPO: ICurren, not ICurrent!)

**Amaç**: Eski kod uyumluluğu için, ek property'ler içerir

```csharp
// Application/Common/ICurrenUserService.cs

public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
```

---

#### 8.3.4 CurrentUserService Implementation

**Dosya**: `Infrastructure/Auth/CurrentUserService.cs`

```csharp
// Infrastructure/Auth/CurrentUserService.cs

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    
    public CurrentUserService(IHttpContextAccessor http) => _http = http;
    
    public bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    
    public string? UserName
    {
        get
        {
            var u = _http.HttpContext?.User;
            return u?.Identity?.Name
                ?? u?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                ?? u?.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
    
    public int? UserId
    {
        get
        {
            var u = _http.HttpContext?.User;
            var id =
                u?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                u?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            
            return int.TryParse(id, out var i) ? i : null;
        }
    }
}
```

**DI Registration**:
```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

// Infrastructure/DependencyInjection.cs
services.AddScoped<ICurrentUserService, CurrentUserService>();
```

---

### 8.4 Token Blacklist Servisi (ITokenBlacklistService)

**Dosya**: `Application/Interfaces/Services/ITokenBlacklistService.cs` ve `Infrastructure/Services/TokenBlacklistService/TokenBlacklistService.cs`

**Amaç**: Logout yapılan JWT token'larını geçersiz kılmak için blacklist mekanizması sağlar.

**Interface**:
```csharp
public interface ITokenBlacklistService
{
    Task InvalidateTokenAsync(string jti, DateTime expiresAt, CancellationToken ct = default);
    Task<bool> IsTokenInvalidatedAsync(string jti, CancellationToken ct = default);
}
```

**Implementation**:
- `InvalidateTokenAsync`: JWT'nin JTI değerini `InvalidatedTokens` tablosuna ekler
- `IsTokenInvalidatedAsync`: JTI'nin blacklist'te olup olmadığını kontrol eder

**Kullanım**: JWT doğrulama sırasında (`Program.cs` içinde `OnTokenValidated` event'inde) token blacklist kontrolü yapılır.

---

### 8.5 Refresh Token Servisi (IRefreshTokenService)

**Amaç**: Kullanıcı oturumlarını yenilemek için refresh token yönetimi sağlar.

**Özellikler**:
- Refresh token oluşturma ve saklama
- Token iptal etme (revoke)
- Token doğrulama
- Expire olan token'ları temizleme

**İlişkiler**: `RefreshTokens` entity'si `Users` ile Many-to-One ilişkilidir.

---

### 8.6 Program.cs JWT Yapılandırması ve SignalR

**Tam Pipeline**:
```csharp
// Program.cs (Top-level statements)

using KuyumStokApi.Infrastructure;
using KuyumStokApi.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

static byte[] DecodeKey(string? b64)
{
    if (string.IsNullOrWhiteSpace(b64))
        throw new InvalidOperationException("Jwt:Key boş!");
    return Convert.FromBase64String(b64.Trim());
}

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddSignalR(); // SignalR desteği

// CORS yapılandırması (SignalR için gerekli)
builder.Services.AddCors(options =>
{
    // SignalR için özel policy
    options.AddPolicy("SignalRCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // DEV: Tüm origin'lere izin (production'da spesifik origin'ler)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // SignalR için credentials gerekli
    });
    
    // Default policy (diğer endpoint'ler için)
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddPersistence(cfg);
builder.Services.AddInfrastructure(cfg);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

// JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keyBytes = DecodeKey(cfg["Jwt:Key"]);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cfg["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = cfg["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero  // Token expire olunca hemen geçersiz
        };
        
        // Token blacklist kontrolü
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var jtiClaim = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Jti);
                if (jtiClaim != null)
                {
                    var tokenBlacklistService = ctx.HttpContext.RequestServices
                        .GetRequiredService<ITokenBlacklistService>();
                    var isInvalidated = await tokenBlacklistService.IsTokenInvalidatedAsync(jtiClaim.Value);
                    if (isInvalidated)
                    {
                        ctx.Fail("Token geçersiz kılınmış (logout yapılmış).");
                        return;
                    }
                }
            },
            OnMessageReceived = context =>
            {
                // SignalR için query string'den token al
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/hubs"))
                {
                    context.Token = accessToken;
                }
                // Authorization header'dan da al
                else if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Swagger + JWT Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header. Example: 'Bearer {token}'"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS (Authentication'dan önce olmalı)
app.UseCors();

// Static files (test HTML sayfası için)
app.UseStaticFiles();

app.UseAuthentication();  // JWT middleware (ÖNCE!)
app.UseAuthorization();   // Authorization middleware (SONRA!)

app.MapControllers();

// SignalR hub'ına özel CORS policy uygula
app.MapHub<DashboardHub>("/api/hubs/dashboard")
   .RequireCors("SignalRCorsPolicy");

// Veritabanı migration ve seed data (app.Run öncesi!)
await app.MigrateAndSeedAsync();

app.Run();
```

---

## 9. ÖNEMLİ ÖZELLİKLER VE DESENLER

### 9.1 ApiResult<T> ve PagedResult<TItem> - Standart Response Modelleri

**Dosya**: `Application/Common/ApiResult.cs`

**İçerik**: 2 ayrı class (nested değil!)

#### 9.1.1 ApiResult<T> Class

Tüm API endpoint'leri için standart yanıt yapısı:

```csharp
// Application/Common/ApiResult.cs

public class ApiResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string TraceId { get; set; } = Guid.NewGuid().ToString();
    
    // Static factory method: Başarılı yanıt
    public static ApiResult<T> Ok(T data, string message = "", int statusCode = 200) =>
        new ApiResult<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = statusCode
        };
    
    // Static factory method: Hatalı yanıt
    public static ApiResult<T> Fail(string message, List<string>? errors = null, int statusCode = 400) =>
        new ApiResult<T>
        {
            Success = false,
            Message = message,
            Errors = errors,
            StatusCode = statusCode
        };
}
```

**Kullanım Örnekleri**:

```csharp
// Controller içinde - Başarılı
public async Task<ActionResult<ApiResult<StockDto>>> GetById(int id)
{
    var stock = await _service.GetByIdAsync(id);
    if (stock == null)
        return NotFound(ApiResult<StockDto>.Fail("Stok bulunamadı", statusCode: 404));
    
    return Ok(ApiResult<StockDto>.Ok(stock, "Stok başarıyla bulundu", 200));
}

// Service içinde - Validation hatası
return ApiResult<bool>.Fail("Geçersiz giriş", 
    new List<string> { "Barcode boş olamaz", "Quantity 0'dan büyük olmalı" },
    statusCode: 400);

// Service içinde - Başarılı işlem
return ApiResult<SaleResultDto>.Ok(result, "Satış başarıyla oluşturuldu", 201);
```

**JSON Çıktı Örneği**:
```json
{
  "success": true,
  "message": "Stok başarıyla bulundu",
  "errors": null,
  "data": {
    "id": 123,
    "barcode": "ABC123",
    "quantity": 5
  },
  "statusCode": 200,
  "timestamp": "2025-11-09T12:34:56Z",
  "traceId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

---

#### 9.1.2 PagedResult<TItem> Class

Büyük listelerin sayfalı olarak döndürülmesi için:

```csharp
// Application/Common/ApiResult.cs (aynı dosyada!)

public sealed class PagedResult<TItem>
{
    public IReadOnlyList<TItem> Items { get; init; } = Array.Empty<TItem>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
}
```

**Kullanım Örnekleri**:

```csharp
// Service içinde - Sayfalı liste oluşturma
public async Task<ApiResult<PagedResult<StockDto>>> GetPagedAsync(StockFilter filter, CancellationToken ct)
{
    var page = Math.Max(1, filter.Page);
    var pageSize = Math.Clamp(filter.PageSize, 1, 200);
    
    var query = _db.Stocks
        .AsNoTracking()
        .Where(s => s.IsDeleted == false);
    
    // Filter uygula (branch, query, vb.)
    if (filter.BranchId.HasValue)
        query = query.Where(s => s.BranchId == filter.BranchId);
    
    // Total count (pagination için)
    var totalCount = await query.LongCountAsync(ct);
    
    // Sayfalama + DTO projection
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(s => new StockDto
        {
            Id = s.Id,
            Barcode = s.Barcode,
            Quantity = s.Quantity,
            // ...
        })
        .ToListAsync(ct);
    
    var pagedResult = new PagedResult<StockDto>
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    };
    
    return ApiResult<PagedResult<StockDto>>.Ok(pagedResult);
}
```

**JSON Çıktı Örneği**:
```json
{
  "success": true,
  "message": "",
  "errors": null,
  "data": {
    "items": [
      { "id": 1, "barcode": "ABC123", "quantity": 5 },
      { "id": 2, "barcode": "DEF456", "quantity": 3 }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 156
  },
  "statusCode": 200,
  "timestamp": "2025-11-09T12:34:56Z",
  "traceId": "..."
}
```

**Frontend Kullanımı**:
```typescript
// TypeScript/React örneği
interface ApiResult<T> {
  success: boolean;
  message?: string;
  errors?: string[];
  data?: T;
  statusCode: number;
  timestamp: string;
  traceId: string;
}

interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// API çağrısı
const response = await fetch(`/api/stocks?page=1&pageSize=20`);
const result: ApiResult<PagedResult<StockDto>> = await response.json();

if (result.success && result.data) {
  const { items, page, pageSize, totalCount } = result.data;
  const totalPages = Math.ceil(totalCount / pageSize);
  
  console.log(`Showing ${items.length} of ${totalCount} stocks (Page ${page}/${totalPages})`);
}
```

---

### 9.2 Soft Delete Pattern

Birçok entity soft delete destekler:
- `IsDeleted`: Silinmiş mi?
- `DeletedAt`: Ne zaman silindi?
- `DeletedBy`: Kim sildi?

**Sorgu Filtreleme**:
```csharp
var activeCustomers = await _db.Customers
    .Where(c => c.IsDeleted == false)
    .ToListAsync();
```

**Soft Delete İşlemi**:
```csharp
public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct)
{
    var entity = await _db.ProductVariants.FindAsync(id);
    if (entity == null)
        return ApiResult<bool>.Fail("Kayıt bulunamadı", statusCode: 404);
    
    entity.IsDeleted = true;
    entity.DeletedAt = DateTime.UtcNow;
    entity.DeletedBy = _currentUser.UserId;
    
    await _db.SaveChangesAsync(ct);
    return ApiResult<bool>.Ok(true, "Kayıt silindi");
}
```

---

### 9.3 Transaction Yönetimi

Kritik işlemler transaction ile korunur:
```csharp
using var tx = await _db.Database.BeginTransactionAsync(ct);

try
{
    // 1. Stok düşür
    stock.Quantity -= item.Quantity;
    
    // 2. Satış kaydı oluştur
    var sale = new Sales { /* ... */ };
    _db.Sales.Add(sale);
    
    // 3. Lifecycle ekle
    var lifecycle = new ProductLifecycles { /* ... */ };
    _db.ProductLifecycles.Add(lifecycle);
    
    await _db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
}
catch
{
    // Rollback otomatik (using scope)
    throw;
}
```

---

### 9.4 Entity Framework Query Optimization

- **AsNoTracking()**: Read-only sorgularda performans
- **Include()**: Eager loading (ilişkili verileri tek sorguda çek)
- **Select()**: Projection (sadece gerekli kolonları çek)
- **Join**: Manuel join ile daha kontrollü sorgular

**Örnek**:
```csharp
// ✅ İyi: Direct projection (performanslı)
var stocks = await _db.Stocks
    .AsNoTracking()
    .Select(s => new StockDto
    {
        Id = s.Id,
        Barcode = s.Barcode,
        ProductVariant = new StockDto.VariantBrief
        {
            Name = s.ProductVariant != null ? s.ProductVariant.Name : null,
            Ayar = s.ProductVariant != null ? s.ProductVariant.Ayar : null
        }
    })
    .ToListAsync();

// ❌ Kötü: Full entity loading (gereksiz)
var stocks = await _db.Stocks
    .Include(s => s.ProductVariant)
    .ToListAsync();
```

---

## 10. İLİŞKİLER VE BAĞIMLILIKLAR

### 10.1 Entity İlişki Diyagramı (ER Diagram)

```
Stores (1) ────────┐
                   │
                   │ (1:N)
                   ▼
              Branches (N) ─────────────┐
                   │                    │
                   │ (1:N)              │ (1:N)
                   ▼                    ▼
              Users (N)             Stocks (N)
                │  │                    │
                │  │                    │
    (1:N)       │  │ (1:N)              │ (N:1)
                │  │                    │
                ▼  ▼                    ▼
           Purchases/Sales      ProductVariants (N)
                │  │                    │
                │  │                    │ (N:1)
                │  │                    ▼
                │  │              ProductTypes (N)
                │  │                    │
                │  │                    │ (N:1)
                │  │                    ▼
                ▼  ▼              ProductCategories (1)
         Purchase/SaleDetails
                │
                │ (1:N)
                ▼
             Stocks (N)
                │
                │ (1:N)
                ▼
         ProductLifecycles (N)
```

### 10.2 Kritik İlişkiler

**1. Store → Branches → Stocks**
- Bir mağaza birden fazla şubeye sahip
- Her şube ayrı stok tutar
- Şubeler arası stok transferi ProductLifecycles ile takip edilir

**2. Users → Branch**
- Her kullanıcı bir şubeye atanır
- Kullanıcı sadece kendi şubesinin stokunu yönetir (genelde)

**3. ProductCategories → ProductTypes → ProductVariants → Stocks**
- 4 seviye hiyerarşi
- Kategori > Tip > Varyant > Stok Kalemi

**4. Sales/Purchases → Details → Stocks**
- Her satış/alış fişi birden fazla kaleme sahip
- Her kalem bir stok kalemine bağlıdır

**5. ProductLifecycles**
- Tüm stok hareketlerini kaydeder
- Denetim (audit trail) için kritik

---

## 11. PROJE ÇALIŞTIRMA

### 11.1 Gereksinimler

- **.NET 8.0 SDK**
- **PostgreSQL** veritabanı
- **IDE**: Visual Studio 2022 / Rider / VS Code

### 11.2 Yapılandırma

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=kuyumstok;Username=postgres;Password=****"
  },
  "Jwt": {
    "Key": "BASE64_ENCODED_SECRET_KEY",
    "Issuer": "KuyumStokApi",
    "Audience": "KuyumStokApiClients",
    "ExpiresInMinutes": 1440
  },
  "Password": {
    "Pepper": "SECRET_PEPPER_VALUE",
    "Iterations": 10000
  }
}
```

### 11.3 Migration ve Veritabanı

```bash
# Migration oluştur
dotnet ef migrations add InitialCreate -p KuyumStokApi.Persistence -s KuyumStokApi.API

# Veritabanına uygula
dotnet ef database update -p KuyumStokApi.Persistence -s KuyumStokApi.API
```

### 11.4 Çalıştırma

```bash
cd KuyumStokApi.API
dotnet run
```

**Swagger UI**: `https://localhost:7xxx/swagger`

---

## 12. ÖRNEK KULLANIM SENARYOLARI

### Senaryo 1: Yeni Ürün Alışı

1. **Kategori Oluştur**: `POST /api/productcategories` (Yüzük)
2. **Tip Oluştur**: `POST /api/producttype` (Nişan Yüzüğü)
3. **Varyant Oluştur**: `POST /api/productvariant` (14 ayar Beyaz Altın)
4. **Alış Yap**: `POST /api/purchase`
   - Barcode: `YZK001`
   - Quantity: 5
   - PurchasePrice: 1500
5. **Sonuç**: Stok oluşur, ProductLifecycles'a "Purchase" kaydı düşer

### Senaryo 2: Müşteriye Satış

1. **Müşteri Ara**: `GET /api/customers?Query=Ahmet`
2. **Stok Bul**: `GET /api/stocks/by-barcode/YZK001`
3. **Satış Yap**: `POST /api/sales`
   ```json
   {
     "branchId": 1,
     "userId": 5,
     "customerName": "Ahmet Yılmaz",
     "customerPhone": "05551234567",
     "paymentMethodId": 2,
     "items": [
       { "stockId": 15, "quantity": 1, "soldPrice": 2000 }
     ]
   }
   ```
4. **Sonuç**:
   - Stok düşer (5 → 4)
   - Satış fişi oluşur
   - ProductLifecycles'a "Sale" kaydı

### Senaryo 3: Stok Limit Uyarısı

1. **Limit Belirle**: `POST /api/limits`
   ```json
   {
     "branchId": 1,
     "productVariantId": 3,
     "minThreshold": 2,
     "maxThreshold": 50
   }
   ```
2. **Stok Kontrolü**: Frontend, stock quantity ile limit'i karşılaştırır
3. **Uyarı**: Quantity < MinThreshold ise "Stok azaldı" uyarısı

---

## 13. GELECEKTEKİ GELİŞTİRMELER

### Olası Özellikler

1. **Raporlama**:
   - Günlük/aylık satış raporları
   - Şube bazlı kar/zarar analizi
   - Stok devir hızı

2. **Şubeler Arası Transfer**:
   - Transfer talebi oluştur
   - Onay mekanizması
   - Lifecycle'da "Transfer" kaydı

3. **Stok Sayım**:
   - Fiziksel sayım girişi
   - Fark analizi (eksik/fazla)
   - Düzeltme kayıtları

4. **Barkod Yazdırma**:
   - PDF/Zebra label üretimi
   - QR kod desteği

5. **Rol Bazlı Yetkilendirme**:
   - Permission tablosu
   - Role-Permission mapping
   - Endpoint seviyesinde yetki kontrolü

6. **Bildirim Sistemi**:
   - Stok limiti aşıldığında email/SMS
   - Büyük satışlarda yönetici bildirimi

7. **Dashboard**:
   - Real-time stok durumu
   - Bugünün satışları
   - En çok satan ürünler

---

## 14. SONUÇ

**KuyumStokApi**, modern yazılım mimarisi prensipleriyle geliştirilmiş, kuyumculuk sektörüne özel kapsamlı bir stok yönetim sistemidir. Clean Architecture yaklaşımı, SOLID prensipleri, güvenli kimlik doğrulama ve transaction yönetimi ile enterprise seviyede bir çözümdür.

### Öne Çıkan Özellikler:
✅ Katmanlı mimari (Domain, Application, Infrastructure, Persistence, API)  
✅ Entity Framework Core ile type-safe veritabanı erişimi  
✅ JWT tabanlı güvenli kimlik doğrulama  
✅ SHA-256 + Salt + Pepper ile güvenli parola hash'leme  
✅ Transaction yönetimi ile veri tutarlılığı  
✅ Soft delete desteği  
✅ Sayfalama ve filtreleme  
✅ RESTful API standartları  
✅ Swagger UI entegrasyonu  
✅ Comprehensive error handling (ApiResult<T>)  
✅ Product lifecycle tracking (audit trail)  
✅ Branch-based inventory management  
✅ POS commission tracking  

### Teknolojiler:
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- PostgreSQL
- JWT (System.IdentityModel.Tokens.Jwt)
- Swagger/OpenAPI
- Dependency Injection
- Async/Await pattern

---

**Doküman Tarihi**: 8 Aralık 2025  
**Versiyon**: 1.2  
**Hazırlayan**: AI Assistant  
**Güncellemeler**: 
- MonthlyTargets entity eklendi (aylık satış hedefleri)
- DashboardController eklendi (12 endpoint - summary endpoint eklendi)
- ReportsController kaldırıldı
- Risk Score Legend endpoint eklendi
- Workload-estimate hibrit yaklaşımla güncellendi
- Reminders endpoint'i avgDailySales = 0 kontrolü eklendi
- RefreshTokens ve InvalidatedTokens entity'leri eklendi (JWT token yönetimi)
- Users entity'sine MustChangePassword alanı eklendi
- TokenBlacklistService eklendi (JWT token blacklist mekanizması)
- RefreshTokenService eklendi (refresh token yönetimi)
- SignalR hub yapılandırması eklendi (DashboardHub - real-time güncellemeler)
- JWT authentication'a token blacklist kontrolü eklendi
- CORS yapılandırması eklendi (SignalR desteği için)

---

## EKLER

### A. Veritabanı Şeması (PostgreSQL)

```sql
-- Örnek tablo yapıları
CREATE TABLE public.stores (
    id SERIAL PRIMARY KEY,
    name VARCHAR NOT NULL,
    is_active BOOLEAN DEFAULT true,
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE public.branches (
    id SERIAL PRIMARY KEY,
    store_id INTEGER REFERENCES stores(id),
    name VARCHAR NOT NULL,
    address VARCHAR,
    is_active BOOLEAN DEFAULT true,
    is_deleted BOOLEAN DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Diğer tablolar için AppDbContext.cs dosyasına bakınız
```

### B. Dependency Injection Yapılandırması

**KuyumStokApi.Infrastructure/DependencyInjection.cs**:
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Services
        services.AddScoped<IStocksService, StocksService>();
        services.AddScoped<ISalesService, SalesService>();
        services.AddScoped<IPurchasesService, PurchasesService>();
        // ... diğer servisler
        
        // Auth
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        // Configuration
        services.Configure<PasswordOptions>(
            configuration.GetSection("Password"));
        
        return services;
    }
}
```

---

## 11. NESTED CLASS'LAR VE AYNI DOSYADA TANIMLANAN SINIFLAR - EKSİKSİZ LİSTE

Bu bölüm, projedeki **tüm nested class'ları** ve **aynı dosyada tanımlanmış birden fazla class'ı** listeler.

### 11.1 Infrastructure Katmanı

#### 📁 `Infrastructure/Services/JwtService/JwtService.cs`

**İçerik**: 2 class (ana + nested)

1. **JwtService** (Ana servis)
2. **JwtOptions** (NESTED CLASS - satır 103-120)
   - `Issuer`, `Audience`, `Key`, `ExpiryMinutes`, `KeyId`

**NOT**: `JwtOptions` ayrı bir dosya DEĞİL, `JwtService.cs` içinde tanımlı!

---

### 11.2 Application/DTOs Katmanı

#### 📁 `Application/DTOs/Stocks/StocksDto.cs`

**İçerik**: 4 class + 2 nested + 1 record

1. **StockDto** (Ana)
   - **VariantBrief** (NESTED - satır 22-34)
   - **BranchBrief** (NESTED - satır 36-40)
2. **StockCreateDto**
3. **StockUpdateDto**
4. **StockFilter** (record)

---

#### 📁 `Application/DTOs/Stocks/StockVariantDetailByStoreDto.cs`

**İçerik**: 1 class + 2 nested

1. **StockVariantDetailByStoreDto** (Ana)
   - **BranchBlock** (NESTED - satır 19-26)
   - **StockChip** (NESTED - satır 28-34)

---

#### 📁 `Application/DTOs/ProductVariant/ProductVariantDto.cs`

**İçerik**: 4 class + 1 nested + 1 record

1. **ProductVariantDto** (Ana)
   - **ProductTypeBrief** (NESTED - satır 41-54)
2. **ProductVariantCreateDto**
3. **ProductVariantUpdateDto**
4. **ProductVariantFilter** (record)

---

#### 📁 `Application/DTOs/ProductType/ProductTypeDto.cs`

**İçerik**: 4 class + 1 nested + 1 record

1. **ProductTypeDto** (Ana)
   - **CategoryBrief** (NESTED - satır 19-23)
2. **ProductTypeCreateDto**
3. **ProductTypeUpdateDto**
4. **ProductTypeFilter** (record)

---

#### 📁 `Application/DTOs/Branches/BranchDto.cs`

**İçerik**: 4 class + 1 nested + 1 record

1. **BranchDto** (Ana)
   - **StoreBrief** (NESTED - satır 37-44)
2. **BranchCreateDto**
3. **BranchUpdateDto**
4. **BranchFilter** (record)

---

#### 📁 `Application/DTOs/Auth/PasswordCheckRequestDto.cs`

**UYARI**: Bu dosyada **3 ayrı class** var (hepsi aynı seviyede, nested değil!)

1. **PasswordCheckRequestDto** (satır 9-15)
2. **PasswordCheckResultDto** (satır 17-22)
3. **RegisterValidationResultDto** (satır 24-29)

---

### 11.3 Application/Common Katmanı

#### 📁 `Application/Common/ApiResult.cs`

**İçerik**: 2 class (nested değil, aynı dosyada!)

1. **ApiResult<T>** (satır 9-36)
2. **PagedResult<TItem>** (satır 37-43)

---

### 11.4 Özet Tablo

| Dosya | Ana Class Sayısı | Nested Class Sayısı | Toplam |
|-------|------------------|---------------------|--------|
| `JwtService/JwtService.cs` | 1 (JwtService) | 1 (JwtOptions) | 2 |
| `DTOs/Stocks/StocksDto.cs` | 4 | 2 (VariantBrief, BranchBrief) | 6 |
| `DTOs/Stocks/StockVariantDetailByStoreDto.cs` | 1 | 2 (BranchBlock, StockChip) | 3 |
| `DTOs/ProductVariant/ProductVariantDto.cs` | 4 | 1 (ProductTypeBrief) | 5 |
| `DTOs/ProductType/ProductTypeDto.cs` | 4 | 1 (CategoryBrief) | 5 |
| `DTOs/Branches/BranchDto.cs` | 4 | 1 (StoreBrief) | 5 |
| `DTOs/Auth/PasswordCheckRequestDto.cs` | 3 (ayrı) | 0 | 3 |
| `Common/ApiResult.cs` | 2 (ayrı) | 0 | 2 |
| **TOPLAM** | **23** | **8** | **31** |

---

### 11.5 Önemli Notlar

1. **JwtOptions**, `JwtService.cs` içinde tanımlı bir nested class'tır. Ayrı bir `JwtOptions.cs` dosyası YOKTUR!

2. **PasswordCheckRequestDto.cs** dosyasında 3 ayrı class var ama bunlar nested DEĞİL, aynı namespace'de aynı dosyada tanımlanmış.

3. **ApiResult.cs** dosyasında `ApiResult<T>` ve `PagedResult<TItem>` ayrı class'lar ama nested DEĞİL.

4. **DTO nested class'ları** genellikle "Brief" (özet) veya "Block" (blok) olarak adlandırılır ve parent class'ın içinde tanımlıdır.

5. **Tüm nested class'lar `sealed`** olarak işaretlenmiştir (immutability ve performans için).

---

## 15. KNOWN ISSUES VE TUTARSIZLIKLAR

### 15.1 Entity Property Tutarsızlıkları

#### 15.1.1 Banks Entity
- **Sorun**: `CreatedAt` property'si YOKTUR, sadece `UpdatedAt` mevcuttur.
- **Etki**: Oluşturulma tarihi takip edilemez.
- **Öneri**: `CreatedAt` property'si eklenmeli veya dokümante edilmelidir.

#### 15.1.2 PaymentMethods Entity
- **Sorun**: `CreatedAt` ve `UpdatedAt` property'leri YOKTUR.
- **Etki**: Oluşturulma ve güncellenme tarihleri takip edilemez.
- **Öneri**: Her iki property de eklenmeli veya dokümante edilmelidir.

#### 15.1.3 BankTransactions Entity
- **Durum**: Soft delete YOKTUR (IsDeleted, DeletedAt, DeletedBy yok).
- **Açıklama**: Bu MANTIKLIDIR çünkü audit trail (denetim izi) için kalıcı tutulmalıdır.

#### 15.1.4 Limits Entity
- **Durum**: Soft delete YOKTUR.
- **Açıklama**: Bu MANTIKLIDIR çünkü limitler dinamik olarak eklenip silinebilir ve audit gerektirmez.

#### 15.1.5 LifecycleActions Entity
- **Durum**: Soft delete ve CreatedAt/UpdatedAt YOKTUR.
- **Açıklama**: Bu referans tablo olduğu için mantıklı olabilir, ancak standartlaştırma açısından eklenebilir.

---

### 15.2 Interface İsim Tutarsızlığı

- **ICustomerService** (Application/Interfaces/Services/ICustomerService.cs) → Doğrusu **ICustomersService** olmalı (çoğul, diğer servislerle uyumlu).

---

### 15.3 Code Quality İyileştirmeleri

#### 15.3.1 Double Await Pattern (Düzeltildi ✅)

**Önceki Durum**: Bazı controller'larda aynı servis metodu iki kez çağrılıyordu:
- `SalesController.GetPagedAsync` ve `GetById`
- `PurchaseController.GetPagedAsync` ve `GetById`
- `CustomersController.GetPagedAsync`
- `ProductLifecyclesController` (3 metod)
- `LifecycleActionsController` (5 metod)

**Düzeltme**: Tüm controller'larda değişkene atayıp tekrar kullanma pattern'ine geçildi.

#### 15.3.2 IProductVariantService Namespace Hatası

**Dosya**: `KuyumStokApi.Application/Interfaces/Services/IProductVariantService.cs`
- **Satır 2**: `using KuyumStokApi.Application.DTOs.ProductVariant.KuyumStokApi.Application.DTOs.ProductVariants;`
- **Sorun**: Yanlış using statement (iki kez namespace yazılmış)
- **Çözüm**: `using KuyumStokApi.Application.DTOs.ProductVariants;` olmalı

#### 15.3.3 ProductVariantController Namespace Hatası

**Dosya**: `KuyumStokApi.API/Controllers/ProductVariantController.cs`
- **Satır 1**: Aynı namespace hatası mevcut

---

### 15.4 Entity Property Matris Tablosu

| Entity | Id | CreatedAt | UpdatedAt | IsDeleted | DeletedAt | DeletedBy | IsActive | Notlar |
|--------|----|----------|-----------|-----------|-----------|-----------|----------|--------|
| **Users** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| **Roles** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| **Stores** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| **Branches** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| **ProductCategories** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| **ProductTypes** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| **ProductVariants** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | IsFavorite var |
| **Stocks** | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | Fiziksel silme olmamalı |
| **Customers** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | NationalId var |
| **PaymentMethods** | ✅ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | **Tutarsızlık** |
| **Banks** | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ | **Tutarsızlık** |
| **ThermalPrinters** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| **MonthlyTargets** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| **Purchases** | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | Transaction header |
| **PurchaseDetails** | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | Transaction line |
| **Sales** | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | Transaction header |
| **SaleDetails** | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | Transaction line |
| **SalePayments** | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | Transaction line |
| **ProductLifecycles** | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | Timestamp var (audit) |
| **LifecycleActions** | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Referans tablo |
| **Limits** | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | Dinamik limitler |
| **BankTransactions** | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | Audit için kalıcı |

**Açıklamalar**:
- ✅: Property mevcut
- ❌: Property yok (bazı durumlarda mantıklı)
- **Tutarsızlık**: Standart olmalı ama eksik

---

**🎯 Bu dokümantasyon, projeyi başka bir AI modeline veya geliştiriciye anlatmak için EKSİKSİZ bir rehber niteliğindedir. Tüm nested class'lar, aynı dosyada tanımlanmış class'lar ve ilişkiler detaylı olarak açıklanmıştır.**

