namespace Shizuku.Enums
{
    public enum PaymentStatus
    {
        Unpaid = 0,   // 未付款 / 待處理
        Success = 1,  // 交易成功 / 已付款
        Failed = 2,   // 交易失敗
        Refunded = 3  // 已退款
    }
}
