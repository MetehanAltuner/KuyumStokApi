# Dashboard Servis Teknik Dokümantasyonu

## İçindekiler
1. [Genel Bakış](#genel-bakış)
2. [ML.NET ve İstatistiksel Yöntemler](#mlnet-ve-istatistiksel-yöntemler)
3. [DashboardService Metodları](#dashboardservice-metodları)
4. [AnomalyDetectionService](#anomalydetectionservice)
5. [WorkloadEstimationService](#workloadestimationservice)
6. [SignalR Real-Time Güncellemeler](#signalr-real-time-güncellemeler)

---

## Genel Bakış

Dashboard servisi, kuyum stok yönetim sisteminin analitik ve tahminsel özelliklerini sağlayan merkezi bir bileşendir. Sistem, **istatistiksel yöntemler** ve **ML.NET altyapısı** (gelecekte kullanım için hazır) kullanarak gerçek zamanlı sayaçlar, anomali tespiti, iş yükü tahmini ve kapsamlı raporlama sunar.

### Teknoloji Stack
- **.NET 8.0**
- **Entity Framework Core 9.0** (PostgreSQL)
- **ML.NET 3.0.1** (paket yüklü, gelecekte kullanım için hazır)
- **Microsoft.ML.TimeSeries 3.0.1** (paket yüklü, gelecekte kullanım için hazır)
- **SignalR** (real-time güncellemeler)
- **Z-Score İstatistiksel Yöntemi** (anomali tespiti)
- **Lineer Regresyon** (iş yükü tahmini)
- **Moving Average** (iş yükü tahmini)

---

## ML.NET ve İstatistiksel Yöntemler

### Mevcut Durum

**ML.NET paketleri yüklü ancak şu anda aktif kullanılmıyor.** Sistem şu anda **istatistiksel yöntemler** ile çalışıyor:

#### 1. Z-Score Tabanlı Anomali Tespiti
- **Kullanılan Servis**: `AnomalyDetectionService`
- **Yöntem**: İstatistiksel Z-score analizi
- **Formül**: `Z = (X - μ) / σ`
  - `X`: Mevcut değer
  - `μ`: Ortalama (mean)
  - `σ`: Standart sapma (standard deviation)
- **Eşik Değeri**: Default 2.0 (|Z| > 2.0 → anomali)
- **Kullanım Alanları**:
  - Satış anomali tespiti
  - Stok seviyesi anomali tespiti

#### 2. Lineer Regresyon (İş Yükü Tahmini)
- **Kullanılan Servis**: `WorkloadEstimationService`
- **Yöntem**: En küçük kareler yöntemi (Least Squares)
- **Formül**: `y = a + b*x`
  - `a`: Y-intercept
  - `b`: Slope (eğim)
  - `x`: Zaman (gün numarası)
  - `y`: Tahmin edilen işlem sayısı
- **Hesaplama**:
  ```
  b = (n*Σxy - Σx*Σy) / (n*Σx² - (Σx)²)
  a = (Σy - b*Σx) / n
  ```
- **Kullanım**: Bir sonraki gün için işlem sayısı tahmini

#### 3. Moving Average (Hareketli Ortalama)
- **Kullanılan Servis**: `WorkloadEstimationService`
- **Yöntem**: Son N günün aritmetik ortalaması
- **Pencere Boyutu**: Default 7 gün
- **Formül**: `MA = (X₁ + X₂ + ... + Xₙ) / n`
- **Kullanım**: Yetersiz veri durumunda fallback yöntem

### ML.NET Gelecek Kullanım Potansiyeli

ML.NET paketleri yüklü ve aşağıdaki senaryolarda kullanılabilir:

1. **Time Series Forecasting** (`Microsoft.ML.TimeSeries`)
   - Satış tahmini için SARIMA, Prophet benzeri modeller
   - Stok seviyesi tahmini
   - Mevsimsel trend analizi

2. **Anomaly Detection** (`Microsoft.ML.TimeSeries`)
   - Spike Detection (ani yükseliş/düşüş)
   - Change Point Detection (değişim noktası tespiti)
   - Z-score yerine daha gelişmiş algoritmalar

3. **Regression Models**
   - Çoklu değişkenli regresyon (satış tahmini için)
   - Decision Tree Regression
   - Fast Tree Regression

---

## DashboardService Metodları

### 1. GetSummaryAsync

**Endpoint**: `GET /api/dashboard/summary`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Tüm parametresiz dashboard verilerini tek seferde döndürür (birleşik endpoint - broadcast yapmaz)

#### Teknik Detaylar

**Veri Toplama**: Metod, aşağıdaki parametresiz dashboard metodlarını **paralel olarak** çağırır:
- `GetWeeklyTrendAsync`
- `GetMonthlyTargetAsync`
- `GetRemindersAsync`
- `GetWorkloadEstimateAsync`
- `GetBranchComparisonAsync`
- `GetRiskScoreLegendAsync`

**Paralel İşleme**:
```csharp
var weeklyTrendTask = GetWeeklyTrendAsync(ct);
var monthlyTargetTask = GetMonthlyTargetAsync(ct);
var remindersTask = GetRemindersAsync(ct);
var workloadEstimateTask = GetWorkloadEstimateAsync(ct);
var branchComparisonTask = GetBranchComparisonAsync(ct);
var riskScoreLegendTask = GetRiskScoreLegendAsync(ct);
```

**Hata Yönetimi**: Her task bağımsız olarak işlenir. Bir task başarısız olsa bile diğerleri etkilenmez. Hata durumunda log yazılır ve ilgili alan `null` veya boş liste olarak kalır.

**Dönüş Tipi**: `ApiResult<DashboardSummaryDto>`

**DashboardSummaryDto Yapısı**:
```csharp
public sealed class DashboardSummaryDto
{
    public SalesTrendReportDto? WeeklyTrend { get; set; }
    public MonthlyTargetDto? MonthlyTarget { get; set; }
    public List<ReminderDto> Reminders { get; set; } = new();
    public WorkloadEstimateDto? WorkloadEstimate { get; set; }
    public BranchComparisonDto? BranchComparison { get; set; }
    public RiskScoreLegendDto? RiskScoreLegend { get; set; }
}
```

**Önemli Notlar**:
- Bu endpoint **broadcast yapmaz** (SignalR event göndermez)
- Broadcast yapan endpoint'ler: `live-counters`, `daily-summary`, `anomalies`
- Frontend'de tek bir API çağrısı ile tüm parametresiz dashboard verilerini almak için kullanılır
- Paralel işleme sayesinde performans optimize edilmiştir

---

### 2. GetLiveCountersAsync

**Endpoint**: `GET /api/dashboard/live-counters`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Gerçek zamanlı canlı sayaçlar (son satış zamanı, bugünkü işlem sayısı, stok senkronizasyonu)

#### Teknik Detaylar

**Veri Toplama**:
```csharp
// Son satış zamanı
var lastSale = await _db.Sales
    .Where(s => s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value))
    .OrderByDescending(s => s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue)
    .Select(s => s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue)
    .FirstOrDefaultAsync(ct);

// Bugünkü işlem sayısı (satış + alış)
var todaySalesCount = await _db.Sales
    .Where(s => s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value) &&
                (s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue) >= todayStart)
    .CountAsync(ct);

var todayPurchasesCount = await _db.Purchases
    .Where(p => p.BranchId != null && scope.AccessibleBranchIds.Contains(p.BranchId.Value) &&
                (p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue) >= todayStart)
    .CountAsync(ct);

// Stok senkronizasyon zamanı
var lastStockSync = await _db.Stocks
    .Where(s => s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value))
    .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt ?? DateTime.MinValue)
    .Select(s => s.UpdatedAt ?? s.CreatedAt ?? DateTime.MinValue)
    .FirstOrDefaultAsync(ct);
```

**Hesaplamalar**:
- `MinutesSinceLastSale`: Son satıştan bu yana geçen dakika (UTC bazlı)
- `TodayTransactionCount`: Bugünkü toplam işlem sayısı (satış + alış)
- `LastStockSyncTime`: Son stok güncelleme zamanı (UTC)
- `LastStockSyncFormatted`: Formatlanmış metin ("HH:mm itibarıyla senkronize")

**SignalR Broadcast**: `LiveCountersUpdated` event'i ile tüm client'lara gönderilir.

**Dönüş Tipi**: `ApiResult<LiveCountersDto>`

---

### 3. GetWeeklyTrendAsync

**Endpoint**: `GET /api/dashboard/weekly-trend`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Haftalık trend grafik verisi

#### Teknik Detaylar

**Delegasyon**: `IReportsService.GetSalesTrendAsync` metoduna delegasyon yapılır.

**Parametreler**:
- `ReportTrendGranularity.Weekly`: Haftalık granularite
- `ReportDateRange`: Son 7 günlük aralık

**Dönüş Tipi**: `ApiResult<SalesTrendReportDto>`

---

### 4. GetDailySummaryAsync

**Endpoint**: `GET /api/dashboard/daily-summary?date={DateTime?}`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Gün sonu raporu (satış, kâr, en çok satan ürün, kritik stok)

#### Teknik Detaylar

**Veri Toplama**:
```csharp
// Satış toplamı (JOIN ile)
var totalSales = await (from d in _db.SaleDetails
                       join s in _db.Sales on d.SaleId equals s.Id
                       where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                       let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                       where created >= dayStart && created <= dayEnd
                       select (d.Quantity ?? 0) * (d.SoldPrice ?? 0m))
    .SumAsync(ct);

// Alış maliyeti (JOIN ile)
var totalCost = await (from d in _db.PurchaseDetails
                      join p in _db.Purchases on d.PurchaseId equals p.Id
                      where p.BranchId != null && scope.AccessibleBranchIds.Contains(p.BranchId.Value)
                      let created = p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue
                      where created >= dayStart && created <= dayEnd
                      select (d.Quantity ?? 0) * (d.PurchasePrice ?? 0m))
    .SumAsync(ct);
```

**Hesaplamalar**:
- `TotalSales`: Günlük toplam satış tutarı
- `TotalCost`: Günlük toplam alış maliyeti
- `TotalProfit`: `TotalSales - TotalCost`
- `ProfitPercentage`: `(TotalProfit / TotalSales) * 100` (2 ondalık basamak)
- `TopSellingProduct`: En çok satan ürün (GROUP BY + ORDER BY)
- `CriticalStockCount`: Kritik stok seviyesindeki ürün sayısı (JOIN ile Limits tablosu)

**Durum Mesajı**:
- `ProfitPercentage >= 25`: "Gün başarılı geçti! 🎉"
- `ProfitPercentage >= 15`: "Gün iyi geçti."
- Diğer: "Gün normal seviyede."

**SignalR Broadcast**: `DailySummaryUpdated` event'i ile tüm client'lara gönderilir.

**Dönüş Tipi**: `ApiResult<DailySummaryDto>`

---

### 5. GetAnomaliesAsync

**Endpoint**: `GET /api/dashboard/anomalies`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Anomali algılama (satış düşüşü, stok seviyesi)

#### Teknik Detaylar

**Veri Toplama**:
```csharp
// Son 30 günlük günlük satış verileri
var dailySales = await (from d in _db.SaleDetails
                       join s in _db.Sales on d.SaleId equals s.Id
                       where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                       let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                       where created >= thirtyDaysAgo && created <= now
                       let day = new DateTime(created.Year, created.Month, created.Day, 0, 0, 0, DateTimeKind.Utc)
                       group d by day into g
                       select new
                       {
                           Day = g.Key,
                           Revenue = g.Sum(x => (x.Quantity ?? 0) * (x.SoldPrice ?? 0m))
                       })
    .ToListAsync(ct);
```

**Anomali Tespiti**:
1. **Satış Anomalisi**:
   - `AnomalyDetectionService.DetectSalesAnomaly()` kullanılır
   - Z-score hesaplanır
   - `Z < -2`: "HighSalesDrop" (Risk Score: `|Z| * 30`, max 100)
   - `Z > 2`: "HighSalesIncrease" (Risk Score: 20)
   - Diğer: "NormalSales" (Risk Score: 30)

2. **Stok Anomalisi**:
   - Kritik stoklar için `AnomalyDetectionService.DetectStockAnomaly()` kullanılır
   - Her kritik stok için Z-score hesaplanır
   - Ortalama risk skoru: `|Z| * 50` (max 100)
   - "LowStockLevel" tipinde anomali oluşturulur

**Dönüş Tipi**: `ApiResult<List<AnomalyDto>>`

**SignalR Broadcast**: `AnomaliesUpdated` event'i ile tüm client'lara gönderilir.

---

### 6. GetMonthlyTargetAsync

**Endpoint**: `GET /api/dashboard/monthly-target`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Aylık satış hedefi (veritabanından okur)

#### Teknik Detaylar

**Veri Toplama**:
```csharp
// Mevcut ay satış toplamı
var currentAmount = await (from d in _db.SaleDetails
                          join s in _db.Sales on d.SaleId equals s.Id
                          where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                          let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                          where created >= monthStart && created <= monthEnd
                          select (d.Quantity ?? 0) * (d.SoldPrice ?? 0m))
    .SumAsync(ct);

// Hedef tutar - MonthlyTargets tablosundan
var monthlyTarget = await _db.MonthlyTargets
    .Where(mt => mt.StoreId == scope.StoreId.Value 
              && mt.Year == now.Year 
              && mt.Month == now.Month
              && !mt.IsDeleted)
    .FirstOrDefaultAsync(ct);
```

**Hesaplamalar**:
- `TargetAmount`: Veritabanından okunan hedef (yoksa default: 100.000 TL)
- `CurrentAmount`: Mevcut ay satış toplamı
- `ProgressPercentage`: `(CurrentAmount / TargetAmount) * 100` (2 ondalık basamak)
- `RemainingAmount`: `Max(0, TargetAmount - CurrentAmount)`

**Durum Mesajı**:
- `ProgressPercentage >= 75`: "Harika gidiyorsun! Sadece ₺{remainingAmount:N0} kaldı! 🚀"
- `ProgressPercentage >= 50`: "İyi gidiyorsun! ₺{remainingAmount:N0} daha hedefe ulaşmak için kaldı."
- Diğer: "Hedefe ulaşmak için ₺{remainingAmount:N0} daha gerekiyor."

**Dönüş Tipi**: `ApiResult<MonthlyTargetDto>`

---

### 7. GetRemindersAsync

**Endpoint**: `GET /api/dashboard/reminders`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Hatırlatıcılar ve ajanda (kritik stok, satılmayan ürünler)

#### Teknik Detaylar

**Kritik Stok Kontrolü**:
```csharp
var criticalStocks = await (from s in _db.Stocks
                           where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                           join pv in _db.ProductVariants on s.ProductVariantId equals pv.Id
                           join l in _db.Limits on new { s.BranchId, s.ProductVariantId } 
                               equals new { BranchId = l.BranchId, ProductVariantId = l.ProductVariantId } into jl
                           from l in jl.DefaultIfEmpty()
                           where l != null && (s.Quantity ?? 0) < (l.MinThreshold ?? 0)
                           select new { pv.Name, s.Quantity, s.ProductVariantId, l.MinThreshold })
    .ToListAsync(ct);
```

**Hesaplamalar**:
1. **Kritik Stok Tükenme Tahmini**:
   - Son 7 günlük ortalama günlük satış hızı hesaplanır
   - `daysUntilDepletion = Ceiling(Quantity / avgDailySales)`
   - Priority:
     - `daysUntilDepletion <= 3`: Priority 5 (Kritik)
     - `daysUntilDepletion <= 7`: Priority 4 (Yüksek)
     - Diğer: Priority 3 (Orta)

2. **Uzun Süre Satılmayan Ürünler**:
   - Son 30 günde satış yapılmamış ürünler tespit edilir
   - En fazla 5 ürün listelenir
   - Priority: 2 (Düşük)

3. **Kritik Stok Özeti**:
   - Toplam kritik stok sayısı
   - Priority: 4 (Yüksek)

**Dönüş Tipi**: `ApiResult<List<ReminderDto>>`

---

### 8. GetTopProductsAsync

**Endpoint**: `GET /api/dashboard/top-products?limit=5&period=week`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: En çok satan ürünler

#### Teknik Detaylar

**Parametreler**:
- `limit`: Döndürülecek ürün sayısı (default: 5)
- `period`: Zaman aralığı
  - `"week"`: Son 7 gün (default)
  - `"month"`: Son 30 gün
  - `"all"`: Tüm zamanlar

**Veri Toplama**:
```csharp
var topProducts = await (from d in _db.SaleDetails
                        join s in _db.Sales on d.SaleId equals s.Id
                        where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                        let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                        where created >= periodStart && created <= now
                        join st in _db.Stocks on d.StockId equals st.Id into js
                        from st in js.DefaultIfEmpty()
                        join pv in _db.ProductVariants on st.ProductVariantId equals pv.Id into jpv
                        from pv in jpv.DefaultIfEmpty()
                        group d by new
                        {
                            ProductName = pv != null ? pv.Name : st != null ? st.Barcode : "Tanımsız"
                        } into g
                        orderby g.Sum(x => x.Quantity ?? 0) descending
                        select new TopProductDto
                        {
                            ProductName = g.Key.ProductName,
                            QuantitySold = g.Sum(x => x.Quantity ?? 0),
                            Revenue = Math.Round(g.Sum(x => (x.Quantity ?? 0) * (x.SoldPrice ?? 0m)), 2)
                        })
    .Take(limit)
    .ToListAsync(ct);
```

**Hesaplamalar**:
- `ProductName`: Ürün adı (ProductVariant.Name → Stock.Barcode → "Tanımsız")
- `QuantitySold`: Toplam satılan miktar (GROUP BY + SUM)
- `Revenue`: Toplam gelir (2 ondalık basamak)

**Dönüş Tipi**: `ApiResult<List<TopProductDto>>`

---

### 9. GetWorkloadEstimateAsync

**Endpoint**: `GET /api/dashboard/workload-estimate`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Günlük iş yükü tahmini (hibrit yaklaşım: mutlak eşikler + yüzde bazlı)

#### Teknik Detaylar

**Veri Toplama**:
```csharp
// Son 30 günlük günlük işlem sayıları (satış + alış)
var dailyTransactions = await (from s in _db.Sales
                             where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                             let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                             where created >= thirtyDaysAgo && created <= now
                             let day = new DateTime(created.Year, created.Month, created.Day, 0, 0, 0, DateTimeKind.Utc)
                             group s by day into g
                             select new { Day = g.Key, Count = g.Count() })
    .ToListAsync(ct);

var dailyPurchases = await (from p in _db.Purchases
                           where p.BranchId != null && scope.AccessibleBranchIds.Contains(p.BranchId.Value)
                           let created = p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue
                           where created >= thirtyDaysAgo && created <= now
                           let day = new DateTime(created.Year, created.Month, created.Day, 0, 0, 0, DateTimeKind.Utc)
                           group p by day into g
                           select new { Day = g.Key, Count = g.Count() })
    .ToListAsync(ct);

// Birleştir
var combined = dailyTransactions
    .Concat(dailyPurchases)
    .GroupBy(x => x.Day)
    .Select(g => new { Day = g.Key, Count = g.Sum(x => x.Count) })
    .ToList();
```

**Tahmin Yöntemi**:
1. **Lineer Regresyon** (7+ gün veri varsa):
   ```csharp
   estimatedCount = _workloadEstimationService.EstimateWithLinearRegression(dailyCounts);
   ```

2. **Moving Average** (7 günden az veri varsa):
   ```csharp
   estimatedCount = _workloadEstimationService.EstimateWithMovingAverage(dailyCounts, dailyCounts.Count);
   ```

**Hesaplamalar**:
- `EstimatedTransactionCount`: Tahmin edilen işlem sayısı
- `OverallAvg`: Genel ortalama işlem sayısı
- `WorkloadPercentage`: `(EstimatedCount / OverallAvg) * 100` (sınırsız)
- `IntensityLevel`: Hibrit yaklaşım ile belirlenir
  - **Mutlak Eşikler**:
    - `estimatedCount <= 5`: "Düşük"
    - `estimatedCount <= 15`: "Orta"
    - `estimatedCount > 15`: "Yüksek"
  - **Yüzde Bazlı** (fallback):
    - `workloadPercentage >= 200`: "Yüksek"
    - `workloadPercentage >= 150`: "Orta"
    - Diğer: "Düşük"

**Durum Mesajı**:
- "Düşük": "Rahat bir gün olacak."
- "Orta": "Normal bir gün olacak."
- "Yüksek": "Yoğun bir gün olacak — kasa ve personel hazır bulunsun! Tahmini işlem hacmi: {estimatedCount} işlem"

**Dönüş Tipi**: `ApiResult<WorkloadEstimateDto>`

---

### 10. GetBranchComparisonAsync

**Endpoint**: `GET /api/dashboard/branch-comparison`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Şube karşılaştırması

#### Teknik Detaylar

**Veri Toplama** (Her şube için):
```csharp
// Satış toplamı (son 7 gün)
var totalSales = await (from d in _db.SaleDetails
                       join s in _db.Sales on d.SaleId equals s.Id
                       where s.BranchId == branchId
                       let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                       where created >= last7Days && created <= now
                       select (d.Quantity ?? 0) * (d.SoldPrice ?? 0m))
    .SumAsync(ct);

// Alış maliyeti (son 7 gün)
var totalCost = await (from d in _db.PurchaseDetails
                      join p in _db.Purchases on d.PurchaseId equals p.Id
                      where p.BranchId == branchId
                      let created = p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue
                      where created >= last7Days && created <= now
                      select (d.Quantity ?? 0) * (d.PurchasePrice ?? 0m))
    .SumAsync(ct);

// POS oranı
var posPayments = await (from sp in _db.SalePayments
                        join s in _db.Sales on sp.SaleId equals s.Id
                        join pm in _db.PaymentMethods on sp.PaymentMethodId equals pm.Id
                        where s.BranchId == branchId 
                           && pm.Name != null 
                           && (pm.Name.Contains("Kart") || pm.Name.Contains("POS") || pm.Name.Contains("Kredi"))
                        select sp.Amount)
    .SumAsync(ct);
```

**Hesaplamalar**:
- `TotalSales`: Son 7 günlük toplam satış
- `TotalCost`: Son 7 günlük toplam maliyet
- `TotalProfit`: `TotalSales - TotalCost`
- `ReceiptCount`: Toplam fiş sayısı
- `PosPercentage`: `(PosPayments / TotalPayments) * 100`
- `CriticalStockCount`: Kritik stok sayısı
- `Trend`: Son 7 gün vs önceki 7 gün karşılaştırması
  - `change > 5%`: "up"
  - `change < -5%`: "down"
  - Diğer: "stable"

**Sıralama**: Satış toplamına göre azalan sırada

**Dönüş Tipi**: `ApiResult<BranchComparisonDto>`

---

### 11. GetProfitLossAsync

**Endpoint**: `GET /api/dashboard/profit-loss?period=week`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Kar-Zarar tablosu

#### Teknik Detaylar

**Parametreler**:
- `period`: Zaman aralığı
  - `"day"`: Son 7 gün (günlük gruplama)
  - `"week"`: Son 4 hafta (haftalık gruplama)
  - `"month"`: Son 6 ay (aylık gruplama)

**Veri Toplama**:
```csharp
// Satış verileri - önce memory'ye çek, sonra grupla
var salesRaw = await (from d in _db.SaleDetails
                     join s in _db.Sales on d.SaleId equals s.Id
                     where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                     let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                     where created >= periodStart && created <= now
                     select new
                     {
                         Created = created,
                         Quantity = d.Quantity ?? 0,
                         SoldPrice = d.SoldPrice ?? 0m
                     })
    .ToListAsync(ct);

var salesData = salesRaw
    .GroupBy(x => periodKeySelector(x.Created))
    .Select(g => new
    {
        Period = g.Key,
        Sales = g.Sum(x => x.Quantity * x.SoldPrice)
    })
    .ToList();
```

**Hesaplamalar**:
- `Sales`: Dönem bazlı satış toplamı
- `Cost`: Dönem bazlı maliyet toplamı
- `Profit`: `Sales - Cost`
- `ProfitPercentage`: `(Profit / Sales) * 100`
- `Trend`: Önceki dönem ile karşılaştırma
  - `change > 5%`: "up"
  - `change < -5%`: "down"
  - Diğer: "stable"

**Toplamlar**:
- `TotalSales`: Tüm dönemlerin toplamı
- `TotalCost`: Tüm dönemlerin toplamı
- `TotalProfit`: `TotalSales - TotalCost`
- `TotalProfitPercentage`: `(TotalProfit / TotalSales) * 100`

**Dönüş Tipi**: `ApiResult<ProfitLossDto>`

---

### 12. GetRiskScoreLegendAsync

**Endpoint**: `GET /api/dashboard/risk-score-legend`  
**Yetkilendirme**: JWT Token gerekli  
**Açıklama**: Risk skor sözlüğü (0-100 aralığı)

#### Teknik Detaylar

**Risk Skor Aralıkları**:
1. **0-20**: Düşük Risk
   - Açıklama: "Düşük risk seviyesi. Normal işleyiş."
   - Renk: `#10b981` (Yeşil)

2. **21-50**: Orta Risk
   - Açıklama: "Orta risk seviyesi. Dikkatli takip edilmeli."
   - Renk: `#f59e0b` (Sarı)

3. **51-80**: Yüksek Risk
   - Açıklama: "Yüksek risk seviyesi. Acil müdahale gerekebilir."
   - Renk: `#f97316` (Turuncu)

4. **81-100**: Kritik Risk
   - Açıklama: "Kritik risk seviyesi. Hemen müdahale edilmeli."
   - Renk: `#ef4444` (Kırmızı)

**Dönüş Tipi**: `ApiResult<RiskScoreLegendDto>`

---

## AnomalyDetectionService

### Genel Bakış

Z-score tabanlı istatistiksel anomali tespiti yapan servis. ML.NET kullanmaz, saf istatistiksel yöntem kullanır.

### Metodlar

#### 1. DetectAnomaly

**Açıklama**: Genel amaçlı Z-score tabanlı anomali tespiti

**Parametreler**:
- `values`: Analiz edilecek değerler listesi (`List<decimal>`)
- `currentValue`: Mevcut değer (`decimal`)
- `threshold`: Z-score eşik değeri (default: 2.0)

**Algoritma**:
1. Ortalama hesapla: `μ = Average(values)`
2. Varyans hesapla: `σ² = Average((x - μ)²)`
3. Standart sapma hesapla: `σ = √σ²`
4. Z-score hesapla: `Z = (currentValue - μ) / σ`
5. Anomali kontrolü: `|Z| > threshold`

**Dönüş Değeri**: `AnomalyResult`
- `IsAnomaly`: Anomali var mı?
- `ZScore`: Hesaplanan Z-score değeri
- `Mean`: Ortalama
- `StandardDeviation`: Standart sapma
- `Message`: Açıklayıcı mesaj

**Özel Durumlar**:
- Veri yoksa: `IsAnomaly = false`, `Message = "Yeterli veri yok"`
- Standart sapma 0 ise: `IsAnomaly = false`, `Message = "Standart sapma sıfır - tüm değerler aynı"`

---

#### 2. DetectSalesAnomaly

**Açıklama**: Satış verileri için özelleştirilmiş anomali tespiti

**Parametreler**:
- `dailySales`: Günlük satış değerleri listesi (`List<decimal>`)
- `todaySales`: Bugünkü satış değeri (`decimal`)

**Kullanım**:
```csharp
var result = _anomalyDetectionService.DetectSalesAnomaly(dailySales, todaySales);
```

**Eşik Değeri**: 2.0 (sabit)

**Dönüş Değeri**: `AnomalyResult`

---

#### 3. DetectStockAnomaly

**Açıklama**: Stok seviyesi için anomali tespiti (kritik seviye kontrolü)

**Parametreler**:
- `currentQuantity`: Mevcut stok miktarı (`int`)
- `minThreshold`: Minimum eşik değeri (`int`)

**Algoritma**:
1. `minThreshold <= 0` ise: `IsAnomaly = false`, `Message = "Minimum eşik değeri tanımlı değil"`
2. `currentQuantity < minThreshold` ise: Anomali var
3. Risk skoru: `Min(100, (1 - (currentQuantity / minThreshold)) * 100)`

**Dönüş Değeri**: `AnomalyResult`
- `IsAnomaly`: `currentQuantity < minThreshold`
- `ZScore`: Anomali varsa `-2.0`, yoksa `0`
- `Mean`: `minThreshold`
- `StandardDeviation`: `0`
- `Message`: Açıklayıcı mesaj (risk skoru dahil)

---

## WorkloadEstimationService

### Genel Bakış

İş yükü tahmini için istatistiksel yöntemler kullanan servis. ML.NET paketleri yüklü ancak şu anda kullanılmıyor. Gelecekte ML.NET Time Series modelleri eklenebilir.

### Metodlar

#### 1. EstimateWithMovingAverage

**Açıklama**: Hareketli ortalama ile iş yükü tahmini

**Parametreler**:
- `dailyCounts`: Günlük işlem sayıları listesi (`List<int>`)
- `windowSize`: Moving average penceresi (default: 7 gün)

**Algoritma**:
1. Son N günü al: `recentDays = dailyCounts.TakeLast(windowSize)`
2. Ortalama hesapla: `average = Average(recentDays)`
3. Yuvarla: `Round(average)`

**Dönüş Değeri**: `int` (tahmin edilen işlem sayısı)

**Özel Durumlar**:
- Veri yoksa: `0` döner
- Hata durumunda: Son değeri döner (`dailyCounts.LastOrDefault()`)

---

#### 2. EstimateWithLinearRegression

**Açıklama**: Basit lineer regresyon ile iş yükü tahmini (trend bazlı)

**Parametreler**:
- `dailyCounts`: Günlük işlem sayıları listesi (en eski → en yeni) (`List<int>`)

**Algoritma**:
1. Veri hazırlama:
   - `x = [1, 2, 3, ..., n]` (gün numaraları)
   - `y = dailyCounts` (işlem sayıları)

2. Lineer regresyon katsayıları:
   ```
   b = (n*Σxy - Σx*Σy) / (n*Σx² - (Σx)²)
   a = (Σy - b*Σx) / n
   ```

3. Bir sonraki gün için tahmin:
   ```
   predicted = a + b * (n + 1)
   ```

4. Negatif değer kontrolü: `Max(0, Round(predicted))`

**Dönüş Değeri**: `int` (tahmin edilen işlem sayısı)

**Özel Durumlar**:
- Veri < 2 ise: Son değeri döner
- Hata durumunda: `EstimateWithMovingAverage()` fallback

---

#### 3. CalculateWorkloadPercentage

**Açıklama**: İş yükü yüzdesi hesaplama

**Parametreler**:
- `estimatedCount`: Tahmin edilen işlem sayısı (`int`)
- `averageCount`: Ortalama işlem sayısı (`double`)

**Algoritma**:
```
percentage = (estimatedCount / averageCount) * 100
```

**Dönüş Değeri**: `int` (sınırsız yüzde, 100'e sınırlanmaz)

**Özel Durumlar**:
- `averageCount <= 0`: `0` döner

---

#### 4. DetermineIntensityLevel

**Açıklama**: Yoğunluk seviyesi belirleme (Hibrit yaklaşım: Mutlak eşikler + Yüzde bazlı kontrol)

**Parametreler**:
- `estimatedCount`: Tahmin edilen işlem sayısı (`int`)
- `workloadPercentage`: İş yükü yüzdesi (`int`)

**Algoritma** (Öncelik sırası):
1. **Mutlak Eşikler** (öncelikli):
   - `estimatedCount <= 5`: "Düşük"
   - `estimatedCount <= 15`: "Orta"
   - `estimatedCount > 15`: "Yüksek"

2. **Yüzde Bazlı** (fallback - mutlak eşikler belirsizse):
   - `workloadPercentage >= 200`: "Yüksek"
   - `workloadPercentage >= 150`: "Orta"
   - Diğer: "Düşük"

**Dönüş Değeri**: `string` ("Düşük", "Orta", "Yüksek")

---

#### 5. GenerateMessage

**Açıklama**: Durum mesajı oluşturma

**Parametreler**:
- `intensityLevel`: Yoğunluk seviyesi (`string`)
- `estimatedCount`: Tahmin edilen işlem sayısı (`int`)

**Mesajlar**:
- `"Düşük"`: "Rahat bir gün olacak."
- `"Orta"`: "Normal bir gün olacak."
- `"Yüksek"`: "Yoğun bir gün olacak — kasa ve personel hazır bulunsun! Tahmini işlem hacmi: {estimatedCount} işlem"
- Diğer: "İş yükü tahmini hazırlandı."

**Dönüş Değeri**: `string`

---

## SignalR Real-Time Güncellemeler

### Hub Yapılandırması

**Hub URL**: `/api/hubs/dashboard`  
**Hub Sınıfı**: `DashboardHub`  
**Namespace**: `KuyumStokApi.Application.Hubs`

**Yapılandırma** (`Program.cs`):
```csharp
builder.Services.AddSignalR();

// CORS yapılandırması (SignalR için gerekli)
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Hub mapping
app.MapHub<DashboardHub>("/api/hubs/dashboard")
   .RequireCors("SignalRCorsPolicy");
```

**JWT Authentication**: SignalR bağlantıları JWT token ile doğrulanır. Token query string'den (`access_token`) veya Authorization header'dan alınabilir.

### Broadcast Event'leri

#### 1. LiveCountersUpdated

**Event Adı**: `"LiveCountersUpdated"`  
**Payload**: `LiveCountersDto`  
**Tetiklendiği Yer**: `GetLiveCountersAsync()` metodunda her API çağrısında

**Kullanım** (Frontend):
```typescript
connection.on('LiveCountersUpdated', (data: LiveCountersDto) => {
  // data: { MinutesSinceLastSale, TodayTransactionCount, LastStockSyncTime, LastStockSyncFormatted }
  updateLiveCountersUI(data);
});
```

---

#### 2. DailySummaryUpdated

**Event Adı**: `"DailySummaryUpdated"`  
**Payload**: `DailySummaryDto`  
**Tetiklendiği Yer**: `GetDailySummaryAsync()` metodunda her API çağrısında

**Kullanım** (Frontend):
```typescript
connection.on('DailySummaryUpdated', (data: DailySummaryDto) => {
  // data: { Date, TotalSales, TotalProfit, ProfitPercentage, TopSellingProduct, CriticalStockCount, StatusMessage }
  updateDailySummaryUI(data);
});
```

---

#### 3. AnomaliesUpdated

**Event Adı**: `"AnomaliesUpdated"`  
**Payload**: `List<AnomalyDto>`  
**Tetiklendiği Yer**: `GetAnomaliesAsync()` metodunda her API çağrısında

**Kullanım** (Frontend):
```typescript
connection.on('AnomaliesUpdated', (anomalies: AnomalyDto[]) => {
  // anomalies: [{ Type, Description, RiskScore }, ...]
  updateAnomaliesUI(anomalies);
});
```

---

### Broadcast Metodları

Tüm broadcast metodları `DashboardService` içinde private olarak tanımlanmıştır:

```csharp
private async Task BroadcastLiveCountersAsync(LiveCountersDto dto, CancellationToken ct)
{
    if (_hubContext == null) return;
    
    try
    {
        await _hubContext.Clients.All.SendAsync("LiveCountersUpdated", dto, ct);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Live counters broadcast edilirken hata oluştu");
    }
}
```

**Özellikler**:
- `_hubContext` null ise sessizce atlanır (opsiyonel)
- Hata durumunda log yazılır, exception fırlatılmaz
- `CancellationToken` desteklenir

---

## Yetkilendirme ve Kapsam Yönetimi

### ResolveScopeAsync Metodu

Tüm dashboard metodları kullanıcı yetkisine göre veri kapsamını belirler:

**Rol Bazlı Erişim**:
1. **Owner/Admin Rolü**:
   - Mağaza bazlı: Tüm şubeler (aynı mağazaya ait)
   - Mağaza yoksa: Tüm şubeler

2. **Manager Rolü**:
   - Sadece kendi şubesi

3. **Diğer Roller**:
   - Sadece kendi şubesi

**ReportScope Sınıfı**:
```csharp
private sealed class ReportScope
{
    public int UserId { get; set; }
    public int? BranchId { get; set; }
    public int? StoreId { get; set; }
    public string? RoleName { get; set; }
    public List<int> AccessibleBranchIds { get; set; } = new();
}
```

---

## Performans Optimizasyonları

### 1. AsNoTracking Kullanımı

Tüm sorgularda `AsNoTracking()` kullanılır (read-only işlemler için):
```csharp
var sales = await _db.Sales.AsNoTracking()
    .Where(...)
    .ToListAsync(ct);
```

### 2. Memory'ye Çekme (Büyük Veri Setleri)

Bazı sorgularda veri önce memory'ye çekilir, sonra LINQ ile gruplanır:
```csharp
var salesRaw = await (from d in _db.SaleDetails
                     ...
                     select new { ... })
    .ToListAsync(ct);

var salesData = salesRaw
    .GroupBy(x => periodKeySelector(x.Created))
    .Select(g => new { ... })
    .ToList();
```

### 3. Decimal Yuvarlama

Tüm decimal değerler 2 ondalık basamağa yuvarlanır:
```csharp
totalSales = Math.Round(totalSales, 2);
```

---

## Hata Yönetimi

### Try-Catch Blokları

Tüm metodlar try-catch ile sarılmıştır:
```csharp
try
{
    // İş mantığı
    return ApiResult<T>.Ok(dto, "Başarılı", 200);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Hata mesajı");
    return ApiResult<T>.Fail("Kullanıcı dostu hata mesajı", statusCode: 500);
}
```

### Logging

- **Error**: Exception'lar için
- **Warning**: SignalR broadcast hataları için
- **Info**: (Şu anda kullanılmıyor)

---

## Sonuç

Dashboard servisi, kuyum stok yönetim sisteminin analitik omurgasını oluşturur. Sistem şu anda **istatistiksel yöntemler** (Z-score, lineer regresyon, moving average) kullanırken, **ML.NET altyapısı** gelecekteki gelişmeler için hazırdır. Real-time güncellemeler SignalR ile sağlanır ve tüm metodlar kullanıcı yetkisine göre veri kapsamını otomatik olarak belirler.

---

**Dokümantasyon Versiyonu**: 1.1  
**Son Güncelleme**: 8 Aralık 2025  
**Yazar**: KuyumStokApi Development Team

**Güncellemeler**:
- `GetSummaryAsync` metodu eklendi (birleşik endpoint)
- SignalR hub URL'i güncellendi (`/api/hubs/dashboard`)
- JWT authentication ve CORS yapılandırması eklendi

