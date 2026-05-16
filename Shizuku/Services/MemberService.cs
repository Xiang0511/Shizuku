using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Shizuku.DTOs; // 引入 DTOs 命名空間
using Shizuku.Models;
using System.Text.Json;

namespace Shizuku.Services
{
    public class MemberService
    {
        private readonly DbShizukuDemoContext _context;
        private readonly IMemoryCache _cache; 

        public MemberService(DbShizukuDemoContext context, IMemoryCache cache)
        {
            _context = context; 
            _cache = cache;
        }

        //登入
        public async Task<ApiResponse<MemberLoginResponseDto>> LoginAsync(MemberLoginRequestDto dto)
        {
            var member = await _context.TMembers
                .FirstOrDefaultAsync(m => m.FEmail == dto.FEmail);

            if (member == null)
            {
                return new ApiResponse<MemberLoginResponseDto> { Success = false, Message = "帳號或密碼錯誤" };
            }

            int captchaThreshold = 3;

            bool isCaptchaRequired = member.FAccessFailedCount >= captchaThreshold;

            if (isCaptchaRequired)
            {
                if (string.IsNullOrEmpty(dto.CaptchaAnswer) || !await ValidateCaptchaAsync(dto.CaptchaId, dto.CaptchaAnswer))
                {
                    return new ApiResponse<MemberLoginResponseDto>
                    {
                        Success = false,
                        Message = "圖形驗證碼輸入錯誤，請重新輸入"
                    };
                }
            }

            bool isPasswordValid = member.FPassword == dto.FPassword;

            if (!isPasswordValid)
            {
                member.FAccessFailedCount = (member.FAccessFailedCount ?? 0) + 1;
                _context.TMembers.Update(member);
                await _context.SaveChangesAsync();

                string returnMessage = member.FAccessFailedCount >= captchaThreshold
                    ? "電子信箱或密碼輸入錯誤，下次登入請輸入驗證碼"
                    : "電子信箱或密碼輸入錯誤";

                return new ApiResponse<MemberLoginResponseDto> { Success = false, Message = returnMessage };
            }

            if (member.FAccessFailedCount > 0)
            {
                member.FAccessFailedCount = 0;
                _context.TMembers.Update(member);
                await _context.SaveChangesAsync();
            }

            var loginResult = new MemberLoginResponseDto
            {
                FId = member.FId,
                FName = member.FName,
                FEmail = member.FEmail,
                FGender = member.FGender,
                FBirthday = member.FBirthday,
                FPhone = member.FPhone
            };

            return new ApiResponse<MemberLoginResponseDto>
            {
                Success = true,
                Message = "登入成功",
                Data = loginResult
            };
        }

        private async Task<bool> ValidateCaptchaAsync(string? id, string? answer)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(answer)) return false;

            // 組合出存放在 Cache 裡的 Key
            string cacheKey = $"Captcha_{id}";

            // 去快取撈出正確答案
            if (_cache.TryGetValue(cacheKey, out string? correctAnswer))
            {
                // 撈出來後，不論對錯都立刻把快取刪除（防止同一個驗證碼被重複暴力嘗試）
                _cache.Remove(cacheKey);

                // 比對答案（忽略大小寫）
                return string.Equals(correctAnswer, answer, StringComparison.OrdinalIgnoreCase);
            }

            return false; // 找不到代表過期了或不存在
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
                FAccessFailedCount = 0
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