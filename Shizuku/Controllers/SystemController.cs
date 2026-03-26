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

        public IActionResult Index(string? level, DateTime? startTime, DateTime? endTime) // 新增參數
        {
            // 1. 先建立查詢基底 (這就是老師說的，在記憶體中先準備好 Query)
            var query = _context.SystemLogs.AsQueryable();

            // 2. 判斷有沒有要篩選等級 (例如：點選了 Error)
            if (!string.IsNullOrEmpty(level))
            {
                query = query.Where(l => l.Level == level);
            }

            //時間區間
            if (startTime.HasValue)
            {
                query = query.Where(l => l.Timestamp >= startTime.Value);
            }
            if (endTime.HasValue)
            {
                var preciseEnd = endTime.Value.AddSeconds(59);
                query = query.Where(l => l.Timestamp <= preciseEnd);
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
            ViewBag.StartTime = startTime?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.EndTime = endTime?.ToString("yyyy-MM-ddTHH:mm");

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

        public IActionResult Export(string? level)
        {
            // 1. 根據等級撈取資料 (跟 Index 邏輯一致，保持正規化)
            var query = _context.SystemLogs.AsQueryable();
            if (!string.IsNullOrEmpty(level))
            {
                query = query.Where(l => l.Level == level);
            }

            var logs = query.OrderByDescending(l => l.Timestamp).Take(500).ToList();

            // 2. 建立文字內容 (精實格式)
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"--- 系統日誌匯出報告 ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ---");
            sb.AppendLine($"篩選等級: {(string.IsNullOrEmpty(level) ? "全部" : level)}");
            sb.AppendLine(new string('-', 50));

            foreach (var log in logs)
            {
                // 格式：[時間] [等級] 訊息內容
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");
                if (!string.IsNullOrEmpty(log.Exception))
                {
                    sb.AppendLine($"錯誤詳情: {log.Exception}");
                }
                sb.AppendLine(""); // 換行隔開
            }

            // 3. 將字串轉為位元組陣列並下載
            var fileContent = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(fileContent, "text/plain", $"SystemLogs_{DateTime.Now:yyyyMMdd_HHmm}.txt");
        }
    }
}
