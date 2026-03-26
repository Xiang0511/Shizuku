using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.ViewModels;

namespace Shizuku.Controllers
{
    public class SystemController : Controller
    {
        
        private readonly DbShizukuDemoContext _context; // 假設你用 EF Core

        public SystemController(DbShizukuDemoContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 撈取最新的 50 筆 Log
            var logs = _context.SystemLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .Select(l => new LogViewModel
                {
                    Id = l.Id,
                    //Timestamp = l.Timestamp.ToOffset(TimeSpan.FromHours(8)).DateTime,
                    Timestamp = l.Timestamp.UtcDateTime, // 轉換成當地時間
                    Level = l.Level,
                    Message = l.Message,
                    Exception = l.Exception,
                    // 這裡可以預留一個解析邏輯
                    Properties = l.Properties
                }).ToList();

            return View(logs);
        }

        // GET: /System/LogList
        public IActionResult LogList(string level, string search)
        {
            // 1. 取得所有日誌，按時間倒序排（最精實的作法：看最新的）
            var logs = _context.SystemLogs.AsQueryable();

            // 2. 簡單過濾 (這就是手動正規化搜尋的第一步)
            if (!string.IsNullOrEmpty(level))
            {
                logs = logs.Where(l => l.Level == level);
            }

            if (!string.IsNullOrEmpty(search))
            {
                logs = logs.Where(l => l.Message.Contains(search));
            }

            return View(logs.OrderByDescending(l => l.Timestamp).Take(100).ToList());
        }
    }
}
