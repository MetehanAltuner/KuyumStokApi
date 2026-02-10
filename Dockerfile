# ----------------------
# BUILD STAGE
# ----------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Önce sadece csproj dosyalarını kopyala (restore cache için)
COPY ["KuyumStokApi.API/KuyumStokApi.API.csproj", "KuyumStokApi.API/"]
COPY ["KuyumStokApi.Application/KuyumStokApi.Application.csproj", "KuyumStokApi.Application/"]
COPY ["KuyumStokApi.Domain/KuyumStokApi.Domain.csproj", "KuyumStokApi.Domain/"]
COPY ["KuyumStokApi.Infrastructure/KuyumStokApi.Infrastructure.csproj", "KuyumStokApi.Infrastructure/"]
COPY ["KuyumStokApi.Persistence/KuyumStokApi.Persistence.csproj", "KuyumStokApi.Persistence/"]

RUN dotnet restore "KuyumStokApi.API/KuyumStokApi.API.csproj"

# Şimdi tüm solution'ı kopyala
COPY . .

# Publish (framework-dependent)
# .NET SDK Web projelerinde wwwroot klasörü otomatik olarak publish'e dahil edilir
RUN dotnet publish "KuyumStokApi.API/KuyumStokApi.API.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:CopyOutputSymbols=false

# wwwroot klasörünün kopyalandığını doğrula (opsiyonel - debug için)
RUN ls -la /app/publish/wwwroot/ || echo "wwwroot klasörü bulunamadı!"

# ----------------------
# RUNTIME STAGE
# ----------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Temel araçları ve Türkiye saat dilimi için tzdata'yı yükle - minimal boyut için cache temizle
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    curl \
    iputils-ping \
    tzdata \
    && ln -snf /usr/share/zoneinfo/Europe/Istanbul /etc/localtime && echo "Europe/Istanbul" > /etc/timezone \
    && rm -rf /var/lib/apt/lists/*

# Uygulama içinde varsayılan saat dilimini Türkiye saati (Europe/Istanbul) yap
ENV TZ=Europe/Istanbul

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "KuyumStokApi.API.dll"]
