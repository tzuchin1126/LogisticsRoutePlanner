using System.Text.Json;
using System.Web;

namespace LogisticsRoutePlanner.Helpers
{
    public static class GeocodingHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _googleApiKey;

        // 初始化方法，用來從設定讀取金鑰
        public static void Initialize(IConfiguration config)
        {
            _googleApiKey = config["GoogleMaps:ApiKey"];
            if (string.IsNullOrEmpty(_googleApiKey))
            {
                Console.WriteLine("錯誤：Google Maps API 金鑰未設定！");
            }
        }

        // 支援 Nominatim 為主、Google Maps 為備援，並加上重試機制
        public static async Task<(double lat, double lon)?> GetLatLonAsync(string address, int maxRetries = 2)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                Console.WriteLine("警告：傳入空地址");
                return null;
            }

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // 嘗試使用 Nominatim
                    var encodedAddress = HttpUtility.UrlEncode(address);
                    var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&addressdetails=1&limit=1";
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "LogisticsRoutePlanner/1.0");

                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            var root = doc.RootElement;
                            if (root.GetArrayLength() > 0)
                            {
                                var firstResult = root[0];
                                if (firstResult.TryGetProperty("lat", out var latElement) &&
                                    firstResult.TryGetProperty("lon", out var lonElement) &&
                                    double.TryParse(latElement.GetString(), out double lat) &&
                                    double.TryParse(lonElement.GetString(), out double lon))
                                {
                                    Console.WriteLine($"[Nominatim] 地址 [{address}] 的經緯度: ({lat}, {lon})");
                                    return (lat, lon);
                                }
                            }
                        }
                    }
                    Console.WriteLine($"[Nominatim] 第 {attempt} 次嘗試失敗，進入下一步驟");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Nominatim] 嘗試失敗：{ex.Message}");
                }

                // 加一點延遲避免頻繁請求
                await Task.Delay(500);
            }

            // Nominatim 都失敗 → 使用 Google Maps 備援
            try
            {
                if (string.IsNullOrWhiteSpace(_googleApiKey))
                {
                    Console.WriteLine("錯誤：Google API 金鑰尚未初始化");
                    return null;
                }

                Console.WriteLine("[Google] 嘗試使用 Google Maps Geocoding API 備援");

                var encodedAddress = HttpUtility.UrlEncode(address);
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_googleApiKey}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Google] API 回應錯誤: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("status", out var status) && status.GetString() != "OK")
                    {
                        Console.WriteLine($"[Google] 回應狀態非 OK：{status.GetString()}");
                        return null;
                    }

                    var location = root.GetProperty("results")[0].GetProperty("geometry").GetProperty("location");
                    var lat = location.GetProperty("lat").GetDouble();
                    var lon = location.GetProperty("lng").GetDouble();
                    Console.WriteLine($"[Google] 地址 [{address}] 的經緯度: ({lat}, {lon})");
                    return (lat, lon);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Google] 錯誤：{ex.Message}");
                return null;
            }
        }
    }
}
