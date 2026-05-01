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

        public virtual TMember Login(string email, string password)
        {   
            var member = _context.TMembers.FirstOrDefault(m => m.FEmail == email);

            if (member != null && member.FPassword == password)
            {
                return member;
            }

            return null;
        }
    }
}
