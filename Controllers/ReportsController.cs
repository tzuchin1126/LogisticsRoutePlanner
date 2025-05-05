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

    public async Task<IActionResult> Index(string month, string status, string customer, int page = 1)
    {
        // 參數初始化
        int pageSize = 10;
        page = page < 1 ? 1 : page; // 確保頁數不小於1

        // 月份處理
        if (string.IsNullOrEmpty(month))
            month = DateTime.Now.ToString("yyyy-MM");

        if (!DateTime.TryParseExact($"{month}-01", "yyyy-MM-dd", null, 
            System.Globalization.DateTimeStyles.None, out var startDate))
        {
            return View(new List<Shipment>());
        }

        var endDate = startDate.AddMonths(1);

        // 基礎查詢
        var query = _context.Shipments
            .Include(s => s.Destinations)
            .Where(s => s.ShipmentDate >= startDate && s.ShipmentDate < endDate);

        // 狀態過濾
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

        // 客戶過濾（如果啟用）
        if (!string.IsNullOrEmpty(customer))
        {
            query = query.Where(s => s.Destinations.Any(d => 
                d.CustomerName.Contains(customer)));
        }

        // 分頁處理
        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        page = Math.Min(page, totalPages); // 確保不超過總頁數

        var shipments = await query
            .OrderByDescending(s => s.ShipmentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 傳遞參數到視圖
        ViewData["Month"] = month;
        ViewData["Status"] = status;
        ViewData["Customer"] = customer;
        ViewData["TotalRecords"] = totalRecords;
        ViewData["TotalPages"] = totalPages;
        ViewData["CurrentPage"] = page;
        ViewData["PageSize"] = pageSize;

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
