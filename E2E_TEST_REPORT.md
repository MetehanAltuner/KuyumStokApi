# E2E Test Report

## How the app was started
- `dotnet ef database update --project KuyumStokApi.Persistence --startup-project KuyumStokApi.API`
- `dotnet run --project KuyumStokApi.API/KuyumStokApi.API.csproj`

## Base URL
- `http://localhost:5235`

## Login
- Endpoint: `POST /api/Auth/login`
- Request sample:
  - Body:
    - `{"username":"mete","password":"Ceren.****"}`
- Token (masked): `eyJhbG...pK0Htc`

## Seeded IDs selected
- BranchId: `2` (Dev Sube Istanbul)
- ProductVariantId: `3` (Madalyon Kolye Altin)
- CustomerId: `4` (Mehmet Kaya)

## Created transactional IDs
- StockId: `75bf9677-e968-4493-aef3-9e7f2849a5e1`
- StockBarcode (reused for purchase): `STK-002-251208210329823-96F20E`
- SaleId: `23` (SaleLineId: `16`)
- PurchaseId: `12`

## Curl requests executed (token masked)
- `POST /api/Auth/login`
  - `curl -s -X POST "http://localhost:5235/api/Auth/login" -H "Content-Type: application/json" --data-binary @-`
  - Body: `{"username":"mete","password":"Ceren.****"}`
- `GET /api/Stocks` (auth check)
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Stocks?page=1&pageSize=1"`
- `GET /api/Branches`
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Branches?page=1&pageSize=20"`
- `GET /api/ProductVariants`
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/ProductVariants?page=1&pageSize=20"`
- `GET /api/Customers`
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Customers?page=1&pageSize=20"`
- `GET /api/Stocks` (baseline, filtered)
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Stocks?page=1&pageSize=50&BranchId=2&ProductVariantId=3"`
- `POST /api/Stocks` (add 10 / 100g)
  - `curl -s -X POST "http://localhost:5235/api/Stocks" -H "Authorization: Bearer eyJhbG...pK0Htc" -H "Content-Type: application/json" --data-binary @-`
  - Body: `{"productVariantId":3,"branchId":2,"quantity":10,"totalWeightGram":100,"generateQrCode":false}`
- `GET /api/Stocks` (after add 10 / 100g)
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Stocks?page=1&pageSize=50&BranchId=2&ProductVariantId=3"`
- `POST /api/Stocks` (add 5 / 40g)
  - `curl -s -X POST "http://localhost:5235/api/Stocks" -H "Authorization: Bearer eyJhbG...pK0Htc" -H "Content-Type: application/json" --data-binary @-`
  - Body: `{"productVariantId":3,"branchId":2,"quantity":5,"totalWeightGram":40,"generateQrCode":false}`
- `GET /api/Stocks` (after add 5 / 40g)
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Stocks?page=1&pageSize=50&BranchId=2&ProductVariantId=3"`
- `POST /api/Sales` (sale-only unified receipt)
  - `curl -s -X POST "http://localhost:5235/api/Sales" -H "Authorization: Bearer eyJhbG...pK0Htc" -H "Content-Type: application/json" --data-binary @-`
  - Body: `{"mode":1,"branchId":2,"customerId":4,"cash":1000,"eft":0,"pos":0,"saleItems":[{"stockId":"75bf9677-e968-4493-aef3-9e7f2849a5e1","quantity":3,"soldPrice":1000,"totalWeightGram":30}]}`
- `GET /api/Stocks` (after sale)
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Stocks?page=1&pageSize=50&BranchId=2&ProductVariantId=3"`
- `POST /api/Purchases` (purchase)
  - `curl -s -X POST "http://localhost:5235/api/Purchases" -H "Authorization: Bearer eyJhbG...pK0Htc" -H "Content-Type: application/json" --data-binary @-`
  - Body: `{"userId":1,"branchId":2,"customerId":4,"paymentMethodId":null,"items":[{"productVariantId":3,"branchId":2,"barcode":"STK-002-251208210329823-96F20E","quantity":2,"purchasePrice":500,"totalWeightGram":25}]}`
- `GET /api/Stocks` (after purchase)
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Stocks?page=1&pageSize=50&BranchId=2&ProductVariantId=3"`
- `GET /api/Sales`
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Sales?page=1&pageSize=50"`
- `GET /api/Sales/{lineId}`
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Sales/16"`
- `GET /api/Purchases`
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Purchases?page=1&pageSize=50"`
- `GET /api/Purchases/{id}`
  - `curl -s -H "Authorization: Bearer eyJhbG...pK0Htc" "http://localhost:5235/api/Purchases/12"`

## Verification table
| Operation | Expected Qty | Actual Qty | Expected TotalGram | Actual TotalGram | PASS/FAIL |
|---|---:|---:|---:|---:|---|
| Stock add (+10 / +100) | 40 | 40 | 619.43 | 619.43 | PASS |
| Stock add (+5 / +40) | 45 | 45 | 659.43 | 659.43 | PASS |
| Sale (-3 / -30) | 42 | 42 | 629.43 | 629.43 | PASS |
| Purchase (+2 / +25) | 44 | 44 | 654.43 | 654.43 | PASS |

## Issues found
- None.
