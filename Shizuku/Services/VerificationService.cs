using Microsoft.EntityFrameworkCore;
using Shizuku.Models;

public class VerificationService
{
    private readonly DbShizukuDemoContext _context;
    public VerificationService(DbShizukuDemoContext context) { _context = context; }

    // 修改產碼邏輯：產 6 位數字
    public async Task<string> CreateEmailVerificationAsync(int memberId)
    {
        // 1. 作廢舊驗證碼
        var oldRecords = await _context.TMemberVerifications
            .Where(v => v.FMemberId == memberId && v.FType == 1 && v.FIsUsed == false)
            .ToListAsync();
        oldRecords.ForEach(r => r.FIsUsed = true);

        // 2. 產生 6 位隨機數字
        string code = new Random().Next(100000, 999999).ToString();

        var newVerify = new TMemberVerification
        {
            FMemberId = memberId,
            FCode = code, // 存入 6 位數字
            FType = 1,
            FExpireTime = DateTime.Now.AddMinutes(10), // 數字驗證碼通常時效較短 (例如 10 分鐘)
            FAttemptCount = 0,
            FIsUsed = false,
            FCreatedTime = DateTime.Now
        };

        _context.TMemberVerifications.Add(newVerify);
        await _context.SaveChangesAsync();
        return code;
    }

    // 修改驗證邏輯：需要傳入 memberId 與 code
    public async Task<bool> VerifyCodeAsync(int memberId, string code)
    {
        var record = await _context.TMemberVerifications
            .Where(v => v.FMemberId == memberId && v.FCode == code && v.FType == 1)
            .OrderByDescending(v => v.FCreatedTime) // 抓最新的一筆
            .FirstOrDefaultAsync();

        if (record == null) throw new Exception("驗證碼錯誤。");
        if (record.FIsUsed == true) throw new Exception("此驗證碼已失效。");
        if (record.FExpireTime < DateTime.Now) throw new Exception("驗證碼已過期。");

        record.FIsUsed = true;

        // 更新會員狀態
        var member = await _context.TMembers.FindAsync(memberId);
        if (member != null)
        {
            member.FLevel = 1; // 假設 1 是已驗證
            member.FAccessFailedCount = 0;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}