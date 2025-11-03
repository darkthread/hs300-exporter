# 使用 .NET 9 SDK 作為建置環境
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 複製專案檔並還原相依套件 (利用快取機制，csproj 檔沒變動時，不需要重複還原)
COPY hs300-exporter.csproj .
RUN dotnet restore

# 專案只有一個 Program.cs 檔案
# 有很多檔案時，可使用 COPY . . 指令並配合 .dockerignore 檔案排除不要複製的檔案
COPY Program.cs .
# --no-restore 跳過前面已做過的 Package 還原步驟，加速建置
# /p:PublishSingleFile=false 不打包成單一檔，執行前不需解壓，啟動速度較快
# /p:PublishTrimmed=false 不進行程式碼修剪，加快建置減少出錯風險
RUN dotnet publish -c Release -o /app/publish \
    --no-restore \
    /p:PublishSingleFile=false \
    /p:PublishTrimmed=false

# 使用 .NET 9 Runtime 作為執行環境（較小的 Image）
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# 複製建置輸出
COPY --from=build /app/publish .

# 宣告使用 Port (Metadata 性質，不影響編譯或執行)
EXPOSE 9999

# 執行應用程式
ENTRYPOINT ["dotnet", "hs300-exporter.dll"]

# 建置與執行
# docker build -t hs300-exporter .
# docker save -o hs300-exporter.tar hs300-exporter
# scp .\hs300-exporter.tar user@remotehost:/path/to/
# docker run -d -p 9999:9999 --name hs300-exporter hs300-exporter
