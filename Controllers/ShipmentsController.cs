using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsRoutePlanner.Data;
using LogisticsRoutePlanner.Models;
using LogisticsRoutePlanner.Helpers;
using System.Diagnostics;
using LogisticsRoutePlanner.Models.ViewModels;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LogisticsRoutePlanner.Controllers
{
    /// <summary>
    /// 配送任務管理控制器
    /// </summary>
    public class ShipmentsController : Controller
    {
        private readonly LogisticsDbContext _context;

        // 建構子，依賴注入資料庫上下文
        public ShipmentsController(LogisticsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 顯示所有配送任務列表
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var shipments = await _context.Shipments.ToListAsync();
            return View(shipments);
        }

        /// 顯示新增配送任務表單
        public IActionResult Create()
        {
            var shipment = new Shipment
            {
                ShipmentDate = DateTime.Today
            };
            return View(shipment);
        }


        /// 處理新增配送任務表單提交
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Shipment shipment)
        {
            if (ModelState.IsValid)
            {
                // 處理表單資料，儲存到資料庫
                _context.Shipments.Add(shipment);
                _context.SaveChanges();

                // 設置 TempData 來顯示成功訊息
                TempData["SuccessMessage"] = "配送任務已成功創建！";

                // 重定向回 Create 頁面，這樣就會顯示訊息
                return RedirectToAction("Create");
            }

            // 如果驗證失敗，返回 Create 頁面並顯示錯誤訊息
            return View(shipment);
        }


        /// 顯示配送任務詳情
        public async Task<IActionResult> Details(int? id)
        {
            // 檢查ID是否有效
            if (id == null || id == 0)
                return NotFound();

            // 查詢配送任務及相關目的地
            var shipment = await _context.Shipments
                .Include(s => s.Destinations)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (shipment == null)
                return NotFound();
            
            return View(shipment);
        }
        
        /// 新增配送目的地
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDestination(AddDestinationViewModel model)
        {
            Console.WriteLine($"嘗試新增地點到 Shipment ID: {model.ShipmentId}");
            
            try
            {
                // 檢查 ShipmentId
                if (model.ShipmentId <= 0)
                {
                    Console.WriteLine("無效的 ShipmentId: " + model.ShipmentId);
                    TempData["ErrorMessage"] = "無效的配送任務 ID";
                    return RedirectToAction("Index");
                }

                // 檢查資料驗證
                if (!ModelState.IsValid)
                {
                    // 輸出驗證錯誤
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"驗證錯誤: {error.ErrorMessage}");
                    }
                    
                    TempData["ErrorMessage"] = "請檢查輸入資料";
                    return RedirectToAction("Details", new { id = model.ShipmentId });
                }

                // 檢查配送任務是否存在
                var shipment = await _context.Shipments.FindAsync(model.ShipmentId);
                if (shipment == null)
                {
                    Console.WriteLine($"找不到Shipment ID: {model.ShipmentId}");
                    TempData["ErrorMessage"] = "找不到配送任務";
                    return RedirectToAction("Index");
                }

                // 建立新的目的地物件
                var destination = new ShipmentDestination
                {
                    ShipmentId = model.ShipmentId,
                    CustomerName = model.CustomerName,
                    Address = model.Address,
                    Note = model.Note,
                    Status = DeliveryStatus.Pending,
                    SkipReason = "", //跳過原因，新增時不用給值
                    SortOrder = await _context.ShipmentDestinations
                        .Where(d => d.ShipmentId == model.ShipmentId)
                        .CountAsync() + 1
                };

                // 透過地址獲取經緯度
                var geo = await GeocodingHelper.GetLatLonAsync(model.Address);
                if (geo != null)
                {
                    destination.Latitude = geo.Value.lat;
                    destination.Longitude = geo.Value.lon;
                }
                else
                {
                    // 如果無法獲取坐標，設置默認值
                    destination.Latitude = 0;
                    destination.Longitude = 0;
                }

                // 新增到資料庫
                _context.ShipmentDestinations.Add(destination);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "成功新增客戶地點";
                return RedirectToAction("Details", new { id = model.ShipmentId });
            }
            catch (Exception ex)
            {
                // 更詳細的錯誤記錄
                Console.WriteLine($"異常: {ex.Message}");
                Console.WriteLine($"堆疊追蹤: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"內部異常: {ex.InnerException.Message}");
                }
                
                TempData["ErrorMessage"] = "新增客戶地點時發生錯誤: " + ex.Message;
                return RedirectToAction("Details", new { id = model.ShipmentId > 0 ? model.ShipmentId : 1 }); // 確保有效的ID
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDestination(int id)
        {
            try
            {
                var destination = _context.ShipmentDestinations.Find(id);
                if (destination == null)
                {
                    return Json(new { 
                        success = false, 
                        message = $"找不到ID為 {id} 的地點" 
                    });
                }

                _context.ShipmentDestinations.Remove(destination);
                _context.SaveChanges();
                
                // 獲取剩餘目的地
                var remainingDestinations = _context.ShipmentDestinations
                    .Where(d => d.ShipmentId == destination.ShipmentId)
                    .ToList();
                
                return Json(new { 
                    success = true,
                    id = id,
                    destinations = remainingDestinations // 返回剩餘目的地
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"刪除失敗: {ex.Message}" 
                });
            }
        }

        [HttpGet]
        public IActionResult TestRoute()
        {
            return Ok("成功對應到 ShipmentController！");
        }


        [HttpPost]
        public IActionResult SkipDestination([FromBody] SkipReasonDto dto)
        {
            var destination = _context.ShipmentDestinations.Find(dto.Id);
            if (destination == null)
                return NotFound();

            destination.Status = DeliveryStatus.Skipped;
            destination.SkipReason = dto.Reason;
            _context.SaveChanges();

            return Ok(new { success = true });
        }




        // 接收前端的 POST 請求來刪除指定的出貨任務（Shipment）
        [HttpPost]
        [ValidateAntiForgeryToken] // 驗證防偽 Token，防止 CSRF 攻擊
        public async Task<IActionResult> Delete(int id)
        {
             // 根據傳入的 id 從資料庫中查詢對應的 Shipment 資料
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
                return NotFound();  // 若找不到資料，回傳 404 Not Found
            
            // 從資料庫移除該筆 Shipment 資料
            _context.Shipments.Remove(shipment);

            // 儲存異動至資料庫
            await _context.SaveChangesAsync();

            return Ok();  // 回傳 200 OK 回應，符合前端 fetch API 的設計（不需重新導頁）
        }


        /// 更新配送目的地的跳過原因
        /// <param name="destinationId">目的地ID</param>
        /// <param name="reason">跳過原因</param>
        /// <returns>JSON結果</returns>
        [HttpPost]
        public IActionResult UpdateSkipReason(int destinationId, string reason)
        {
            // 取得相對應的配送地點
            var destination = _context.ShipmentDestinations.FirstOrDefault(d => d.Id == destinationId);
            if (destination == null)
            {
                return NotFound(); // 若找不到該地點，返回 404
            }

            // 更新跳過原因欄位
            destination.SkipReason = reason;

            // 儲存變更
            _context.SaveChanges();

            return Json(new { success = true, message = "跳過原因已更新" });
        }




 

        /// 優化配送路線順序
        /// <param name="id">配送任務ID</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OptimizeRoute(int id)
        {
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.Destinations)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (shipment == null)
                {
                    TempData["ErrorMessage"] = "找不到配送任務";
                    return RedirectToAction("Index");
                }

                // 確保至少有一個目的地
                if (!shipment.Destinations.Any())
                {
                    TempData["ErrorMessage"] = "沒有配送目的地可供優化";
                    return RedirectToAction("Details", new { id });
                }

                // 獲取出貨地點的經緯度
                var originGeo = await GeocodingHelper.GetLatLonAsync(shipment.OriginAddress);
                if (originGeo == null)
                {
                    TempData["ErrorMessage"] = "無法獲取出貨地點的地理座標";
                    return RedirectToAction("Details", new { id });
                }

                // 記錄起點信息
                Console.WriteLine($"起點地址: {shipment.OriginAddress}, 經度: {originGeo.Value.lon}, 緯度: {originGeo.Value.lat}");

                var startPoint = new { Lat = originGeo.Value.lat, Lon = originGeo.Value.lon };
                var currentPoint = startPoint;
                var destinations = shipment.Destinations.ToList();
                
                // 檢查目的地的經緯度，如果為0或null則嘗試獲取
                foreach (var dest in destinations.Where(d => d.Latitude == 0 && d.Longitude == 0))
                {
                    var geo = await GeocodingHelper.GetLatLonAsync(dest.Address);
                    if (geo != null)
                    {
                        dest.Latitude = geo.Value.lat;
                        dest.Longitude = geo.Value.lon;
                        Console.WriteLine($"更新目的地經緯度: {dest.Address}, 經度: {geo.Value.lon}, 緯度: {geo.Value.lat}");
                    }
                    else
                    {
                        // 如果無法取得經緯度，則記錄錯誤並通知使用者
                        TempData["WarningMessage"] = $"目的地 {dest.Address} 無法獲取經緯度。";
                        Console.WriteLine($"警告: 無法獲取目的地經緯度: {dest.Address}");
                    }
                }

                var sortedDestinations = new List<ShipmentDestination>();

                // 貪婪算法：每次找最近的點
                int order = 1;
                while (destinations.Any())
                {
                    // 找到距離當前點最近的下一個點
                    var nearestDestination = destinations
                        .Select(d => new { 
                            Destination = d, 
                            Distance = CalculateDistance(currentPoint.Lat, currentPoint.Lon, d.Latitude, d.Longitude) 
                        })
                        .OrderBy(x => x.Distance)
                        .First();
                    
                    // 記錄選擇的最近點和距離
                    Console.WriteLine($"選擇目的地: {nearestDestination.Destination.Address}, 距離: {nearestDestination.Distance:F2}公里");
                    
                    nearestDestination.Destination.SortOrder = order++;
                    sortedDestinations.Add(nearestDestination.Destination);
                    destinations.Remove(nearestDestination.Destination);
                    
                    // 更新當前點
                    currentPoint = new { Lat = nearestDestination.Destination.Latitude, Lon = nearestDestination.Destination.Longitude };
                }

                Console.WriteLine("優化後的路線順序:");
                // 依照最佳路線順序，重新賦值 SortOrder
                for (int i = 0; i < sortedDestinations.Count; i++)
                {
                    sortedDestinations[i].SortOrder = i + 1;  // 設置排序值
                    Console.WriteLine($"{sortedDestinations[i].SortOrder}. {sortedDestinations[i].Address}");
                }

                // 儲存到資料庫
                _context.ShipmentDestinations.UpdateRange(sortedDestinations);
                await _context.SaveChangesAsync();

                
                TempData["SuccessMessage"] = "送貨順序已優化";
                // return RedirectToAction("Details", new { id });

                Console.WriteLine("優化路線成功");

                // 返回 JSON 而非重定向
                return Json(new { 
                    success = true,
                    message = "送貨順序已優化",
                    optimizedDestinations = sortedDestinations.Select(d => new {
                        id = d.Id,
                        sortOrder = d.SortOrder, //排序順序
                        customerName = d.CustomerName, //收貨人
                        address = d.Address, //地址
                        productInfo = d.ProductInfo, //產品資訊
                        note = d.Note, //備註
                        skipReason = d.SkipReason //跳過原因
                    }).ToList()
                });
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"路線優化時發生錯誤: {ex.Message}");
                Console.WriteLine($"堆疊追蹤: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = "優化路線時發生錯誤: " + ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // 使用 Haversine 公式計算兩點間的距離
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // 地球半徑（公里）
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                    
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        /// <summary>
        /// 更新配送狀態
        /// </summary>
        /// <param name="id">目的地ID</param>
        /// <param name="status">新狀態</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, DeliveryStatus status)
        {
            try
            {
                // 查詢目的地
                var destination = await _context.ShipmentDestinations.FindAsync(id);
                if (destination == null)
                {
                    TempData["ErrorMessage"] = "找不到配送地點";
                    return RedirectToAction("Index");
                }

                // 更新狀態
                destination.Status = status;
                await _context.SaveChangesAsync();  // 儲存變更

                TempData["SuccessMessage"] = $"狀態已更新為 {status}";
                return RedirectToAction("Details", new { id = destination.ShipmentId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "更新狀態時發生錯誤";
                return RedirectToAction("Index");
            }
        }


        /// <summary>
        /// 匯入 Excel 檔案
        /// </summary>
        /// <param name="id">配送任務ID</param>
        /// <returns>匯入頁面</returns>
        public IActionResult ImportExcel(int id)
        {
            ViewBag.ShipmentId = id;
            return View();
        }


        /// 預覽 Excel 檔案
        /// 這個方法會讀取上傳的 Excel 檔案，並將第一行的標題列出來供使用者選擇對應的欄位。
        [HttpPost]
        public async Task<IActionResult> PreviewExcel(int shipmentId, IFormFile file, string productInfo, string note)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "請選擇檔案";
                return RedirectToAction(nameof(ImportExcel), new { id = shipmentId });
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var package = new OfficeOpenXml.ExcelPackage(stream);
                var sheet = package.Workbook.Worksheets.First();

                // 1. 取得標題列
                var headers = new List<string>();
                for (int col = 1; col <= sheet.Dimension.Columns; col++)
                {
                    headers.Add(sheet.Cells[1, col].Text.Trim());
                }

                // 2. 新增資料行讀取
                var previewData = new List<Dictionary<string, string>>();
                for (int row = 2; row <= sheet.Dimension.Rows; row++) // 從第二列開始
                {
                    var rowData = new Dictionary<string, string>();
                    for (int col = 1; col <= sheet.Dimension.Columns; col++)
                    {
                        rowData[headers[col-1]] = sheet.Cells[row, col].Text.Trim();
                    }
                    previewData.Add(rowData);
                    
                    // 只讀取前5行作為預覽
                    if (row >= 5) break;
                }

                // 3. 傳遞更多資料到前端
                ViewBag.Headers = headers;
                ViewBag.PreviewData = previewData; // 新增這行
                ViewBag.ShipmentId = shipmentId;
                ViewBag.FileContent = Convert.ToBase64String(stream.ToArray());
                ViewBag.ProductInfo = productInfo;
                ViewBag.Note = note;

                return View("MapColumns");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"檔案解析錯誤: {ex.Message}";
                return RedirectToAction(nameof(ImportExcel), new { id = shipmentId });
            }
        }






        /// 確認匯入 Excel 檔案
        /// 這個方法會根據使用者選擇的欄位名稱，將 Excel 檔案中的資料匯入到資料庫中。
        [HttpPost]
        public async Task<IActionResult> ImportExcelConfirmed(int shipmentId, string base64,
            string addressColumn, string nameColumn, string productColumn, string noteColumn)
        {
            var shipment = await _context.Shipments.Include(s => s.Destinations)
                .FirstOrDefaultAsync(s => s.Id == shipmentId);
            if (shipment == null) return NotFound();

            var bytes = Convert.FromBase64String(base64);
            using var stream = new MemoryStream(bytes);
            using var package = new OfficeOpenXml.ExcelPackage(stream);
            var sheet = package.Workbook.Worksheets.First();

            var columns = new Dictionary<string, int>();
            for (int col = 1; col <= sheet.Dimension.Columns; col++)
            {
                var title = sheet.Cells[1, col].Text.Trim();
                columns[title] = col;
            }

            for (int row = 2; row <= sheet.Dimension.Rows; row++)
            {
                var dest = new ShipmentDestination
                {
                    ShipmentId = shipmentId,
                    Address = sheet.Cells[row, columns[addressColumn]].Text.Trim(),
                    CustomerName = sheet.Cells[row, columns[nameColumn]].Text.Trim(),
                    ProductInfo = string.IsNullOrWhiteSpace(productColumn) ? null : sheet.Cells[row, columns[productColumn]].Text.Trim(),
                    Note = string.IsNullOrWhiteSpace(noteColumn) ? null : sheet.Cells[row, columns[noteColumn]].Text.Trim()
                };

                var geo = await GeocodingHelper.GetLatLonAsync(dest.Address);
                if (geo != null)
                {
                    dest.Latitude = geo.Value.lat;
                    dest.Longitude = geo.Value.lon;
                }

                _context.ShipmentDestinations.Add(dest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = shipmentId });
        }

        

        


    }
}