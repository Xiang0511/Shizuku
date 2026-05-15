using Microsoft.EntityFrameworkCore;
using Serilog;
using Shizuku.DTOs; // 引入 DTOs 命名空間
using Shizuku.Models;
using System.Text.Json;

namespace Shizuku.Services
{
    public class MemberService
    {
        private readonly DbShizukuDemoContext _context;

        public MemberService(DbShizukuDemoContext context)
        {
            _context = context;
        }

        //登入
        public async Task<MemberLoginResponseDto> LoginAsync(string email, string password)
        {
            var loginResult = await _context.TMembers
                .Where(m => m.FEmail == email && m.FPassword == password)
                .Select(m => new MemberLoginResponseDto
                {
                    FId=m.FId,
                    FName = m.FName,
                    FEmail = m.FEmail,
                    FGender = m.FGender,
                    FBirthday = m.FBirthday,
                    FPhone = m.FPhone
                })
                .FirstOrDefaultAsync(); // 使用非同步方法

            return loginResult;
        }

        //註冊
        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.TMembers.AnyAsync(m => m.FEmail == email);
        }

        public async Task<MemberRegisterResponseDto?> RegisterAsync(MemberRegisterRequestDto dto)
        {
            // 1. 建立實體並填入初始資料
            var newMember = new TMember
            {
                FName = dto.FName,
                FEmail = dto.FEmail,
                FAccount = dto.FEmail, // 帳號同 Email
                FPassword = dto.FPassword,
                FCreatedTime = DateTime.Now,
                FUpdatedTime = DateTime.Now,
                FIsActive = true,
                FLevel = 0,
                FGender = dto.FGender, // 補上預設性別 女裝 所以是女性
                //FImage = "default.jpg" // 補上預設圖片 先不要好了
                FReceiverName = dto.FName,
                FPhone = dto.FPhone,
                FReceiverPhone = dto.FPhone,
            };

            _context.TMembers.Add(newMember);

            // 2. 第一次 SaveChanges 取得 Identity ID
            await _context.SaveChangesAsync();

            // 3. 處理 fMemberId (M0 + ID)
            newMember.FMemberId = $"M0{newMember.FId}";

            // 4. 第二次 SaveChanges 更新代主鍵
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                // 5. 轉換成 Response DTO 回傳
                return new MemberRegisterResponseDto
                {
                    FId = newMember.FId,
                    FMemberId = newMember.FMemberId,
                    FName = newMember.FName,
                    FEmail = newMember.FEmail,
                    FPhone = newMember.FPhone,
                    FBirthday = newMember.FBirthday,
                    FCreatedTime = newMember.FCreatedTime
                };
            }

            return null;
        }

        //地址查詢
        public async Task<List<MemberAddressDto>> GetAddressesAsync(int memberId)
        {
            var member = await _context.TMembers.FindAsync(memberId);
            if (member == null || string.IsNullOrEmpty(member.FReceiverAddress))
            {
                return new List<MemberAddressDto>();
            }

            // 反序列化：字串 -> 物件清單
            return JsonSerializer.Deserialize<List<MemberAddressDto>>(member.FReceiverAddress) ?? new List<MemberAddressDto>();
        }

        //更新地址
        public async Task<bool> UpdateAddressesAsync(int memberId, List<MemberAddressDto> addresses)
        {
            var member = await _context.TMembers.FindAsync(memberId);
            if (member == null) return false;

            // 序列化：物件清單 -> 字串
            member.FReceiverAddress = JsonSerializer.Serialize(addresses);

            // 同步更新外層的預設收件人（可選邏輯）
            var defaultAddr = addresses.FirstOrDefault(a => a.fIsDefault);
            if (defaultAddr != null)
            {
                member.FReceiverName = defaultAddr.fReceiverName;
                member.FReceiverPhone = defaultAddr.fReceiverPhone;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        //更新個人資料
        public async Task<bool> UpdateProfileAsync(MemberEditRequestDto dto)
        {
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.FId == dto.FId);

            if (member == null) return false;

            // 僅更新名稱與性別
            member.FName = dto.FName;
            member.FGender = dto.FGender;
            member.FUpdatedTime = DateTime.Now;

            // 如果你有連動收件人名稱，也可以順便改
            member.FReceiverName = dto.FName;

            return await _context.SaveChangesAsync() > 0;
        }
    }
}