using Shizuku.DTOs; // 引入 DTOs 命名空間 [cite: 87]
using Shizuku.Models;
using System.Linq;

namespace Shizuku.Services
{
    public class MemberService
    {
        private readonly DbShizukuDemoContext _context;

        public MemberService(DbShizukuDemoContext context)
        {
            _context = context;
        }

        public virtual MemberLoginResponseDTO Login(string email, string password)
        {
            var loginResult = _context.TMembers
                .Where(m => m.FEmail == email && m.FPassword == password)
                .Select(m => new MemberLoginResponseDTO
                {
                    FName = m.FName
                })
                .FirstOrDefault();

            return loginResult; // 若找不到符合的帳密，loginResult 會是 null [cite: 87]
        }
    }
}