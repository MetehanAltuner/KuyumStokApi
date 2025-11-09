# KUYUMSTOKAPI - KAPSAMLI PROJE DOKÜMANTASYONU

> **Tarih**: 9 Kasım 2025  
> **Proje**: Kuyum (Kuyumcu) Stok Yönetim Sistemi API  
> **Teknoloji**: ASP.NET Core 8.0 Web API, Entity Framework Core, PostgreSQL  
> **Mimari**: Clean Architecture (Layered Architecture)

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
- JwtService (Token üretimi)
- LifecycleActionsService
- LimitsService
- PaymentMethodsService
- ProductCategoryService
- ProductLifecycleService
- ProductTypeService
- ProductVariantService
- PurchasesService
- RolesService
- SalesService
- StocksService
- StoresService
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

**İlişkiler**:
- **Role** (Many-to-One): Bir kullanıcının bir rolü vardır
- **Branch** (Many-to-One): Bir kullanıcı bir şubeye atanır
- **Purchases** (One-to-Many): Kullanıcı birden fazla alış işlemi yapabilir
- **Sales** (One-to-Many): Kullanıcı birden fazla satış işlemi yapabilir
- **ProductLifecycles** (One-to-Many): Kullanıcı ürün hareketleri kaydeder

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
| **IsActive** | bool | Aktif mi? |
| **IsDeleted** | bool | Soft delete |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |

**İlişkiler**:
- **Purchases** (One-to-Many): Alışlarda kullanılan ödeme yöntemleri
- **Sales** (One-to-Many): Satışlarda kullanılan ödeme yöntemleri

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
| **IsActive** | bool | Aktif mi? |
| **IsDeleted** | bool | Soft delete |
| **DeletedAt** | DateTime? | Silinme tarihi |
| **DeletedBy** | int? | Silen kullanıcı ID |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **BankTransactions** (One-to-Many): Banka işlemleri

**Amaç**:
POS ile yapılan ödemelerde komisyon takibi için kullanılır.

---

### 4.12 BankTransactions (Banka İşlemleri)

**Tablo**: `bank_transactions`  
**Amaç**: POS ile yapılan satışlarda komisyon ve banka bilgilerini kaydeder.

**Alanlar**:

| Alan | Tip | Açıklama |
|------|-----|----------|
| **Id** | int | Birincil anahtar |
| **SaleId** | int? | Hangi satış (Foreign Key → Sales) |
| **BankId** | int? | Hangi banka (Foreign Key → Banks) |
| **CommissionRate** | decimal? | Komisyon oranı (%2.5 gibi) |
| **ExpectedAmount** | decimal? | Beklenen tutar (komisyon sonrası) |
| **Status** | string? | Durum (pending, completed, failed) |
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Sale** (Many-to-One): Bir satışa bağlıdır
- **Bank** (Many-to-One): Bir bankaya bağlıdır

**İş Mantığı**:
- Satış yapılırken POS seçilirse banka işlemi oluşturulur
- Komisyon oranı kaydedilir
- Beklenen tutar = Satış Tutarı × (1 - Komisyon Oranı)

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
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Sale** (Many-to-One): Bir fişe bağlıdır
- **Stock** (Many-to-One): Bir stok kalemine bağlıdır

**İş Mantığı**:
```
Toplam Gelir = Quantity × SoldPrice
Kâr = (SoldPrice - PurchasePrice) × Quantity
```

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
| **CreatedAt** | DateTime? | Oluşturulma tarihi |
| **UpdatedAt** | DateTime? | Güncellenme tarihi |

**İlişkiler**:
- **Branch** (Many-to-One): Bir şubeye bağlıdır
- **ProductVariant** (Many-to-One): Bir varyanta bağlıdır

**İş Mantığı**:
- Şube yöneticisi varyant bazında limit belirler
- Stok minimum eşiğin altına düşerse sistem uyarı verebilir
- Maksimum eşik aşılırsa aşırı stok uyarısı

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

### 5.7 Diğer Servisler (Özet)

| Servis | Sorumluluk |
|--------|------------|
| **BanksService** | Banka CRUD işlemleri |
| **BranchesService** | Şube CRUD, şube listesi |
| **CustomersService** | Müşteri CRUD, arama |
| **LifecycleActionsService** | Aksiyon tipi CRUD |
| **LimitsService** | Stok limiti CRUD, eşik kontrolleri |
| **PaymentMethodsService** | Ödeme yöntemi CRUD |
| **ProductCategoryService** | Kategori CRUD |
| **ProductLifecycleService** | Lifecycle kayıtları, geçmiş sorgulama |
| **ProductTypeService** | Tip CRUD |
| **ProductVariantService** | Varyant CRUD, arama |
| **RolesService** | Rol CRUD |
| **StoresService** | Mağaza CRUD |

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

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | `/api/purchase` | Alış listesi (sayfalı) |
| GET | `/api/purchase/{id}` | Alış fişi detayı |
| POST | `/api/purchase` | Yeni alış oluştur |

---

### 6.5 AuthController

**Endpoint'ler**:

| Metod | Endpoint | Açıklama | Auth Gerekli? |
|-------|----------|----------|---------------|
| POST | `/api/auth/register` | Yeni kullanıcı kaydet | Hayır |
| POST | `/api/auth/login` | Kullanıcı girişi | Hayır |
| POST | `/api/auth/validate-password` | Parola gücü kontrolü | Hayır |
| POST | `/api/auth/validate-register` | Kayıt validasyonu | Hayır |

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

## 7. DTO'LAR (Data Transfer Objects)

### 7.1 DTO Amacı

DTO'lar, API ile client arasında veri alışverişi için kullanılan hafif veri modelleridir:
- Entity'lerin tüm özelliklerini expose etmez (güvenlik)
- Validation attribute'ları içerir
- Nested objeler ile ilişkili verileri birleştirir

### 7.2 Örnek DTO'lar

#### StockDto
```csharp
public class StockDto
{
    public int Id { get; set; }
    public int? Quantity { get; set; }
    public string Barcode { get; set; }
    public string? QrCode { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public decimal TotalWeight { get; set; } // Calculated: Gram × Quantity
    
    public BranchBrief? Branch { get; set; }
    public VariantBrief? ProductVariant { get; set; }
    
    public class BranchBrief
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
    }
    
    public class VariantBrief
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Ayar { get; set; }
        public string? Color { get; set; }
        public string? Brand { get; set; }
        public decimal? Gram { get; set; }
        public int? ProductTypeId { get; set; }
        public string? ProductTypeName { get; set; }
        public string? CategoryName { get; set; }
    }
}
```

#### SaleCreateDto
```csharp
public class SaleCreateDto
{
    public int BranchId { get; set; }
    public int? UserId { get; set; }
    public int? CustomerId { get; set; }
    
    // Inline customer creation
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerNationalId { get; set; }
    
    public int? PaymentMethodId { get; set; }
    
    // Optional POS transaction
    public int? BankId { get; set; }
    public decimal? CommissionRate { get; set; }
    public decimal? ExpectedAmount { get; set; }
    
    public List<SaleItemDto> Items { get; set; }
}

public class SaleItemDto
{
    public int StockId { get; set; }
    public int Quantity { get; set; }
    public decimal? SoldPrice { get; set; }
}
```

---

## 8. GÜVENLİK VE KİMLİK DOĞRULAMA

### 8.1 Parola Güvenliği

**Algoritma**: SHA-256 + Salt + Pepper + Iterations

**Adımlar**:
1. **Salt Üretimi**: 16 byte random değer, Base64'e çevir
2. **Pepper**: appsettings.json'da saklanan sabit değer
3. **Hash Hesaplama**:
   ```
   Input = Salt + Password + Pepper
   Hash = SHA-256(Input)
   For i = 1 to Iterations:
       Hash = SHA-256(Hash)
   ```
4. **Doğrulama**: Sabit zamanlı karşılaştırma (timing attack'a karşı)

**Parola Politikası**:
- Minimum 8 karakter
- En az 1 büyük harf
- En az 1 küçük harf
- En az 1 rakam
- En az 1 özel karakter

### 8.2 JWT Authentication

**Token Yapısı**:
- **Algorithm**: HS256 (HMAC-SHA256)
- **Issuer**: KuyumStokApi
- **Audience**: KuyumStokApiClients
- **Expiration**: 24 saat (ayarlanabilir)
- **Claims**: UserId, Username, Role, BranchId

**Program.cs Yapılandırması**:
```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cfg["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = cfg["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### 8.3 CurrentUser Context

**ICurrentUserContext** interface'i ile aktif kullanıcı bilgilerine erişim:
```csharp
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    string? UserName { get; }
    int? UserId { get; }
    int? BranchId { get; }
}
```

**Kullanım**:
```csharp
public class StocksService : IStocksService
{
    private readonly ICurrentUserContext _user;
    
    public async Task<ApiResult<PagedResult<StockDto>>> GetPagedAsync(StockFilter filter)
    {
        var branchId = filter.BranchId ?? _user.BranchId;
        // Kullanıcı kendi şubesini görür
    }
}
```

---

## 9. ÖNEMLİ ÖZELLİKLER VE DESENLER

### 9.1 ApiResult<T> Standardizasyonu

Tüm API yanıtları standart formatta:
```csharp
public class ApiResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
    public string TraceId { get; set; }
}
```

**Kullanım**:
```csharp
// Success
return ApiResult<StockDto>.Ok(dto, "Stok bulundu", 200);

// Failure
return ApiResult<StockDto>.Fail("Stok bulunamadı", statusCode: 404);
```

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

### 9.3 Transaction Yönetimi

Kritik işlemler transaction ile korunur:
```csharp
using var tx = await _db.Database.BeginTransactionAsync(ct);

try
{
    // Stok düşür
    // Satış kaydı oluştur
    // Lifecycle ekle
    
    await _db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
}
catch
{
    // Rollback otomatik (using scope)
    throw;
}
```

### 9.4 Sayfalama (Pagination)

Büyük listelerde performans için:
```csharp
public class PagedResult<TItem>
{
    public IReadOnlyList<TItem> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
}
```

**Kullanım**:
```csharp
var page = Math.Max(1, filter.Page);
var pageSize = Math.Clamp(filter.PageSize, 1, 200);

var items = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### 9.5 Entity Framework Query Optimization

- **AsNoTracking()**: Read-only sorgularda performans
- **Include()**: Eager loading (ilişkili verileri tek sorguda çek)
- **Select()**: Projection (sadece gerekli kolonları çek)
- **Join**: Manuel join ile daha kontrollü sorgular

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

**Doküman Tarihi**: 9 Kasım 2025  
**Versiyon**: 1.0  
**Hazırlayan**: AI Assistant  
**Proje Sahası**: c:\Users\45868582848\source\repos\KuyumStokApi

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

**🎯 Bu dokümantasyon, projeyi başka bir AI modeline veya geliştiriciye anlatmak için tam bir rehber niteliğindedir.**

