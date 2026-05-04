using Serilog;
using Shizuku.DTOs; // 引入 DTOs 命名空間
using Shizuku.Models;

namespace Shizuku.Services
{
    public class MemberService
    {
        private readonly DbShizukuDemoContext _context;

        public MemberService(DbShizukuDemoContext context)
        {
            _context = context;
        }

        public MemberLoginResponseDTO Login(string email, string password)
        {
            Log.Information("MemberService Login");
            var loginResult = _context.TMembers
                .Where(m => m.FEmail == email && m.FPassword == password)
                .Select(m => new MemberLoginResponseDTO
                {
                    FName = m.FName,
                    FEmail = m.FEmail,
                    FGender=m.FGender,
                    FBirthday=m.FBirthday,
                    FPhone=m.FPhone
                })
                .FirstOrDefault();

            return loginResult; // 若找不到符合的帳密，loginResult 會是 null
        }
    }
}