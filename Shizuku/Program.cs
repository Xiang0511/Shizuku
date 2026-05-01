using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Shizuku.Infrastructure.Filters.Shizuku.Infrastructure.Filters;
using Shizuku.Infrastructure.Logging;
using Shizuku.Models;
using Shizuku.Services;
using Shizuku.Infrastructure.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog 設定 (放在最前面，確保啟動過程也被紀錄)
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

builder.Host.UseSerilog();

// 2. 基礎服務註冊 (DbContext 只留這一個)
builder.Services.AddDbContext<DbShizukuDemoContext>(options =>
    options.UseSqlServer(connectionString));

// 3. CORS 設定 (只留這一個)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddControllersWithViews(); // 包含 API 和 Razor View 支援

// 4. Autofac 設定
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterType<LogInterceptor>();

    // 批次註冊 Service
    containerBuilder.RegisterAssemblyTypes(typeof(Program).Assembly)
        .Where(t => t.Name.EndsWith("Service"))
        .PublicOnly()
        .EnableClassInterceptors()
        .InterceptedBy(typeof(LogInterceptor));

    // 批次註冊 Controller
    containerBuilder.RegisterAssemblyTypes(typeof(Program).Assembly)
        .Where(t => t.Name.EndsWith("Controller"))
        .EnableClassInterceptors()
        .InterceptedBy(typeof(LogInterceptor));
});

var app = builder.Build();

// --- 中間件順序 (這很重要) ---

// 第一名：全域錯誤捕捉，要包住所有人
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 第二名：紀錄 HTTP 請求 (放在 Routing 之前或之後皆可，通常放這裡)
app.UseSerilogRequestLogging();

app.UseRouting();
app.UseCors("AllowVue");
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();