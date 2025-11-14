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
RUN dotnet publish "KuyumStokApi.API/KuyumStokApi.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ----------------------
# RUNTIME STAGE
# ----------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "KuyumStokApi.API.dll"]
