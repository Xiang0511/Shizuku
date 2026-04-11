using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Shizuku.Models;
using Shizuku.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//

//

// 1. 設定 Serilog (這就是那幾行)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DbShizukuDemoContext>(options =>
    options.UseSqlServer(connectionString));

var sinkOptions = new MSSqlServerSinkOptions { TableName = "SystemLogs", AutoCreateSqlTable = true };

//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Information()
//    .WriteTo.MSSqlServer(connectionString, sinkOptions)
//.CreateLogger();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // 這裡！過濾掉微軟內建的瑣碎訊息
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
    .WriteTo.MSSqlServer(connectionString, sinkOptions)
    .CreateLogger();

builder.Host.UseSerilog(); // 告訴系統用 Serilog
///////

// ✨✨ 關鍵新增：在這裡註冊資料庫服務 ✨✨
builder.Services.AddDbContext<DbShizukuDemoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//service

builder.Services.AddScoped<MemberService>();

// 註冊 CORS 服務 
builder.Services.AddCors(options => {
    options.AddPolicy("AllowVue", policy => {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

////
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// 套用 CORS 中間件 
app.UseCors("AllowVue");

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// 2. 啟動自動請求紀錄 (這行也算在那幾行裡)
app.UseSerilogRequestLogging();
///////

app.Run();
