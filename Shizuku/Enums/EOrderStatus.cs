namespace Shizuku.Enums
{
    public enum OrderStatus
    {
        Pending = 1,        // 未付款
        Paid = 2,           // 已付款 (準備出貨)
        Shipping = 3,       // 出貨中
        Delivered = 4,      // 已送達
        Cancelled = 5,      // 已取消
        PendingRefund = 6,  // 待退款
        Refunded = 7        // 已退款
    }
}
