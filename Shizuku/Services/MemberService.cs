using Shizuku.Models;
using Serilog;
namespace Shizuku.Services
{
    public class MemberService
    {
        private readonly DbShizukuDemoContext _context;

        public MemberService(DbShizukuDemoContext context)
        {
            _context = context;
        }

        public TMember Login(string email, string password)
        {
            Log.Information("顯目的東西");
            
            var member = _context.TMembers.FirstOrDefault(m => m.FEmail == email);

            if (member != null && member.FPassword == password)
            {
                return member;
            }

            return null;
        }
    }
}
