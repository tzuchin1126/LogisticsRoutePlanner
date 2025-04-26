// using LogisticsRoutePlanner.Helpers;
// using OfficeOpenXml; // 確保已引用此命名空間
// using LogisticsRoutePlanner.Data;
// using Microsoft.EntityFrameworkCore;

// var builder = WebApplication.CreateBuilder(args);

// // 設定 EPPlus 授權為非商業用途（個人用途）
// ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// // 或者，如果是非商業組織
// // EPPlusLicense.SetNonCommercialOrganization("YourOrganizationName");

// builder.Services.AddDbContext<LogisticsDbContext>(options =>
//     options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
//     new MySqlServerVersion(new Version(8, 0, 23))));

// builder.Services.AddControllersWithViews();




using LogisticsRoutePlanner.Data;
using LogisticsRoutePlanner.Helpers;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml; // 確保已引用此命名空間

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

builder.Services.AddDbContext<LogisticsDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    new MySqlServerVersion(new Version(8, 0, 23))));


// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// 在 Program.cs 或 Startup.cs 加入這段初始化
GeocodingHelper.Initialize(builder.Configuration);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Shipments}/{action=Index}/{id?}");

app.Run();