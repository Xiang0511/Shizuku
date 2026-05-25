using Microsoft.EntityFrameworkCore;
using Shizuku.DTOs;
using Shizuku.Models;

namespace Shizuku.Services
{
    public class SystemLogsService
    {
        private readonly DbShizukuDemoContext _context;

        public SystemLogsService(DbShizukuDemoContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SystemLogResponseDto>> GetSystemLogsAsync(int skip, int take)
        {
            return await _context.SystemLogs
                .AsNoTracking()
                .OrderByDescending(log => log.Timestamp)
                .Skip(skip)
                .Take(take)
                .Select(log => new SystemLogResponseDto
                {
                    Timestamp = log.Timestamp,
                    Level = log.Level,
                    Message = log.Message
                })
                .ToListAsync();
        }
    }
}
