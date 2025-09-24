# ---------- RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Non-root kullanýcý oluþtur (ID: 64198 örnek)
RUN adduser --disabled-password --gecos "" --uid 64198 appuser \
 && chown -R appuser:appuser /app
USER appuser

# Uygulamanýn dinlediði adres/port
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Proje dosyalarýný kopyala (restore cache için önce csproj’lar)
COPY ["KuyumStokApi.API/KuyumStokApi.API.csproj", "KuyumStokApi.API/"]
COPY ["KuyumStokApi.Application/KuyumStokApi.Application.csproj", "KuyumStokApi.Application/"]
COPY ["KuyumStokApi.Domain/KuyumStokApi.Domain.csproj", "KuyumStokApi.Domain/"]
COPY ["KuyumStokApi.Infrastructure/KuyumStokApi.Infrastructure.csproj", "KuyumStokApi.Infrastructure/"]
COPY ["KuyumStokApi.Persistence/KuyumStokApi.Persistence.csproj", "KuyumStokApi.Persistence/"]

RUN dotnet restore "KuyumStokApi.API/KuyumStokApi.API.csproj"

# Kaynaklarýn tamamýný kopyala ve build et
COPY . .
WORKDIR /src/KuyumStokApi.API
RUN dotnet build "KuyumStokApi.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ---------- PUBLISH ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "KuyumStokApi.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ---------- FINAL ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KuyumStokApi.API.dll"]
