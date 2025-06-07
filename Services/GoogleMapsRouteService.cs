// using System.Text.Json;
// using LogisticsRoutePlanner.Models;

// namespace LogisticsRoutePlanner.Services
// {
//     public class GoogleMapsRouteService
//     {
//         private readonly HttpClient _httpClient;
//         private readonly string _apiKey;

//         public GoogleMapsRouteService(HttpClient httpClient, IConfiguration configuration)
//         {
//             _httpClient = httpClient;
//             _apiKey = configuration["GoogleMaps:ApiKey"];
//         }

//         /// <summary>
//         /// 使用 Google Maps Distance Matrix API 獲取兩點間的距離和時間
//         /// </summary>
//         public async Task<DistanceMatrixResponse> GetDistanceMatrixAsync(List<string> origins, List<string> destinations)
//         {
//             var originsParam = string.Join("|", origins.Select(Uri.EscapeDataString));
//             var destinationsParam = string.Join("|", destinations.Select(Uri.EscapeDataString));
            
//             var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?" +
//                      $"origins={originsParam}&destinations={destinationsParam}&" +
//                      $"key={_apiKey}&units=metric&mode=driving&language=zh-TW";

//             try
//             {
//                 var response = await _httpClient.GetStringAsync(url);
//                 var result = JsonSerializer.Deserialize<DistanceMatrixResponse>(response, new JsonSerializerOptions
//                 {
//                     PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
//                 });
                
//                 return result;
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Google Maps Distance Matrix API 錯誤: {ex.Message}");
//                 throw;
//             }
//         }

//         /// <summary>
//         /// 使用貪婪算法配合 Google Maps 距離數據來優化路線
//         /// </summary>
//         public async Task<List<OptimizedDestination>> OptimizeRouteAsync(string originAddress, List<ShipmentDestination> destinations)
//         {
//             if (destinations == null || !destinations.Any())
//                 return new List<OptimizedDestination>();

//             // 準備所有地址
//             var allAddresses = new List<string> { originAddress };
//             allAddresses.AddRange(destinations.Select(d => d.Address));

//             // 獲取距離矩陣
//             var distanceMatrix = await GetDistanceMatrixAsync(allAddresses, allAddresses);
            
//             if (distanceMatrix?.Status != "OK")
//                 throw new Exception($"Google Maps API 回應錯誤: {distanceMatrix?.Status}");

//             // 使用貪婪算法進行路線優化
//             var optimizedRoute = GreedyTspSolver(distanceMatrix, destinations);
            
//             return optimizedRoute;
//         }

//         /// <summary>
//         /// 貪婪算法解決 TSP 問題
//         /// </summary>
//         private List<OptimizedDestination> GreedyTspSolver(DistanceMatrixResponse matrix, List<ShipmentDestination> destinations)
//         {
//             var result = new List<OptimizedDestination>();
//             var unvisited = destinations.Select((dest, index) => new { Destination = dest, Index = index + 1 }).ToList();
//             int currentIndex = 0; // 從起點開始
//             int order = 1;

//             while (unvisited.Any())
//             {
//                 var nearest = unvisited
//                     .Where(u => matrix.Rows[currentIndex].Elements[u.Index].Status == "OK")
//                     .OrderBy(u => matrix.Rows[currentIndex].Elements[u.Index].Distance.Value)
//                     .FirstOrDefault();

//                 if (nearest == null)
//                 {
//                     // 如果找不到可達的點，取第一個未訪問的點
//                     nearest = unvisited.First();
//                 }

//                 var element = matrix.Rows[currentIndex].Elements[nearest.Index];
                
//                 result.Add(new OptimizedDestination
//                 {
//                     Id = nearest.Destination.Id,
//                     SortOrder = order++,
//                     CustomerName = nearest.Destination.CustomerName,
//                     Address = nearest.Destination.Address,
//                     ProductInfo = nearest.Destination.ProductInfo,
//                     Note = nearest.Destination.Note,
//                     SkipReason = nearest.Destination.SkipReason,
//                     Distance = element.Distance?.Text ?? "未知",
//                     Duration = element.Duration?.Text ?? "未知",
//                     DistanceValue = element.Distance?.Value ?? 0,
//                     DurationValue = element.Duration?.Value ?? 0
//                 });

//                 currentIndex = nearest.Index;
//                 unvisited.Remove(nearest);
//             }

//             return result;
//         }
//     }

//     // 回應模型類別
//     public class DistanceMatrixResponse
//     {
//         public string Status { get; set; }
//         public List<DistanceMatrixRow> Rows { get; set; }
//     }

//     public class DistanceMatrixRow
//     {
//         public List<DistanceMatrixElement> Elements { get; set; }
//     }

//     public class DistanceMatrixElement
//     {
//         public string Status { get; set; }
//         public DistanceInfo Distance { get; set; }
//         public DurationInfo Duration { get; set; }
//     }

//     public class DistanceInfo
//     {
//         public string Text { get; set; }
//         public int Value { get; set; }
//     }

//     public class DurationInfo
//     {
//         public string Text { get; set; }
//         public int Value { get; set; }
//     }

//     public class OptimizedDestination
//     {
//         public int Id { get; set; }
//         public int SortOrder { get; set; }
//         public string CustomerName { get; set; }
//         public string Address { get; set; }
//         public string ProductInfo { get; set; }
//         public string Note { get; set; }
//         public string SkipReason { get; set; }
//         public string Distance { get; set; }
//         public string Duration { get; set; }
//         public int DistanceValue { get; set; }
//         public int DurationValue { get; set; }
//     }
// }


using System.Text.Json;
using System.Text.RegularExpressions;
using LogisticsRoutePlanner.Models;

namespace LogisticsRoutePlanner.Services
{
    public class GoogleMapsRouteService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GoogleMapsRouteService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleMaps:ApiKey"];
        }

        /// <summary>
        /// 使用 Google Maps Distance Matrix API 獲取兩點間的距離和時間
        /// </summary>
        public async Task<DistanceMatrixResponse> GetDistanceMatrixAsync(List<string> origins, List<string> destinations)
        {
            var originsParam = string.Join("|", origins.Select(Uri.EscapeDataString));
            var destinationsParam = string.Join("|", destinations.Select(Uri.EscapeDataString));
            
            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?" +
                     $"origins={originsParam}&destinations={destinationsParam}&" +
                     $"key={_apiKey}&units=metric&mode=driving&language=zh-TW";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<DistanceMatrixResponse>(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Google Maps Distance Matrix API 錯誤: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 智能路線優化，考慮時間約束和區域集中配送
        /// </summary>
        public async Task<List<OptimizedDestination>> OptimizeRouteAsync(
            string originAddress, 
            List<ShipmentDestination> destinations,
            DeliveryConstraints constraints = null)
        {
            if (destinations == null || !destinations.Any())
                return new List<OptimizedDestination>();

            Console.WriteLine($"出貨地: {originAddress}");

            // 準備所有地址
            var allAddresses = new List<string> { originAddress };
            allAddresses.AddRange(destinations.Select(d => d.Address));

            // 獲取距離矩陣
            var distanceMatrix = await GetDistanceMatrixAsync(allAddresses, allAddresses);
            
            if (distanceMatrix?.Status != "OK")
                throw new Exception($"Google Maps API 回應錯誤: {distanceMatrix?.Status}");

            // 解析配送約束
            var deliveryConstraints = constraints ?? new DeliveryConstraints();
            var destinationsWithConstraints = ParseDeliveryConstraints(destinations, deliveryConstraints);

            // 使用新的區域集中配送算法
            var optimizedRoute = RegionalDeliveryOptimization(
                distanceMatrix, 
                destinationsWithConstraints, 
                originAddress,
                deliveryConstraints);
            
            return optimizedRoute;
        }

        /// <summary>
        /// 解析配送約束條件
        /// </summary>
        private List<DestinationWithConstraints> ParseDeliveryConstraints(
            List<ShipmentDestination> destinations, 
            DeliveryConstraints globalConstraints)
        {
            var result = new List<DestinationWithConstraints>();

            foreach (var dest in destinations)
            {
                var constraints = new DestinationConstraints
                {
                    PreferredTimeSlots = new List<TimeSlot>(),
                    AvoidTimeSlots = new List<TimeSlot>(),
                    Priority = DestinationPriority.Normal
                };

                // 解析備註中的時間約束
                if (!string.IsNullOrEmpty(dest.Note))
                {
                    ParseTimeConstraintsFromNote(dest.Note, constraints);
                }

                // 根據地址和產品類型設定優先級
                SetPriorityBasedOnProductAndAddress(dest, constraints);

                result.Add(new DestinationWithConstraints
                {
                    Destination = dest,
                    Constraints = constraints
                });
            }

            return result;
        }

        /// <summary>
        /// 從備註中解析時間約束
        /// </summary>
        private void ParseTimeConstraintsFromNote(string note, DestinationConstraints constraints)
        {
            var lowerNote = note.ToLower();

            // 解析避免的時間段
            if (lowerNote.Contains("中午前不在") || lowerNote.Contains("上午不在"))
            {
                constraints.AvoidTimeSlots.Add(new TimeSlot
                {
                    StartTime = TimeSpan.FromHours(8),
                    EndTime = TimeSpan.FromHours(12),
                    Reason = "客戶不在家"
                });
            }

            if (lowerNote.Contains("下午前不在") || lowerNote.Contains("14點前不在"))
            {
                constraints.AvoidTimeSlots.Add(new TimeSlot
                {
                    StartTime = TimeSpan.FromHours(8),
                    EndTime = TimeSpan.FromHours(14),
                    Reason = "客戶不在家"
                });
            }

            if (lowerNote.Contains("晚上") || lowerNote.Contains("18點後"))
            {
                constraints.PreferredTimeSlots.Add(new TimeSlot
                {
                    StartTime = TimeSpan.FromHours(18),
                    EndTime = TimeSpan.FromHours(21),
                    Reason = "客戶偏好時間"
                });
            }

            // 解析緊急或優先配送
            if (lowerNote.Contains("緊急") || lowerNote.Contains("急件") || lowerNote.Contains("優先"))
            {
                constraints.Priority = DestinationPriority.High;
            }

            // 解析特定時間要求
            var timePattern = @"(\d{1,2})[：:點](\d{0,2})";
            var matches = Regex.Matches(note, timePattern);
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int hour))
                {
                    int minute = 0;
                    if (match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out int min))
                    {
                        minute = min;
                    }

                    if (hour >= 8 && hour <= 21)
                    {
                        constraints.PreferredTimeSlots.Add(new TimeSlot
                        {
                            StartTime = new TimeSpan(hour, minute, 0),
                            EndTime = new TimeSpan(hour + 1, minute, 0),
                            Reason = "客戶指定時間"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 根據產品和地址設定優先級
        /// </summary>
        private void SetPriorityBasedOnProductAndAddress(ShipmentDestination dest, DestinationConstraints constraints)
        {
            // 冷凍冷藏商品優先配送
            if (!string.IsNullOrEmpty(dest.ProductInfo))
            {
                var productLower = dest.ProductInfo.ToLower();
                if (productLower.Contains("冷凍") || productLower.Contains("冷藏") || productLower.Contains("生鮮"))
                {
                    constraints.Priority = DestinationPriority.High;
                }
            }
        }

        /// <summary>
        /// 區域集中配送優化算法
        /// </summary>
        private List<OptimizedDestination> RegionalDeliveryOptimization(
            DistanceMatrixResponse matrix,
            List<DestinationWithConstraints> destinations,
            string originAddress,
            DeliveryConstraints globalConstraints)
        {
            try
            {
                Console.WriteLine("開始區域集中配送優化...");
                
                // 第一步：按區域分組（相同區的一起送）
                var regionalGroups = GroupByRegion(destinations);
                Console.WriteLine($"共分為 {regionalGroups.Count} 個區域組");

                // 第二步：決定區域訪問順序（從出貨地開始，選擇最近的區域）
                var regionOrder = DetermineRegionOrder(matrix, regionalGroups, destinations.Select(d => d.Destination).ToList());

                // 第三步：在每個區域內按最近鄰居算法排序，並計算預計送達時間
                var optimizedRoute = new List<OptimizedDestination>();
                var currentTime = TimeSpan.FromHours(9); // 從早上9點開始
                int currentLocationIndex = 0; // 從出貨地開始
                int globalOrder = 1;

                foreach (var regionName in regionOrder)
                {
                    var regionDestinations = regionalGroups[regionName];
                    Console.WriteLine($"處理區域: {regionName}, 包含 {regionDestinations.Count} 個目的地");

                    // 處理時間約束：先送有時間限制的，再送一般的
                    var urgentDestinations = regionDestinations
                        .Where(d => d.Constraints.Priority == DestinationPriority.High || 
                                   d.Constraints.AvoidTimeSlots.Any() || 
                                   d.Constraints.PreferredTimeSlots.Any())
                        .OrderByDescending(d => d.Constraints.Priority)
                        .ToList();

                    var normalDestinations = regionDestinations
                        .Where(d => d.Constraints.Priority != DestinationPriority.High && 
                                   !d.Constraints.AvoidTimeSlots.Any() && 
                                   !d.Constraints.PreferredTimeSlots.Any())
                        .ToList();

                    // 先處理有時間約束的
                    var allDestInRegion = destinations.Select(d => d.Destination).ToList();
                    var regionOptimized = ProcessDestinationsInRegion(
                        matrix, urgentDestinations.Concat(normalDestinations).ToList(), 
                        allDestInRegion, currentLocationIndex, currentTime, globalOrder);

                    // 更新當前位置和時間
                    if (regionOptimized.Any())
                    {
                        var lastDest = regionOptimized.Last();
                        currentLocationIndex = allDestInRegion.FindIndex(d => d.Id == lastDest.Id) + 1;
                        currentTime = lastDest.EstimatedArrivalTime.Add(TimeSpan.FromMinutes(15)); // 假設每次配送需要15分鐘
                        globalOrder += regionOptimized.Count;
                    }

                    optimizedRoute.AddRange(regionOptimized);
                }

                Console.WriteLine($"區域集中配送優化完成，共 {optimizedRoute.Count} 個配送點");
                return optimizedRoute;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"區域集中配送優化錯誤: {ex.Message}");
                return GetFallbackRoute(destinations.Select(d => d.Destination).ToList());
            }
        }

        /// <summary>
        /// 按區域分組
        /// </summary>
        private Dictionary<string, List<DestinationWithConstraints>> GroupByRegion(
            List<DestinationWithConstraints> destinations)
        {
            var groups = new Dictionary<string, List<DestinationWithConstraints>>();

            foreach (var dest in destinations)
            {
                var region = ExtractRegionFromAddress(dest.Destination.Address);
                
                if (!groups.ContainsKey(region))
                {
                    groups[region] = new List<DestinationWithConstraints>();
                }
                
                groups[region].Add(dest);
            }

            return groups;
        }

        /// <summary>
        /// 從地址中提取區域名稱
        /// </summary>
        private string ExtractRegionFromAddress(string address)
        {
            // 高雄市的區域匹配
            var regionPattern = @"高雄市(\w+區)";
            var match = Regex.Match(address, regionPattern);
            
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // 如果沒有匹配到，用郵遞區號前三碼
            var zipPattern = @"^(\d{3})";
            var zipMatch = Regex.Match(address, zipPattern);
            if (zipMatch.Success)
            {
                return $"郵遞區號{zipMatch.Groups[1].Value}";
            }

            return "其他區域";
        }

        /// <summary>
        /// 決定區域訪問順序
        /// </summary>
        private List<string> DetermineRegionOrder(
            DistanceMatrixResponse matrix,
            Dictionary<string, List<DestinationWithConstraints>> regionalGroups,
            List<ShipmentDestination> allDestinations)
        {
            var regionOrder = new List<string>();
            var unvisitedRegions = regionalGroups.Keys.ToList();
            int currentLocationIndex = 0; // 從出貨地開始

            Console.WriteLine("決定區域訪問順序...");

            while (unvisitedRegions.Any())
            {
                string nearestRegion = null;
                int nearestDistance = int.MaxValue;

                foreach (var region in unvisitedRegions)
                {
                    // 找這個區域中離當前位置最近的點
                    var regionDestinations = regionalGroups[region];
                    var minDistanceInRegion = int.MaxValue;

                    foreach (var dest in regionDestinations)
                    {
                        var destIndex = allDestinations.FindIndex(d => d.Id == dest.Destination.Id) + 1;
                        try
                        {
                            var distance = matrix.Rows[currentLocationIndex].Elements[destIndex].Distance?.Value ?? int.MaxValue;
                            if (distance < minDistanceInRegion)
                            {
                                minDistanceInRegion = distance;
                            }
                        }
                        catch { }
                    }

                    if (minDistanceInRegion < nearestDistance)
                    {
                        nearestDistance = minDistanceInRegion;
                        nearestRegion = region;
                    }
                }

                if (nearestRegion != null)
                {
                    regionOrder.Add(nearestRegion);
                    unvisitedRegions.Remove(nearestRegion);
                    
                    // 找到這個區域的最後一個配送點，作為下一個起始點
                    var regionDests = regionalGroups[nearestRegion];
                    if (regionDests.Any())
                    {
                        // 先選一個代表點（區域中的第一個點）作為下次的起始參考
                        var representativeDest = regionDests.First();
                        currentLocationIndex = allDestinations.FindIndex(d => d.Id == representativeDest.Destination.Id) + 1;
                    }
                    
                    Console.WriteLine($"下一個區域: {nearestRegion} (距離: {nearestDistance / 1000.0:F1} 公里)");
                }
            }

            return regionOrder;
        }

        /// <summary>
        /// 處理區域內的目的地
        /// </summary>
        private List<OptimizedDestination> ProcessDestinationsInRegion(
            DistanceMatrixResponse matrix,
            List<DestinationWithConstraints> destinations,
            List<ShipmentDestination> allDestinations,
            int startLocationIndex,
            TimeSpan startTime,
            int startOrder)
        {
            var result = new List<OptimizedDestination>();
            var unvisited = destinations.ToList();
            int currentIndex = startLocationIndex;
            var currentTime = startTime;
            int order = startOrder;

            while (unvisited.Any())
            {
                // 找到離當前位置最近的目的地
                DestinationWithConstraints nearest = null;
                int nearestDistance = int.MaxValue;
                int nearestIndex = -1;
                int travelTimeMinutes = 0;

                foreach (var dest in unvisited)
                {
                    var destIndex = allDestinations.FindIndex(d => d.Id == dest.Destination.Id) + 1;
                    
                    try
                    {
                        var distance = matrix.Rows[currentIndex].Elements[destIndex].Distance?.Value ?? int.MaxValue;
                        var duration = matrix.Rows[currentIndex].Elements[destIndex].Duration?.Value ?? 0;
                        
                        // 檢查時間約束
                        if (IsTimeSlotSuitable(dest, currentTime.Add(TimeSpan.FromSeconds(duration))))
                        {
                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearest = dest;
                                nearestIndex = destIndex;
                                travelTimeMinutes = duration / 60; // 轉換為分鐘
                            }
                        }
                    }
                    catch { }
                }

                // 如果沒有找到符合時間約束的，選擇最近的
                if (nearest == null && unvisited.Any())
                {
                    foreach (var dest in unvisited)
                    {
                        var destIndex = allDestinations.FindIndex(d => d.Id == dest.Destination.Id) + 1;
                        
                        try
                        {
                            var distance = matrix.Rows[currentIndex].Elements[destIndex].Distance?.Value ?? int.MaxValue;
                            var duration = matrix.Rows[currentIndex].Elements[destIndex].Duration?.Value ?? 0;
                            
                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearest = dest;
                                nearestIndex = destIndex;
                                travelTimeMinutes = duration / 60;
                            }
                        }
                        catch { }
                    }
                }

                if (nearest != null)
                {
                    // 計算預計送達時間
                    var arrivalTime = currentTime.Add(TimeSpan.FromMinutes(travelTimeMinutes));
                    var deliveryEndTime = arrivalTime.Add(TimeSpan.FromMinutes(15)); // 假設配送需要15分鐘

                    var optimizedDest = CreateOptimizedDestination(
                        nearest.Destination, 
                        matrix, 
                        currentIndex, 
                        nearestIndex);
                    
                    optimizedDest.SortOrder = order++;
                    optimizedDest.EstimatedArrivalTime = arrivalTime;
                    optimizedDest.TimeSlot = $"{arrivalTime:hh\\:mm}-{deliveryEndTime:hh\\:mm}";
                    
                    result.Add(optimizedDest);
                    unvisited.Remove(nearest);
                    currentIndex = nearestIndex;
                    currentTime = deliveryEndTime;
                    
                    Console.WriteLine($"  配送點 {order-1}: {nearest.Destination.Address}");
                    Console.WriteLine($"    車程時間: {travelTimeMinutes} 分鐘");
                    Console.WriteLine($"    預計送達: {arrivalTime:hh\\:mm}-{deliveryEndTime:hh\\:mm}");
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 檢查時間段是否合適
        /// </summary>
        private bool IsTimeSlotSuitable(DestinationWithConstraints dest, TimeSpan arrivalTime)
        {
            // 檢查避免時間段
            foreach (var avoidSlot in dest.Constraints.AvoidTimeSlots)
            {
                if (arrivalTime >= avoidSlot.StartTime && arrivalTime <= avoidSlot.EndTime)
                {
                    return false;
                }
            }

            // 如果有偏好時間段，檢查是否在偏好時間內
            if (dest.Constraints.PreferredTimeSlots.Any())
            {
                return dest.Constraints.PreferredTimeSlots.Any(slot =>
                    arrivalTime >= slot.StartTime && arrivalTime <= slot.EndTime);
            }

            return true;
        }

        /// <summary>
        /// 創建優化的目的地對象
        /// </summary>
        private OptimizedDestination CreateOptimizedDestination(
            ShipmentDestination destination,
            DistanceMatrixResponse matrix,
            int fromIndex,
            int toIndex)
        {
            try
            {
                var element = matrix.Rows[fromIndex].Elements[toIndex];
                return new OptimizedDestination
                {
                    Id = destination.Id,
                    SortOrder = 0,
                    CustomerName = destination.CustomerName,
                    Address = destination.Address,
                    ProductInfo = destination.ProductInfo,
                    Note = destination.Note,
                    SkipReason = destination.SkipReason,
                    Distance = element.Distance?.Text ?? "未知",
                    Duration = element.Duration?.Text ?? "未知",
                    DistanceValue = element.Distance?.Value ?? 0,
                    DurationValue = element.Duration?.Value ?? 0
                };
            }
            catch
            {
                return new OptimizedDestination
                {
                    Id = destination.Id,
                    SortOrder = 0,
                    CustomerName = destination.CustomerName,
                    Address = destination.Address,
                    ProductInfo = destination.ProductInfo,
                    Note = destination.Note,
                    SkipReason = destination.SkipReason,
                    Distance = "未知",
                    Duration = "未知",
                    DistanceValue = 0,
                    DurationValue = 0
                };
            }
        }

        /// <summary>
        /// 備用路線（發生錯誤時使用）
        /// </summary>
        private List<OptimizedDestination> GetFallbackRoute(List<ShipmentDestination> destinations)
        {
            Console.WriteLine("使用備用路線");
            return destinations.Select((dest, index) => new OptimizedDestination
            {
                Id = dest.Id,
                SortOrder = index + 1,
                CustomerName = dest.CustomerName,
                Address = dest.Address,
                ProductInfo = dest.ProductInfo,
                Note = dest.Note,
                SkipReason = dest.SkipReason,
                Distance = "未知",
                Duration = "未知",
                DistanceValue = 0,
                DurationValue = 0,
                TimeSlot = "全天候"
            }).ToList();
        }
    }

    // 配送約束相關類別
    public class DeliveryConstraints
    {
        public TimeSpan DeliveryStartTime { get; set; } = TimeSpan.FromHours(8);
        public TimeSpan DeliveryEndTime { get; set; } = TimeSpan.FromHours(18);
        public int MaxDeliveryDuration { get; set; } = 600;
        public bool ConsiderTrafficConditions { get; set; } = true;
    }

    public class DestinationWithConstraints
    {
        public ShipmentDestination Destination { get; set; }
        public DestinationConstraints Constraints { get; set; }
    }

    public class DestinationConstraints
    {
        public List<TimeSlot> PreferredTimeSlots { get; set; } = new List<TimeSlot>();
        public List<TimeSlot> AvoidTimeSlots { get; set; } = new List<TimeSlot>();
        public DestinationPriority Priority { get; set; } = DestinationPriority.Normal;
    }

    public class TimeSlot
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Reason { get; set; }
    }

    public enum DestinationPriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Urgent = 4
    }

    // 擴展的 OptimizedDestination 類別
    public class OptimizedDestination
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string ProductInfo { get; set; }
        public string Note { get; set; }
        public string SkipReason { get; set; }
        public string Distance { get; set; }
        public string Duration { get; set; }
        public int DistanceValue { get; set; }
        public int DurationValue { get; set; }
        public string TimeSlot { get; set; }
        public TimeSpan EstimatedArrivalTime { get; set; } // 新增：預計送達時間
    }

    // API 回應模型
    public class DistanceMatrixResponse
    {
        public string Status { get; set; }
        public List<DistanceMatrixRow> Rows { get; set; }
    }

    public class DistanceMatrixRow
    {
        public List<DistanceMatrixElement> Elements { get; set; }
    }

    public class DistanceMatrixElement
    {
        public string Status { get; set; }
        public DistanceInfo Distance { get; set; }
        public DurationInfo Duration { get; set; }
    }

    public class DistanceInfo
    {
        public string Text { get; set; }
        public int Value { get; set; }
    }

    public class DurationInfo
    {
        public string Text { get; set; }
        public int Value { get; set; }
    }
}