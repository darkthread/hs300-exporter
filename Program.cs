using Prometheus;
using System.Diagnostics;
using TPLinkSmartDevices.Devices;

// 讀取設定，提供預設值
var host = Environment.GetEnvironmentVariable("HS300_DeviceIP") ?? throw new ArgumentNullException("HS300_DeviceIP", "請提供 HS300 裝置的 IP 位址");

var metricServer = new MetricServer(port: 9999); // 指定 Exposure Port
metricServer.Start();

Metrics.SuppressDefaultMetrics(); // 停用預設指標

// 建立自訂指標
var myGauge = Metrics.CreateGauge("hs300_stats", "HS300 插座耗電量(w)", new GaugeConfiguration
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
        try
        {
            if (DateTime.Now.Second % 10 == 0)
            {
                Console.Write(DateTime.Now.ToString("MM-dd HH:mm:ss "));
                Console.Out.Flush();

                var sw = Stopwatch.StartNew();
                
                // 設定 30 秒逾時
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var readTask = Task.Run(() =>
                {
                    var hs300 = new TPLinkSmartStrip(host);
                    for (int i = 0; i < 6; i++)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        var powerData = hs300.ReadRealtimePowerData(i + 1);
                        data[i] = powerData.Power;
                        myGauge.WithLabels($"插座{i + 1}").Set(powerData.Power);
                    }
                }, cts.Token);

                await readTask.WaitAsync(cts.Token);
                
                sw.Stop();
                Console.WriteLine($"Read data: {string.Join(",", data)} ({sw.ElapsedMilliseconds:n0}ms)");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("錯誤: 讀取逾時");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"錯誤: {ex.Message}");
        }
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
});
