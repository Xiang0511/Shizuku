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

        public IActionResult Index(string? level) // 新增參數
        {
            // 1. 先建立查詢基底 (這就是老師說的，在記憶體中先準備好 Query)
            var query = _context.SystemLogs.AsQueryable();

            // 2. 判斷有沒有要篩選等級 (例如：點選了 Error)
            if (!string.IsNullOrEmpty(level))
            {
                query = query.Where(l => l.Level == level);
            }

            // 3. 執行查詢
            var logs = query
                .OrderByDescending(l => l.Timestamp)
                .Take(100) // 既然有篩選，我們可以多看幾筆 (例如 100 筆)
                .Select(l => new LogViewModel
                {
                    Id = l.Id,
                    // 修正：確保顯示的是台灣時間
                    Timestamp = l.Timestamp.DateTime,
                    Level = l.Level,
                    Message = l.Message,
                    Exception = l.Exception,
                    Properties = l.Properties
                }).ToList();

            // 把目前的等級存進 ViewBag，讓 View 的下拉選單可以「定住」在那個選項
            ViewBag.CurrentLevel = level;

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
