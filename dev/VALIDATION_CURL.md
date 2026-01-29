# Pozitif Sayi Validasyon Ornekleri (PowerShell)

> Not: URL ve token degerlerini ortamina gore guncelle.

## Stok Girisi (POST /api/Stocks)

### purchasePrice = 0 (HATA)
```
curl.exe -X POST "https://localhost:5001/api/Stocks" ^
  -H "Content-Type: application/json" ^
  -H "Authorization: Bearer <token>" ^
  -d "{ \"productVariantId\": 1, \"branchId\": 1, \"quantity\": 1, \"totalWeightGram\": 1, \"purchasePrice\": 0 }"
```

### purchasePrice = -1 (HATA)
```
curl.exe -X POST "https://localhost:5001/api/Stocks" ^
  -H "Content-Type: application/json" ^
  -H "Authorization: Bearer <token>" ^
  -d "{ \"productVariantId\": 1, \"branchId\": 1, \"quantity\": 1, \"totalWeightGram\": 1, \"purchasePrice\": -1 }"
```

### purchasePrice yok (HATA)
```
curl.exe -X POST "https://localhost:5001/api/Stocks" ^
  -H "Content-Type: application/json" ^
  -H "Authorization: Bearer <token>" ^
  -d "{ \"productVariantId\": 1, \"branchId\": 1, \"quantity\": 1, \"totalWeightGram\": 1 }"
```

### Basarili ornek (>0)
```
curl.exe -X POST "https://localhost:5001/api/Stocks" ^
  -H "Content-Type: application/json" ^
  -H "Authorization: Bearer <token>" ^
  -d "{ \"productVariantId\": 1, \"branchId\": 1, \"quantity\": 1, \"totalWeightGram\": 1, \"purchasePrice\": 10.5 }"
```
