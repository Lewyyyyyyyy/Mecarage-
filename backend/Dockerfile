# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project files
COPY backend/*.sln ./
COPY backend/MecaManage.API/MecaManage.API.csproj ./MecaManage.API/
COPY backend/MecaManage.Application/MecaManage.Application.csproj ./MecaManage.Application/
COPY backend/MecaManage.Domain/MecaManage.Domain.csproj ./MecaManage.Domain/
COPY backend/MecaManage.Infrastructure/MecaManage.Infrastructure.csproj ./MecaManage.Infrastructure/

# Restore dependencies
RUN dotnet restore MecaManage.API/MecaManage.API.csproj

# Copy all source code
COPY backend/MecaManage.API ./MecaManage.API/
COPY backend/MecaManage.Application ./MecaManage.Application/
COPY backend/MecaManage.Domain ./MecaManage.Domain/
COPY backend/MecaManage.Infrastructure ./MecaManage.Infrastructure/

# Build
RUN dotnet build -c Release --no-restore

# Publish
RUN dotnet publish MecaManage.API/MecaManage.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "MecaManage.API.dll"]

