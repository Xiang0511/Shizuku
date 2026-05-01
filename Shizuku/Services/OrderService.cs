using Shizuku.Models;

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
    }
}
