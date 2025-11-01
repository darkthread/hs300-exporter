using Prometheus;
using System.Diagnostics;
using TPLinkSmartDevices.Devices;
using Microsoft.Extensions.Configuration;
using System.Dynamic;

// 建立設定系統 (優先順序: CLI 參數 > 環境變數 > 預設值)
var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables(prefix: "HS300_") // 使用 HS300_ 前綴的環境變數
    .AddCommandLine(args) // CLI 參數
    .Build();

// 讀取設定，提供預設值
var host = configuration["DeviceIP"] ?? throw new ArgumentNullException("DeviceIP", "請提供 HS300 裝置的 IP 位址");

var metricServer = new MetricServer(port: 9999); // 指定 Exposure Port
metricServer.Start();

Metrics.SuppressDefaultMetrics(); // 停用預設指標

// 建立自訂指標
var myGauge = Metrics.CreateGauge("hs300_stats", "HS300 插座電流量", new GaugeConfiguration
{
    LabelNames = new[] { "type" }
});

// 以背景執行方式更新指標
await Task.Run(async () =>
{
    var rand = new Random();
    while (true)
    {
        double[] data = new double[6];
        long waitMs = 10_000;
        try
        {
            Console.Write(DateTime.Now.ToString("MM-dd HH:mm:ss "));
            var sw = Stopwatch.StartNew();
            var hs300 = new TPLinkSmartStrip(host);
            for (int i = 0; i < 6; i++)
            {
                var powerData = hs300.ReadRealtimePowerData(i + 1);
                data[i] = powerData.Power;
                myGauge.WithLabels($"插座{i + 1}").Set(powerData.Power);
            }
            sw.Stop();
            waitMs = Math.Max(waitMs - (int)sw.ElapsedMilliseconds, 0);
            Console.WriteLine($"Read data: {string.Join(",", data)} ({sw.ElapsedMilliseconds:n0}ms)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"錯誤: {ex.Message}");
        }
        await Task.Delay(TimeSpan.FromMilliseconds(waitMs));
    }
});

// 等待外部結束程式
// metricServer.Stop();