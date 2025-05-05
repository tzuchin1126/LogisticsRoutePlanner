# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 複製專案檔並還原相依套件
COPY *.csproj ./
RUN dotnet restore

# 複製全部程式碼並建置
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# 啟動你的應用程式
ENTRYPOINT ["dotnet", "LogisticsRoutePlanner.dll"]
