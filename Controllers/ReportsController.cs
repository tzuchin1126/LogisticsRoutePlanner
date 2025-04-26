using Microsoft.AspNetCore.Mvc;
using LogisticsRoutePlanner.Data;
using LogisticsRoutePlanner.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

public class ReportsController : Controller
{
    private readonly LogisticsDbContext _context;

    public ReportsController(LogisticsDbContext context)
    {
        _context = context;
    }

    // public async Task<IActionResult> Index(string month, int page = 1)
    // {
    //     DateTime startDate, endDate;
    //     int pageSize = 10;  // 每頁顯示的資料筆數

    //     // 如果沒有傳遞 month 參數，設置為當前月份
    //     if (string.IsNullOrEmpty(month))
    //     {
    //         month = DateTime.Now.ToString("yyyy-MM");
    //     }

    //     // 轉換月份為日期範圍
    //     if (DateTime.TryParse($"{month}-01", out startDate))
    //     {
    //         endDate = startDate.AddMonths(1);

    //         // 查詢總筆數（不分頁用）
    //         var totalShipments = await _context.Shipments
    //             .Where(s => s.ShipmentDate >= startDate && s.ShipmentDate < endDate)
    //             .CountAsync();

    //         // 計算分頁資訊
    //         var totalPages = (int)Math.Ceiling(totalShipments / (double)pageSize);

    //         // 取得當頁資料
    //         var shipments = await _context.Shipments
    //             .Where(s => s.ShipmentDate >= startDate && s.ShipmentDate < endDate)
    //             .Include(s => s.Destinations)
    //             .OrderBy(s => s.ShipmentDate) // 你可以依需求排序
    //             .Skip((page - 1) * pageSize)
    //             .Take(pageSize)
    //             .ToListAsync();

    //         // 傳送分頁與月份資料給 View
    //         ViewData["TotalPages"] = totalPages;
    //         ViewData["CurrentPage"] = page;
    //         ViewData["Month"] = month;
    //         ViewData["TotalRecords"] = totalShipments;

    //         return View(shipments);
    //     }

    //     // 解析月份失敗時，回傳空結果
    //     return View(new List<Shipment>());
    // }

    public async Task<IActionResult> Index(string month, string status, string customer, int page = 1)
    {
        DateTime startDate, endDate;
        int pageSize = 10;

        // 預設當月
        if (string.IsNullOrEmpty(month))
            month = DateTime.Now.ToString("yyyy-MM");

        // 嘗試解析選擇的月份
        if (!DateTime.TryParseExact($"{month}-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out startDate))
        {
            return View(new List<Shipment>()); // 如果解析失敗，返回空清單
        }
        
        endDate = startDate.AddMonths(1); // 設定當月結束日期

        // 查詢基礎資料
        var query = _context.Shipments
            .Include(s => s.Destinations)
            .Where(s => s.ShipmentDate >= startDate && s.ShipmentDate < endDate);

        // 配送狀態篩選
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DeliveryStatus>(status, out var parsedStatus))
        {
            query = query.Where(s => s.Destinations.Any(d => d.Status == parsedStatus));
        }
        // 計算總筆數
        var totalRecords = await query.CountAsync();

        // 分頁
        var shipments = await query
            .OrderByDescending(s => s.ShipmentDate)  // 排序：出貨日期遞減
            .Skip((page - 1) * pageSize)  // 跳過前面頁數的資料
            .Take(pageSize)  // 取當頁資料
            .ToListAsync();

        // 設定分頁資料
        ViewBag.Month = month;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        ViewBag.CurrentPage = page;

        return View(shipments);
    }






    // 匯出excel功能
    public async Task<IActionResult> ExportExcel(string month)
    {
        DateTime startDate, endDate;

        if (string.IsNullOrEmpty(month))
        {
            month = DateTime.Now.ToString("yyyy-MM");
        }

        if (DateTime.TryParse($"{month}-01", out startDate))
        {
            endDate = startDate.AddMonths(1);

            var shipments = await _context.Shipments
                .Where(s => s.ShipmentDate >= startDate && s.ShipmentDate < endDate)
                .Include(s => s.Destinations)
                .ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Shipment Report");
                worksheet.Cells[1, 1].Value = "客戶名稱";
                worksheet.Cells[1, 2].Value = "地址";
                worksheet.Cells[1, 3].Value = "狀態";
                worksheet.Cells[1, 4].Value = "跳過原因";

                int row = 2;
                foreach (var shipment in shipments)
                {
                    foreach (var dest in shipment.Destinations)
                    {
                        worksheet.Cells[row, 1].Value = dest.CustomerName;
                        worksheet.Cells[row, 2].Value = dest.Address;
                        worksheet.Cells[row, 3].Value = dest.Status;
                        worksheet.Cells[row, 4].Value = dest.SkipReason;
                        row++;
                    }
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"Shipment_Report_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        return View(new List<Shipment>());
    }


    // 報表頁面中，刪除按鈕功能
    [HttpDelete]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteDestination(int id)
    {
        var destination = _context.ShipmentDestinations.FirstOrDefault(d => d.Id == id);
        if (destination == null)
            return NotFound();

        _context.ShipmentDestinations.Remove(destination);
        _context.SaveChanges();

        return Ok();
    }


}
