using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Shizuku.Models;
using Shizuku.Services;    //  ProductService 所在的命名空間

var builder = WebApplication.CreateBuilder(args);

// --- 取得連線字串（只取一次，後面共用）---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// --- 註冊 MVC ---
builder.Services.AddControllersWithViews();

// --- 註冊資料庫（只寫一次）---
builder.Services.AddDbContext<DbShizukuDemoContext>(options =>
    options.UseSqlServer(connectionString));

// --- 註冊 Service ---
builder.Services.AddScoped<ProductService>();

// --- 設定 Serilog ---
var sinkOptions = new MSSqlServerSinkOptions
{
    TableName = "SystemLogs",
    AutoCreateSqlTable = true
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
    .WriteTo.MSSqlServer(connectionString, sinkOptions)
    .CreateLogger();

builder.Host.UseSerilog();

// --- 建立 App ---
var app = builder.Build();

// --- 設定 Middleware Pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();    //  建議放在 UseRouting 之前，才能紀錄完整請求
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();