using LogisticsRoutePlanner.Data;
using LogisticsRoutePlanner.Helpers;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// 資料庫設定
builder.Services.AddDbContext<LogisticsDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    new MySqlServerVersion(new Version(8, 0, 23))));

// ✅ 控制器與 JSON 循環參考處理
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });


// 初始化 Geocoding
GeocodingHelper.Initialize(builder.Configuration);

var app = builder.Build();

// 管線設定
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 預設路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Shipments}/{action=Index}/{id?}");

app.Run();
