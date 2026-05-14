using Microsoft.EntityFrameworkCore;
using Shizuku.Models;

public class VerificationService
{
    private readonly DbShizukuDemoContext _context;

    public VerificationService(DbShizukuDemoContext context)
    {
        _context = context;
    }

    // 產生驗證碼邏輯
    public async Task<string> CreateEmailVerificationAsync(int memberId)
    {
        // 作廢舊驗證碼
        var oldRecords = await _context.TMemberVerifications
            .Where(v => v.FMemberId == memberId && v.FType == 1 && v.FIsUsed == false)
            .ToListAsync();
        oldRecords.ForEach(r => r.FIsUsed = true);

        string token = Guid.NewGuid().ToString();
        var newVerify = new TMemberVerification
        {
            FMemberId = memberId,
            FCode = token,
            FType = 1,
            FExpireTime = DateTime.Now.AddHours(24),
            FAttemptCount = 0,
            FIsUsed = false,
            FCreatedTime = DateTime.Now
        };

        _context.TMemberVerifications.Add(newVerify);
        await _context.SaveChangesAsync();
        return token;
    }

    // 驗證 Token 邏輯
    public async Task<bool> VerifyEmailTokenAsync(string token)
    {
        var record = await _context.TMemberVerifications
            .FirstOrDefaultAsync(v => v.FCode == token && v.FType == 1);

        // 業務規則檢查，失敗直接丟 Exception，訊息會被 Controller 捕捉
        if (record == null) throw new Exception("找不到對應的驗證資訊。");
        if (record.FIsUsed == true) throw new Exception("此驗證連結已被使用過。");
        if (record.FExpireTime < DateTime.Now) throw new Exception("驗證連結已過期，請重新申請。");
        if (record.FAttemptCount >= 5) throw new Exception("嘗試次數過多，安全性考量已失效。");

        // 通過驗證，更新狀態
        record.FIsUsed = true;
        // TODO: 這裡通常會一併更新 TMember 表的 FIsVerified 狀態

        await _context.SaveChangesAsync();
        return true;
    }
}