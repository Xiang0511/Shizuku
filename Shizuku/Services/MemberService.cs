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
        private readonly VerificationService _verificationService;

        public MemberService(DbShizukuDemoContext context, IMemoryCache cache, VerificationService verificationService)
        {
            _context = context; 
            _cache = cache;
            _verificationService= verificationService;
        }

        //登入
        public async Task<ApiResponse<MemberLoginResponseDto>> LoginAsync(MemberLoginRequestDto dto)
        {
            var member = await _context.TMembers
                .FirstOrDefaultAsync(m => m.FEmail == dto.FEmail);

            // 1. 找不到帳號，直接回傳錯誤
            if (member == null)
            {
                return new ApiResponse<MemberLoginResponseDto> { Success = false, Message = "帳號或密碼錯誤" };
            }

            int captchaThreshold = 3;
            int lockThreshold = 6;

            // 2. 檢查帳號是否已經被鎖定或停用
            if (member.FIsActive == false)
            {
                // 即使被鎖定，繼續嘗試登入依然要 count + 1
                member.FAccessFailedCount = (member.FAccessFailedCount ?? 0) + 1;
                _context.TMembers.Update(member);
                await _context.SaveChangesAsync();

                return new ApiResponse<MemberLoginResponseDto>
                {
                    Success = false,
                    Message = "您的帳號已被鎖定或停用，請聯繫客服人員處理。"
                };
            }

            // 3. 檢查是否需要驗證碼
            bool isCaptchaRequired = member.FAccessFailedCount >= captchaThreshold;

            if (isCaptchaRequired)
            {
                if (string.IsNullOrEmpty(dto.CaptchaAnswer) || !await ValidateCaptchaAsync(dto.CaptchaId, dto.CaptchaAnswer))
                {
                    // 驗證碼打錯，count + 1
                    member.FAccessFailedCount = (member.FAccessFailedCount ?? 0) + 1;

                    // 檢查加完這一次之後，有沒有剛好觸發硬鎖定門檻
                    if (member.FAccessFailedCount >= lockThreshold)
                    {
                        member.FIsActive = false;
                    }

                    _context.TMembers.Update(member);
                    await _context.SaveChangesAsync();

                    string captchaErrorMessage = member.FIsActive == false
                        ? "錯誤次數已達上限，帳號已被鎖定，請聯繫客服人員處理。"
                        : "圖形驗證碼輸入錯誤，請重新輸入";

                    return new ApiResponse<MemberLoginResponseDto>
                    {
                        Success = false,
                        Message = captchaErrorMessage
                    };
                }
            }

            // 4. 驗證密碼
            bool isPasswordValid = member.FPassword == dto.FPassword;

            if (!isPasswordValid)
            {
                // 密碼錯誤，count + 1
                member.FAccessFailedCount = (member.FAccessFailedCount ?? 0) + 1;

                string returnMessage;

                if (member.FAccessFailedCount >= lockThreshold)
                {
                    member.FIsActive = false;
                    returnMessage = "密碼錯誤次數已達上限，帳號已被鎖定，請聯繫客服人員處理。";
                }
                else if (member.FAccessFailedCount >= captchaThreshold)
                {
                    returnMessage = "電子信箱或密碼輸入錯誤，下次登入請輸入驗證碼。";
                }
                else
                {
                    returnMessage = "電子信箱或密碼輸入錯誤。";
                }

                _context.TMembers.Update(member);
                await _context.SaveChangesAsync();

                return new ApiResponse<MemberLoginResponseDto> { Success = false, Message = returnMessage };
            }

            // 5. 只有所有驗證完全通過，才重設為 0
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
                FPhone = member.FPhone,
                FLevel=member.FLevel,
                FPoints=member.FPoints
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
                FAccessFailedCount = 0,
                FPoints=1000
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

        // 1. 生成安全驗證碼
        public async Task<ApiResponse<string>> GenerateSecurityCodeAsync(int memberId, string inputEmail)
        {
            var member = await _context.TMembers.FindAsync(memberId);
            if (member == null)
            {
                return new ApiResponse<string> { Success = false, Message = "找不到該會員" };
            }

            // 安全防禦：輸入的 Email 是否為該會員綁定的帳號
            if (!string.Equals(member.FEmail, inputEmail, StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResponse<string> { Success = false, Message = "輸入的 Email 與目前登入帳號不相符" };
            }

            // 呼叫現有的驗證碼服務生成 6 位數驗證碼 (預期內部效期為 10 分鐘)
            // 這裡傳入 member.FId，如果 VerificationService 需要，請配合原有的結構
            string code = await _verificationService.CreateEmailVerificationAsync(member.FId);

            // 同時放一份在 Cache 加強步驟 3 的安全校驗（防網頁繞過）
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            _cache.Set($"PhoneChangeVerifyPassed_{memberId}", code, cacheOptions);

            return new ApiResponse<string> { Success = true, Message = "成功", Data = code };
        }

        // 2. 驗證安全驗證碼
        public async Task<ApiResponse<string>> VerifySecurityCodeAsync(int memberId, string code)
        {
            // 呼叫你的驗證服務比對 (這裡可以沿用註冊時驗證資料庫或快取的邏輯)
            // 假設你是用一組獨立的機制比對，以下為一般邏輯範例：
            if (_cache.TryGetValue($"PhoneChangeVerifyPassed_{memberId}", out string? savedCode))
            {
                if (string.Equals(savedCode, code, StringComparison.Ordinal))
                {
                    return new ApiResponse<string> { Success = true, Message = "驗證通過" };
                }
            }

            return new ApiResponse<string> { Success = false, Message = "驗證碼不正確或已過期" };
        }

        // 3. 實際寫入資料庫並保存
        public async Task<ApiResponse<string>> UpdatePhoneAsync(int memberId, string newPhone, string verifiedCode)
        {
            // 雙重保險：確認快取內真的有這筆通過紀錄，且代碼一致，防止直接呼叫 API 闖入
            if (!_cache.TryGetValue($"PhoneChangeVerifyPassed_{memberId}", out string? savedCode) ||
                !string.Equals(savedCode, verifiedCode, StringComparison.Ordinal))
            {
                return new ApiResponse<string> { Success = false, Message = "安全權杖錯誤或失效，請重新進行首步驗證" };
            }

            var member = await _context.TMembers.FindAsync(memberId);
            if (member == null)
            {
                return new ApiResponse<string> { Success = false, Message = "找不到該會員" };
            }

            // 開始變更手機號碼
            member.FPhone = newPhone;
            member.FReceiverPhone = newPhone; // 同步改預設收件人手機
            member.FUpdatedTime = DateTime.Now;

            _context.TMembers.Update(member);
            var isSaved = await _context.SaveChangesAsync() > 0;

            if (isSaved)
            {
                // 變更成功後清除快取金鑰
                _cache.Remove($"PhoneChangeVerifyPassed_{memberId}");

                return new ApiResponse<string> { Success = true, Message = "手機號碼修改成功" };
            }

            return new ApiResponse<string> { Success = false, Message = "手機號碼變更失敗，無資料更動" };
        }
    }
}