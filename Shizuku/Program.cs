using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Shizuku.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 取得連線字串
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 1. 註冊資料庫服務 (幫你清掉重複的了，留這一個就好)
builder.Services.AddDbContext<DbShizukuDemoContext>(options =>
    options.UseSqlServer(connectionString));

// 2. 設定 Serilog
var sinkOptions = new MSSqlServerSinkOptions { TableName = "SystemLogs", AutoCreateSqlTable = true };
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // 過濾掉微軟內建的瑣碎訊息
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
    .WriteTo.MSSqlServer(connectionString, sinkOptions)
    .CreateLogger();

builder.Host.UseSerilog(); // 告訴系統用 Serilog

// 3. ✨✨ 關鍵新增：註冊 CORS 政策，開門讓 Vue 進來 ✨✨
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue", policy =>
    {
        // 這裡對應你 Vue 跑起來的網址
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ✨✨ 關鍵新增：套用 CORS 政策 (注意！必須放在 UseRouting 之後，UseAuthorization 之前) ✨✨
app.UseCors("AllowVue");

app.UseAuthorization();
app.MapStaticAssets();

// 啟動自動請求紀錄
app.UseSerilogRequestLogging();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();