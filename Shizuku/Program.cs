using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Shizuku.Models;
using Shizuku.Services;
using Shizuku.Hubs;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Serilog 配置 ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var sinkOptions = new MSSqlServerSinkOptions { TableName = "SystemLogs", AutoCreateSqlTable = true };

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
    .WriteTo.MSSqlServer(connectionString, sinkOptions)
    .WriteTo.Debug()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog(); // 告知系統使用 Serilog

// --- 2. 註冊基礎服務 ---
builder.Services.AddControllersWithViews();
builder.Services.AddSwaggerGen();

// 資料庫服務 (EF Core)
builder.Services.AddDbContext<DbShizukuDemoContext>(options =>
    options.UseSqlServer(connectionString));

// --- 3. 設定 CORS ---
// --- 3. 設定 CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // 這裡必須明確寫出你的 Vue 網址，請確認 5173 是你 Vue 目前執行的 Port
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // 加入這行，這是 SignalR 順利連線的關鍵
    });
});

// --- 4. 註冊自定義服務 (DI) ---
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddHttpClient<LinePayService>();


// 加入這行，讓系統載入 SignalR 的相關功能
builder.Services.AddSignalR();
var app = builder.Build();

// --- 5. 中間件順序 (Middleware Pipeline) ---
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

// 啟動 Serilog 自動請求紀錄 (建議放在 UseStaticFiles 之前)
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// UseCors 必須在 UseRouting 之後，UseAuthorization 之前
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 加入這行，設定對外開放的 WebSocket 通道網址為 /chatHub
app.MapHub<ChatHub>("/chatHub");
app.Run();