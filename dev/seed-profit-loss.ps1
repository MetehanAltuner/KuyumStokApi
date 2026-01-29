$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$launchSettingsPath = Join-Path $repoRoot "KuyumStokApi.API\Properties\launchSettings.json"

# Development seed env
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DEV_SEED_ENABLE = "true"
$env:DEV_SEED_USERNAME = "mete"
$env:DEV_SEED_PASSWORD = "Ceren.0199"

# DB baglanti bilgisi appsettings.json'dan okunur

# Determine base URL from launchSettings.json
$launch = Get-Content -Raw $launchSettingsPath | ConvertFrom-Json
$urls = @()
foreach ($profile in $launch.profiles.PSObject.Properties.Value) {
    if ($profile.applicationUrl) {
        $urls += ($profile.applicationUrl -split ";")
    }
}
$baseUrl = ($urls | Where-Object { $_ -match "^http://" } | Select-Object -First 1)
if (-not $baseUrl) {
    $baseUrl = ($urls | Select-Object -First 1)
}
if (-not $baseUrl) {
    throw "launchSettings.json içinde applicationUrl bulunamadı."
}
$baseUrl = $baseUrl.TrimEnd("/")

Write-Host "API kontrol ediliyor: $baseUrl"

$apiAlreadyRunning = $false
$port = ([Uri]$baseUrl).Port
try {
    $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($listener) {
        $apiAlreadyRunning = $true
    }
} catch {
    $apiAlreadyRunning = $false
}

if (-not $apiAlreadyRunning) {
    Start-Process "dotnet" -ArgumentList "run --project `"$repoRoot\KuyumStokApi.API`"" -WorkingDirectory $repoRoot -NoNewWindow
    Write-Host "API baslatiliyor: $baseUrl"

    # Wait for API to be reachable
    $maxTries = 45
    for ($i = 1; $i -le $maxTries; $i++) {
        try {
            $status = curl.exe -s -o $null -w "%{http_code}" --connect-timeout 2 --max-time 2 "$baseUrl/api/Dashboard/profit-loss"
            if ($status -match "^[1-5][0-9][0-9]$" -and $status -ne "000") {
                break
            }
        } catch {
            # ignore
        }
        Start-Sleep -Seconds 2
        if ($i -eq $maxTries) {
            throw "API baslatilamadi (timeout)."
        }
    }
} else {
    Write-Host "API zaten calisiyor."
}

# Login
$loginBody = @{ username = $env:DEV_SEED_USERNAME; password = $env:DEV_SEED_PASSWORD } | ConvertTo-Json -Compress
$loginTemp = New-TemporaryFile
Set-Content -Path $loginTemp -Value $loginBody -Encoding UTF8
$loginRaw = $null
for ($i = 1; $i -le 10; $i++) {
    $loginRaw = curl.exe -s -X POST "$baseUrl/api/Auth/login" -H "Content-Type: application/json" --data-binary "@$loginTemp"
    if ($loginRaw) { break }
    Start-Sleep -Seconds 2
}
if (-not $loginRaw) {
    throw "Login yaniti bos."
}

Remove-Item $loginTemp -ErrorAction SilentlyContinue

$loginJson = $loginRaw | ConvertFrom-Json
$token = $null
if ($loginJson.token) { $token = $loginJson.token }
elseif ($loginJson.accessToken) { $token = $loginJson.accessToken }
elseif ($loginJson.jwtToken) { $token = $loginJson.jwtToken }
elseif ($loginJson.data -and $loginJson.data.accessToken) { $token = $loginJson.data.accessToken }
elseif ($loginJson.data -and $loginJson.data.token) { $token = $loginJson.data.token }

if (-not $token) {
    throw "Token bulunamadi. Login yaniti: $loginRaw"
}

# Call Profit/Loss
$plRaw = curl.exe -s -H "Authorization: Bearer $token" "$baseUrl/api/Dashboard/profit-loss"
if (-not $plRaw) {
    throw "Profit/Loss yaniti bos."
}

$plJson = $plRaw | ConvertFrom-Json
$plJson | ConvertTo-Json -Depth 10
