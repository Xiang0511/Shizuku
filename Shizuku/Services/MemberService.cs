using Google.Apis.Auth;
using Humanizer;
using Microsoft.AspNetCore.Identity;
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
        private readonly IWebHostEnvironment _environment;

        public MemberService(
            DbShizukuDemoContext context, 
            IMemoryCache cache, 
            VerificationService verificationService, 
            IWebHostEnvironment environment)
        {
            _context = context; 
            _cache = cache;
            _verificationService = verificationService;
            _environment = environment;
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
            var passwordHasher = new PasswordHasher<TMember>();
            var verificationResult = passwordHasher.VerifyHashedPassword(member, member.FPassword ?? "", dto.FPassword);

            bool isPasswordValid = verificationResult == PasswordVerificationResult.Success;

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
                FPoints=member.FPoints,
                FImage=member.FImage
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

            var passwordHasher = new PasswordHasher<TMember>();
            newMember.FPassword = passwordHasher.HashPassword(newMember, dto.FPassword);

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

        // 1. 生成安全驗證碼（手機、生日共用）
        public async Task<ApiResponse<string>> GenerateSecurityCodeAsync(int memberId, string inputEmail, int type)
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
            string code = await _verificationService.CreateEmailVerificationAsync(member.FId);

            // 依據型態決定 Cache Key，1 為手機，其餘（2）為生日
            string cacheKey = type switch
            {
                1 => $"PhoneChangeVerifyPassed_{memberId}",
                2 => $"BirthdayChangeVerifyPassed_{memberId}",
                3 => $"PasswordChangeVerifyPassed_{memberId}", // 新增 Type 3 的 Key
                _ => throw new ArgumentException("未支援的安全變更類型")
            };

            // 同時放一份在 Cache 加強步驟 3 的安全校驗（防網頁繞過）
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            _cache.Set(cacheKey, code, cacheOptions);

            return new ApiResponse<string> { Success = true, Message = "成功", Data = code };
        }

        // 2. 驗證安全驗證碼（手機、生日共用）
        public async Task<ApiResponse<string>> VerifySecurityCodeAsync(int memberId, string code, int type)
        {
            // 依據型態尋找對應的 Cache Key
            string cacheKey = type switch
            {
                1 => $"PhoneChangeVerifyPassed_{memberId}",
                2 => $"BirthdayChangeVerifyPassed_{memberId}",
                3 => $"PasswordChangeVerifyPassed_{memberId}", // 新增 Type 3 的 Key
                _ => throw new ArgumentException("未支援的安全變更類型")
            };

            if (_cache.TryGetValue(cacheKey, out string? savedCode))
            {
                if (string.Equals(savedCode, code, StringComparison.Ordinal))
                {
                    return new ApiResponse<string> { Success = true, Message = "驗證通過" };
                }
            }

            return new ApiResponse<string> { Success = false, Message = "驗證碼不正確或已過期" };
        }

        // 3. 實際寫入資料庫並保存手機
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

        // 3. 實際寫入資料庫並保存生日
        public async Task<ApiResponse<string>> UpdateBirthdayAsync(int memberId, DateOnly newBirthday, string verifiedCode)
        {
            // 雙重保險：確認生日快取權杖正確
            if (!_cache.TryGetValue($"BirthdayChangeVerifyPassed_{memberId}", out string? savedCode) ||
                !string.Equals(savedCode, verifiedCode, StringComparison.Ordinal))
            {
                return new ApiResponse<string> { Success = false, Message = "安全權杖錯誤或失效，請重新進行首步驗證" };
            }

            var member = await _context.TMembers.FindAsync(memberId);
            if (member == null)
            {
                return new ApiResponse<string> { Success = false, Message = "找不到該會員" };
            }

            // 開始變更生日
            member.FBirthday = newBirthday;
            member.FUpdatedTime = DateTime.Now;

            _context.TMembers.Update(member);
            var isSaved = await _context.SaveChangesAsync() > 0;

            if (isSaved)
            {
                // 變更成功後清除生日快取
                _cache.Remove($"BirthdayChangeVerifyPassed_{memberId}");

                return new ApiResponse<string> { Success = true, Message = "生日修改成功" };
            }

            return new ApiResponse<string> { Success = false, Message = "生日變更失敗，無資料更動" };
        }

        // 3. 實際寫入資料庫並保存新密碼 (含二次密碼與安全權杖校驗)
        public async Task<ApiResponse<string>> UpdatePasswordAsync(int memberId, string newPassword, string confirmPassword, string verifiedCode)
        {
            // 二次密碼後端防線：確認兩次輸入一致
            if (newPassword != confirmPassword)
            {
                return new ApiResponse<string> { Success = false, Message = "兩次輸入的新密碼不一致" };
            }

            // 雙重保險：確認密碼快取權杖正確，防止網頁繞過
            if (!_cache.TryGetValue($"PasswordChangeVerifyPassed_{memberId}", out string? savedCode) ||
                !string.Equals(savedCode, verifiedCode, StringComparison.Ordinal))
            {
                return new ApiResponse<string> { Success = false, Message = "安全權杖錯誤或失效，請重新進行首步驗證" };
            }

            var member = await _context.TMembers.FindAsync(memberId);
            if (member == null)
            {
                return new ApiResponse<string> { Success = false, Message = "找不到該會員" };
            }

            // 開始變更密碼 (注意：這裡請換成你專案實際使用的密碼雜湊/加密演算法，例如 BCrypt 或 Salt+SHA256)
            // 範例：member.FPassword = _passwordHasher.Hash(newPassword);

            var passwordHasher = new PasswordHasher<TMember>();
            member.FPassword = passwordHasher.HashPassword(member, newPassword);

            member.FUpdatedTime = DateTime.Now;

            _context.TMembers.Update(member);
            var isSaved = await _context.SaveChangesAsync() > 0;

            if (isSaved)
            {
                // 變更成功後清除密碼快取，防止重複使用
                _cache.Remove($"PasswordChangeVerifyPassed_{memberId}");

                return new ApiResponse<string> { Success = true, Message = "密碼修改成功，下次請使用新密碼登入" };
            }

            return new ApiResponse<string> { Success = false, Message = "密碼變更失敗，無資料更動" };
        }

        public async Task<ApiResponse<int>> GetMemberIdByEmailAsync(string email)
        {
            var member = await _context.TMembers
                .FirstOrDefaultAsync(m => m.FEmail == email); // 如果資料庫欄位是 fEmail，請改成 m.fEmail

            if (member == null)
            {
                return new ApiResponse<int> { Success = false, Message = "找不到該會員" };
            }

            return new ApiResponse<int> { Success = true, Message = "成功", Data = member.FId }; // 欄位名請依 TMembers 實際主鍵為主
        }

        public async Task<ApiResponse<string>> UploadAvatarAsync(int memberId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return new ApiResponse<string> { Success = false, Message = "請選擇要上傳的圖片" };
            }

            // 1. 檢查會員是否存在
            var member = await _context.TMembers.FindAsync(memberId);
            if (member == null)
            {
                return new ApiResponse<string> { Success = false, Message = "找不到該會員" };
            }

            try
            {
                // 2. 設定儲存資料夾 (wwwroot/uploads/avatars)
                string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // 3. 產生唯一檔名，避免重複（例如: 5a1b2c3d_profile.jpg）
                string extension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}{extension}";
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                // 4. 將檔案實際寫入伺服器硬碟
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 5. 刪除舊的大頭貼（可選：如果原本有舊圖，可以順便刪掉省空間）
                if (!string.IsNullOrEmpty(member.FImage))
                {
                    string oldFilePath = Path.Combine(_environment.WebRootPath, "uploads", "avatars", member.FImage);
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                // 6. 更新資料庫的欄位（只存檔名）
                member.FImage = uniqueFileName;
                _context.TMembers.Update(member);
                await _context.SaveChangesAsync();

                // 7. 回傳新圖片的檔名，前端可以直接拿去拼湊成圖片網址
                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "大頭貼更新成功",
                    Data = uniqueFileName
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string> { Success = false, Message = $"伺服器錯誤: {ex.Message}" };
            }
        }

        // Google 第三方登入與自動註冊業務邏輯
        public async Task<ApiResponse<MemberLoginResponseDto>> LoginWithGoogleAsync(string idToken)
        {
            try
            {
                // 1. 從 appsettings.json 讀取 Google Client ID
                var googleClientId = _context.Database.ProviderName != null
                    ? new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json")
                        .Build()["Google:ClientId"]
                    : null;

                // 註：若有透過 DI 注入 IConfiguration，亦可直接使用，此處採最安全的相容寫法。
                // 由於建構子中未注入 IConfiguration，我們可以透過從 _environment 或手動建立讀取，
                // 但更好的做法是，如果您方便，也可以在 MemberService 的建構子中注入 `IConfiguration configuration` 並讀取 `_configuration["Google:ClientId"]`。

                // 2. 設定驗證參數
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    // 若您已在 appsettings.json 中配置，請在此加入 Client ID 驗證（選填，但建議驗證）
                    // Audience = new[] { googleClientId } 
                };
                // 3. 驗證 Google ID Token
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                if (payload == null || string.IsNullOrEmpty(payload.Email))
                {
                    return new ApiResponse<MemberLoginResponseDto> { Success = false, Message = "Google 驗證失敗，無法解析用戶資訊" };
                }
                // 4. 根據 Email 尋找現有會員
                var member = await _context.TMembers.FirstOrDefaultAsync(m => m.FEmail == payload.Email);
                if (member != null)
                {
                    // 帳號已存在，檢查是否被停用
                    if (member.FIsActive == false)
                    {
                        return new ApiResponse<MemberLoginResponseDto>
                        {
                            Success = false,
                            Message = "您的帳號已被鎖定或停用，請聯繫客服人員處理。"
                        };
                    }
                    // 重設失敗次數並更新登入時間
                    member.FAccessFailedCount = 0;
                    member.FLoginTime = DateTime.Now;
                    _context.TMembers.Update(member);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // 帳號不存在，執行自動註冊流程
                    member = new TMember
                    {
                        FName = payload.Name ?? payload.GivenName ?? "Google 用戶",
                        FEmail = payload.Email,
                        FAccount = payload.Email, // 帳號預設為 Email
                        FPassword = Guid.NewGuid().ToString("N").Substring(0, 16), // 生成隨機安全密碼
                        FCreatedTime = DateTime.Now,
                        FUpdatedTime = DateTime.Now,
                        FIsActive = true, // 預設啟用
                        FLevel = 0, // 預設等級
                        FGender = 3, // 預設為未指定或其他
                        FReceiverName = payload.Name ?? "Google 用戶",
                        FAccessFailedCount = 0,
                        FPoints = 1000, // 贈送新註冊 1000 點
                        FImage = payload.Picture // 將 Google 大頭貼 URL 存入 FImage 欄位
                    };
                    _context.TMembers.Add(member);
                    await _context.SaveChangesAsync();
                    // 自動產生會員 ID：比照常規註冊機制 M0 + ID
                    member.FMemberId = $"M0{member.FId}";
                    _context.TMembers.Update(member);
                    await _context.SaveChangesAsync();
                }
                // 5. 組裝登入成功的回傳 DTO 格式
                var loginResult = new MemberLoginResponseDto
                {
                    FId = member.FId,
                    FName = member.FName,
                    FEmail = member.FEmail,
                    FGender = member.FGender,
                    FBirthday = member.FBirthday,
                    FPhone = member.FPhone,
                    FLevel = member.FLevel,
                    FPoints = member.FPoints,
                    FImage = member.FImage
                };
                return new ApiResponse<MemberLoginResponseDto>
                {
                    Success = true,
                    Message = "Google 登入成功",
                    Data = loginResult
                };
            }
            catch (InvalidJwtException)
            {
                return new ApiResponse<MemberLoginResponseDto> { Success = false, Message = "Google 金鑰驗證無效或已過期" };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Google 登入時發生未預期錯誤");
                return new ApiResponse<MemberLoginResponseDto> { Success = false, Message = "驗證過程中發生伺服器錯誤" };
            }
        }
    }
}