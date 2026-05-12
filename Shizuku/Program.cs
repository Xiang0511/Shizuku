using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Shizuku.Models;
using Shizuku.Services;
using Shizuku.Hubs;

// 啟用 Serilog 內部除錯，這行非常重要！
// 如果資料庫連線失敗，錯誤會顯示在 Output 視窗
Serilog.Debugging.SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine(msg));

try
{
    var builder = WebApplication.CreateBuilder(args);
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // --- 1. Serilog 配置 ---
    var sinkOptions = new MSSqlServerSinkOptions
    {
        TableName = "SystemLogs",
        AutoCreateSqlTable = true
    };

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
        // 建議加上 .Async()，避免資料庫連線不穩時卡住後端啟動
        .WriteTo.Async(a => a.MSSqlServer(connectionString, sinkOptions))
        .WriteTo.Debug()
        .WriteTo.Console()
        .CreateLogger();

    builder.Host.UseSerilog();

// --- 2. 註冊基礎服務 ---
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllersWithViews();
builder.Services.AddSwaggerGen();

    builder.Services.AddDbContext<DbShizukuDemoContext>(options =>
        options.UseSqlServer(connectionString));

    // --- 3. 設定 CORS ---
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
                  .AllowCredentials();
        });
    });

// --- 4. 註冊自定義服務 (DI) ---
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddHttpClient<LinePayService>();
builder.Services.AddScoped<ProductService>();



// 加入這行，讓系統載入 SignalR 的相關功能
builder.Services.AddSignalR();
var app = builder.Build();

    // --- 5. 中間件順序 ---
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseSerilogRequestLogging(); // 紀錄 API 請求日誌
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseCors("AllowAll");
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // 加入這行，設定對外開放的 WebSocket 通道網址為 /chatHub
    app.MapHub<ChatHub>("/chatHub");

    Log.Information("應用程式正在啟動...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "應用程式啟動失敗！");
}
finally
{
    Log.CloseAndFlush(); // 確保日誌完整寫入
}