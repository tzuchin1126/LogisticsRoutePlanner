# 使用 ASP.NET Core 8.0 執行環境
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# 建置階段 - 使用 .NET 8.0 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

# 指定 .csproj 來建置專案
RUN dotnet publish LogisticsRoutePlanner.csproj -c Release -o /app/publish

# 發行階段
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LogisticsRoutePlanner.dll"]
