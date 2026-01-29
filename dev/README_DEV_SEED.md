## Dev Seed (Profit/Loss)

Bu doküman, Development ortamında **Profit/Loss** endpoint'ine anlamlı veri üretmek için kullanılan dev seed akışını anlatır.

### Ön Koşullar
- .NET SDK

### Çalıştırma
PowerShell:
`powershell -ExecutionPolicy Bypass -File .\dev\seed-profit-loss.ps1`

### Gating
Seed sadece şu koşullarda çalışır:
- `ASPNETCORE_ENVIRONMENT=Development`
- `DEV_SEED_ENABLE=true`

Script bu değişkenleri otomatik ayarlar. İstemiyorsanız:
- `DEV_SEED_ENABLE=false` olarak set edin.

### Ne Eklenir?
- **Sadece transactional data**: Purchase + Sale (+ detayları).
- Gerekliyse **tek bir Stock** kaydı (`DEV-PL-001` barkodlu).
- Mevcut master data (Store/Branch/ProductVariant/PaymentMethod) **re-use** edilir.

### İdempotency
Seed, aynı veriyi tekrar eklememek için `DEV-PL-001` barkodunu ve ilgili sale/purchase detaylarını kontrol eder. Script'i tekrar çalıştırmak güvenlidir.

### Manuel Doğrulama (Kâr/Zarar)
1. `purchasePrice=1000` olacak şekilde bir stok oluştur (veya mevcut stoku kullan).
2. Aynı stok için `soldPrice=1200` satış satırı oluştur → kâr +200.
3. Aynı stok için `soldPrice=900` satış satırı oluştur → zarar 100.
4. Alış kaydı olmayan bir `stock_id` ile satış satırı oluştur → rapora dahil edilmemeli.
5. İptal edilen satış/satış satırları → rapora dahil edilmemeli.

Beklenen toplamlar:
- `totalProfit = 200`
- `totalLoss = 100`
- `netTotal = 100`
