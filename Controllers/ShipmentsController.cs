using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsRoutePlanner.Data;
using LogisticsRoutePlanner.Models;
using LogisticsRoutePlanner.Helpers;
using LogisticsRoutePlanner.Models.ViewModels;
using LogisticsRoutePlanner.Services;

namespace LogisticsRoutePlanner.Controllers
{
    public class ShipmentsController : Controller
    {
        private readonly LogisticsDbContext _context;
        private readonly GoogleMapsRouteService _googleMapsService;

        // 建構子注入資料庫與 Google Maps 路徑服務
        public ShipmentsController(LogisticsDbContext context, GoogleMapsRouteService googleMapsService)
        {
            _context = context;
            _googleMapsService = googleMapsService; 
        }


        // 顯示分頁的配送任務列表
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // 計算總記錄數
            var totalRecords = await _context.Shipments.CountAsync();
            
            // 計算總頁數
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            
            // 獲取分頁數據
            var shipments = await _context.Shipments
                .OrderBy(s => s.ShipmentDate) // 按日期排序
                .Skip((page - 1) * pageSize)  // 跳過前面的記錄
                .Take(pageSize)               // 取當頁的數量
                .ToListAsync();
            
            // 將分頁信息傳遞到View
            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewBag.TotalRecords = totalRecords; // 用於顯示記錄範圍
            
            return View(shipments);
        }

        // 顯示新增配送任務表單
        public IActionResult Create()
        {
            var shipment = new Shipment
            {
                ShipmentDate = DateTime.Today // 預設為今天
            };
            return View(shipment);
        }


        // 處理新增配送任務表單提交
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Shipment shipment)
        {
            if (ModelState.IsValid)
            {
                // 儲存到資料庫
                _context.Shipments.Add(shipment);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "配送任務已成功創建！";

                return RedirectToAction("Create"); // 導回同頁以顯示訊息
            }
            return View(shipment); // 驗證失敗時回傳原表單
        }


        // 顯示配送任務詳情
        public async Task<IActionResult> Details(int? id)
        {
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
        

        // 新增配送目的地   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDestination(AddDestinationViewModel model)
        {
            // 檢查 ShipmentId
            if (model.ShipmentId <= 0)
            {
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
                SkipReason = "", //跳過原因
                SortOrder = await _context.ShipmentDestinations.Where(d => d.ShipmentId == model.ShipmentId).CountAsync() + 1
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



        /// 刪除配送目的地（前端使用 AJAX 呼叫）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDestination(int id)
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
            
            // 查詢剩下的目的地並回傳
            var remainingDestinations = _context.ShipmentDestinations
                .Where(d => d.ShipmentId == destination.ShipmentId)
                .ToList();
            
            
            return Json(new { 
                success = true,
                id = id,
                destinations = remainingDestinations // 返回剩餘目的地
            });
        }


        /// 測試用 API，確認路由是否正確連接 Controller
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
        [ValidateAntiForgeryToken] 
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

            return Ok();  
        }

        /// 更新配送目的地的跳過原因
        [HttpPost]
        public IActionResult UpdateSkipReason(int destinationId, string reason)
        {
            // 取得相對應的配送地點
            var destination = _context.ShipmentDestinations.FirstOrDefault(d => d.Id == destinationId);
            if (destination == null)
                return NotFound(); // 若找不到該地點，返回 404

            // 更新跳過原因欄位
            destination.SkipReason = reason;

            // 儲存變更
            _context.SaveChanges();

            return Json(new { success = true, message = "跳過原因已更新" });
        }

     
        /// 使用 Google Maps API 優化配送路線順序
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
                    return Json(new { success = false, message = "找不到配送任務" });

                // 確保至少有一個目的地
                if (!shipment.Destinations.Any())
                    return Json(new { success = false, message = "沒有配送目的地可供優化" });

                // 使用 Google Maps 服務進行路線優化
                var optimizedDestinations = await _googleMapsService.OptimizeRouteAsync(
                    shipment.OriginAddress, 
                    shipment.Destinations.ToList()
                );

                // 更新資料庫中的排序順序
                foreach (var optimized in optimizedDestinations)
                {
                    var destination = shipment.Destinations.FirstOrDefault(d => d.Id == optimized.Id);
                    if (destination != null)
                        destination.SortOrder = optimized.SortOrder;
                }

                _context.ShipmentDestinations.UpdateRange(shipment.Destinations);
                await _context.SaveChangesAsync();

                Console.WriteLine("Google Maps 優化後的路線順序:");
                foreach (var dest in optimizedDestinations)
                {
                    Console.WriteLine($"{dest.SortOrder}. {dest.Address} - 距離: {dest.Distance}, 時間: {dest.Duration}");
                }

                return Json(new { 
                    success = true,
                    message = "送貨順序已使用 Google Maps 優化",
                    optimizedDestinations = optimizedDestinations.Select(d => new {
                        id = d.Id,
                        sortOrder = d.SortOrder,
                        customerName = d.CustomerName,
                        address = d.Address,
                        productInfo = d.ProductInfo,
                        note = d.Note,
                        skipReason = d.SkipReason,
                        distance = d.Distance,
                        duration = d.Duration
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"路線優化錯誤: {ex.Message}");
                return Json(new { 
                    success = false, 
                    message = $"路線優化失敗: {ex.Message}" 
                });
            }
        }

        /// 更新配送狀態
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, DeliveryStatus status)
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
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"狀態已更新為 {status}";
            return RedirectToAction("Details", new { id = destination.ShipmentId });
        }


        /// 匯入 Excel 檔案
        public IActionResult ImportExcel(int id)
        {
            ViewBag.ShipmentId = id;
            return View();
        }


        /// 預覽 Excel 檔案
        [HttpPost]
        public async Task<IActionResult> PreviewExcel(int shipmentId, IFormFile file, string productInfo, string note)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "請選擇檔案";
                return RedirectToAction(nameof(ImportExcel), new { id = shipmentId });
            }

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
                if (row >= 5) 
                    break;
            }

            // 3. 傳遞更多資料到前端
            ViewBag.Headers = headers;
            ViewBag.PreviewData = previewData; 
            ViewBag.ShipmentId = shipmentId;
            ViewBag.FileContent = Convert.ToBase64String(stream.ToArray());
            ViewBag.ProductInfo = productInfo;
            ViewBag.Note = note;

            return View("MapColumns");
        }



        /// 確認匯入 Excel 檔案
        [HttpPost]
        public async Task<IActionResult> ImportExcelConfirmed(int shipmentId, string base64,
            string addressColumn, string nameColumn, string productColumn, string noteColumn)
        {
            // 1. 查詢貨運批次資料（包含目的地清單）
            var shipment = await _context.Shipments.Include(s => s.Destinations)
                .FirstOrDefaultAsync(s => s.Id == shipmentId);
            if (shipment == null) 
                return NotFound();

            // 2. 處理Excel檔案
            var bytes = Convert.FromBase64String(base64); // 將Base64轉為byte陣列
            using var stream = new MemoryStream(bytes); // 建立記憶體流
            using var package = new OfficeOpenXml.ExcelPackage(stream); // 使用EPPlus套件讀取Excel
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
                    Address = sheet.Cells[row, columns[addressColumn]].Text.Trim(), // 讀取地址
                    CustomerName = sheet.Cells[row, columns[nameColumn]].Text.Trim(), // 讀取客戶名稱
                    // 可選欄位處理（若欄位名稱為空則設為null）
                    ProductInfo = string.IsNullOrWhiteSpace(productColumn) ? null : sheet.Cells[row, columns[productColumn]].Text.Trim(),
                    Note = string.IsNullOrWhiteSpace(noteColumn) ? null : sheet.Cells[row, columns[noteColumn]].Text.Trim()
                };

                var geo = await GeocodingHelper.GetLatLonAsync(dest.Address);
                if (geo != null) // 若成功取得經緯度
                {
                    dest.Latitude = geo.Value.lat; // 設定緯度
                    dest.Longitude = geo.Value.lon; // 設定經度
                }

                _context.ShipmentDestinations.Add(dest);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = shipmentId });
        }


        
    }
}