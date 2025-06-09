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

    // 報表查詢主畫面，支援條件查詢、分頁
    public async Task<IActionResult> Index(string month, string status, string customer, int page = 1)
    {
        int pageSize = 10;
        page = page < 1 ? 1 : page;

        // 預設查詢當月
        if (string.IsNullOrEmpty(month))
            month = DateTime.Now.ToString("yyyy-MM");

        // 將 yyyy-MM 格式轉成 DateTime 作為查詢起始日
        if (!DateTime.TryParseExact($"{month}-01", "yyyy-MM-dd", null,
            System.Globalization.DateTimeStyles.None, out var startDate))
        {
            return View(new List<Shipment>());
        }

        var endDate = startDate.AddMonths(1);

        // 查詢起始日到下個月之前的出貨資料，含目的地資料
        var query = _context.Shipments
            .Include(s => s.Destinations)
            .Where(s => s.ShipmentDate >= startDate && s.ShipmentDate < endDate);

        // 若有選擇配送狀態，則過濾目的地並只取對應狀態的
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DeliveryStatus>(status, out var parsedStatus))
        {
            query = query
                .Where(s => s.Destinations.Any(d => d.Status == parsedStatus))
                .Select(s => new Shipment
                {
                    Id = s.Id,
                    ShipmentDate = s.ShipmentDate,
                    Destinations = s.Destinations
                        .Where(d => d.Status == parsedStatus)
                        .ToList()
                });
        }

        // 若有輸入客戶名稱，模糊搜尋
        if (!string.IsNullOrEmpty(customer))
        {
            query = query.Where(s => s.Destinations.Any(d =>
                d.CustomerName.Contains(customer)));
        }

        // 計算分頁資訊
        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        totalPages = totalPages < 1 ? 1 : totalPages;
        page = Math.Min(page, totalPages);

        // 分頁取資料
        var shipments = await query
            .OrderByDescending(s => s.ShipmentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 傳遞查詢參數與頁面資訊至 View
        ViewData["Month"] = month;
        ViewData["Status"] = status;
        ViewData["Customer"] = customer;
        ViewData["TotalRecords"] = totalRecords;
        ViewData["TotalPages"] = totalPages;
        ViewData["CurrentPage"] = page;
        ViewData["PageSize"] = pageSize;

        return View(shipments);
    }

    // 狀態轉中文的對應方法
    private string ConvertStatusToChinese(DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.Pending => "待送達",
            DeliveryStatus.Delivered => "已送達",
            DeliveryStatus.Skipped => "跳過",
            _ => status.ToString()
        };
    }

    // 匯出 Excel 時用轉換後的中文
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

                // 標題
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
                        worksheet.Cells[row, 3].Value = ConvertStatusToChinese(dest.Status); // 這裡轉成中文
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

    // 報表頁面中刪除單一目的地（AJAX 呼叫）
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
