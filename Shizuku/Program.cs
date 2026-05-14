using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Shizuku.Models;
using Shizuku.Services;
using Shizuku.Hubs;
using Shizuku.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// 啟用 Serilog 內部除錯
Serilog.Debugging.SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine(msg));

try
{
    var builder = WebApplication.CreateBuilder(args);
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // JWT: 取得設定檔中的 JWT 資訊
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

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
        .WriteTo.Async(a => a.MSSqlServer(connectionString, sinkOptions))
        .WriteTo.Debug()
        .WriteTo.Console()
        .CreateLogger();

    builder.Host.UseSerilog();

    // --- 2. 註冊基礎服務 ---
    builder.Services.AddControllersWithViews();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddDbContext<DbShizukuDemoContext>(options =>
        options.UseSqlServer(connectionString));

    // --- 3. 設定 CORS ---
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // --- 4. JWT 驗證服務設定 ---
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // 恢復驗證發行者
            ValidateAudience = true, // 恢復驗證接收者
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

    // --- 5. 註冊自定義服務 (DI) ---
    builder.Services.AddScoped<OrderService>();
    builder.Services.AddScoped<MemberService>();
    builder.Services.AddHttpClient<LinePayService>();
    builder.Services.AddScoped<ProductService>();
    builder.Services.AddScoped<JwtHelper>();
    builder.Services.AddScoped<EmailService>();
    builder.Services.AddScoped<VerificationService>();

    builder.Services.AddSignalR();

    var app = builder.Build();

    // --- 6. 中間件順序 (Pipeline) ---
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

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseCors("AllowAll"); // CORS 必須在 Routing 之前

    app.UseRouting();

    app.UseAuthentication(); // 認證：你是誰
    app.UseAuthorization();  // 授權：你能做什麼

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

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
    Log.CloseAndFlush();
}