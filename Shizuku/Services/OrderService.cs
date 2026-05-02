using Shizuku.Models;
using Shizuku.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Shizuku.Services
{
    public class OrderService
    {
        // 宣告一個唯讀的私有變數，用來存放資料庫連線
        private readonly DbShizukuDemoContext _db;

        // 這是建構子 (Constructor)
        // 當 DI 容器要建立 OrderService 時，發現它需要 DbShizukuDemoContext，就會自動塞進來
        public OrderService(DbShizukuDemoContext db)
        {
            _db = db; // 把 DI 塞進來的東西，存到私有變數裡給後面的方法用
        }

        // 隨便先寫一個測試用的方法，等等給 Controller 呼叫
        public string GetTestMessage()
        {
            return "OrderService 已經成功啟動了！";
        }



        public CreateOrderResponseDto CreateOrder(CreateOrderRequestDto request)
        {
            // 1. 先檢查購物車是不是空的
            if (request.CartItems == null || request.CartItems.Count == 0)
            {
                return new CreateOrderResponseDto { IsSuccess = false, Message = "購物車是空的喔！" };
            }

            // 2. 產生一個唯一的訂單編號 (例如: ORD20260502123456)
            string newOrderNo = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");
            decimal totalAmount = 0; // 用來累加總金額

            // 3. 開啟資料庫交易 (Transaction) 防護罩！
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // 準備一個空箱子，用來裝所有的訂單明細
                    List<TOrderDetail> details = new List<TOrderDetail>();

                    // 步驟 4：跑一個 foreach 迴圈，檢查購物車裡的每一個商品
                    foreach (var item in request.CartItems)
                    {
                        // 4-1: 去 _db.TProductVariants 找出這個商品規格
                        var variant = _db.TProductVariants.FirstOrDefault(v => v.FId == item.VariantId);
                        if (variant == null)
                        {
                            throw new Exception($"找不到商品規格代碼 {item.VariantId}");
                        }

                        // 4-2: 檢查庫存 (FStock) 夠不夠？
                        if (variant.FStock < item.Quantity)
                        {
                            throw new Exception("很抱歉，部分商品庫存不足，被搶走啦！");
                        }

                        // 4-3: 庫存夠的話，馬上扣除庫存！
                        variant.FStock -= item.Quantity;

                        // 4-4: 為了拿到商品的名字，我們去 TProduct 找一下主商品資料
                        var product = _db.TProducts.FirstOrDefault(p => p.FId == variant.FProductId);
                        string productName = product != null ? product.FName : "未知商品";

                        // 決定結帳價格 (如果 Variant 沒特別設定價格，就用 Product 的預設價格)
                        decimal unitPrice = variant.FPrice ?? (product?.FPrice ?? 0);
                        decimal subtotal = unitPrice * item.Quantity; // 算出小計

                        // 4-5: 建立一個 TOrderDetail (明細) 並且放入剛剛的箱子裡
                        details.Add(new TOrderDetail
                        {
                            FVariantId = item.VariantId,
                            FProductNameSnap = productName,
                            FPriceSnap = unitPrice,
                            FQuantity = item.Quantity,
                            FSubtotal = subtotal
                        });

                        // 4-6: 把小計金額累加到這整張訂單的總金額上
                        totalAmount += subtotal;
                    }

                    // 步驟 5：建立一張主訂單 TOrder
                    var newOrder = new TOrder
                    {
                        FOrderNo = newOrderNo,
                        FMemberId = request.MemberId,
                        FTotalAmount = totalAmount,
                        FStatus = 1, // 1: 代表待處理 (待付款)
                        FReceiverName = request.ReceiverName,
                        FReceiverPhone = request.ReceiverPhone,
                        FReceiverAddress = request.ReceiverAddress,
                        FNote = request.Note,
                        FCreatedAt = DateTime.Now,
                        FUpdatedAt = DateTime.Now
                    };

                    // 先把主訂單寫入資料庫，這樣系統才會發一個最新的 FId (訂單流水號) 給它
                    _db.TOrders.Add(newOrder);
                    _db.SaveChanges();

                    // 把剛拿到的新訂單流水號，塞給箱子裡面的每一個明細，然後存入資料庫
                    foreach (var detail in details)
                    {
                        detail.FOrderId = newOrder.FId;
                        _db.TOrderDetails.Add(detail);
                    }

                    // 步驟 6：最後存檔，並按下「確認送出 (Commit)」按鈕
                    _db.SaveChanges();
                    transaction.Commit(); // 🌟 極度重要：這行執行完，資料才會真的寫入資料庫！

                    // 回傳大成功！
                    return new CreateOrderResponseDto
                    {
                        IsSuccess = true,
                        Message = "訂單建立成功！",
                        OrderNo = newOrderNo
                    };
                }
                catch (Exception ex)
                {
                    // 如果上面任何一個步驟發生錯誤 (例如庫存不足、資料庫當機)
                    // 就會跳到這裡，執行 Rollback (時光倒流)，什麼都不會存進去資料庫
                    transaction.Rollback();

                    return new CreateOrderResponseDto
                    {
                        IsSuccess = false,
                        Message = "訂單建立失敗：" + ex.Message
                    };
                }
            }
        }
    }
}

