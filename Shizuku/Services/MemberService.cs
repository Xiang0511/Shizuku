using Microsoft.EntityFrameworkCore;
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

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.TMembers.AnyAsync(m => m.FEmail == email);
        }

        public async Task<bool> RegisterAsync(MemberRegisterDTO dto)
        {
            // 1. 建立實體並填入初始資料
            var newMember = new TMember
            {
                FName = dto.FName,
                FEmail = dto.FEmail,
                FAccount = dto.FEmail, // 帳號同 Email
                FPassword = dto.FPassword, // 建議此處加入雜湊加密邏輯
                FCreatedTime = DateTime.Now,
                FUpdatedTime = DateTime.Now,
                FIsActive = true,
                FLevel = 1,
                FWishlist = "[]",
                FReceiverAddress = "[]"
            };

            _context.TMembers.Add(newMember);

            // 2. 第一次 SaveChanges 取得 Identity ID
            await _context.SaveChangesAsync();

            // 3. 處理 fMemberId (M0 + ID)
            newMember.FMemberId = $"M0{newMember.FId}";

            // 4. 第二次 SaveChanges 更新代主鍵
            return await _context.SaveChangesAsync() > 0;
        }
    }
}