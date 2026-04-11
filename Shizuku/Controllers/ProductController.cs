using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; 
using Shizuku.Models;
using Shizuku.ViewModel;

namespace Shizuku.Controllers
{
    public class ProductController : Controller
    {
        // 1. 宣告這個變數
        private readonly DbShizukuDemoContext _context;

        // 2. 透過相依注入 (Dependency Injection) 把它接進來
        public ProductController(DbShizukuDemoContext context)
        {
            _context = context;
        }

        public IActionResult List(string txtKeyword)
        {
            // 1. 取得產品主表
            var query = _context.TProducts.Where(p => p.FStatus != 0);

            /// 2. 關鍵字過濾
            if (!string.IsNullOrEmpty(txtKeyword))
            {
                query = query.Where(p => p.FName.Contains(txtKeyword) || p.FProduct.Contains(txtKeyword));
            }

            // 3. 執行投影 (將資料塞進 Wrap 盒子)
            var datas = query.Select(p => new CProductwrap
            {
                product = p,

                // ✨ 圖片：直接去圖片表找該產品的主圖
                FImage = _context.TProductImages
                          .Where(img => img.FProductId == p.FId)
                          .OrderByDescending(img => img.FIsMain)
                          .Select(img => img.FImageUrl)
                          .FirstOrDefault(),

                // ✨ 規格：直接在 Select 內進行手動 Join
                Variants = _context.TProductVariants
                          .Where(v => v.FProductId == p.FId)
                          .Select(v => new Shizuku.Models.ProductVariantItem
                          {
                              Color = _context.TProductColors
                             .Where(c => c.FId == v.FColorId) // 注意：這裡通常是用 FId 對應 v.FColorId
                             .Select(c => c.FName)           // 這裡是 FName
                             .FirstOrDefault() ?? "無顏色",

                              // 👉 去尺寸表撈名稱 (請確認你的表名是 TSizes 還是 TSize)
                              Size = _context.TProductSizes
                                        .Where(s => s.FId == v.FSizeId)
                                        .Select(s => s.FName)
                                        .FirstOrDefault() ?? "無尺寸",

                              Stock = v.FStock
                          }).ToList()
            }).ToList();

            ViewBag.Keyword = txtKeyword;
            return View(datas);
        }

        public IActionResult Delete(int? id)
        {
            var x = _context.TProducts.FirstOrDefault(p => p.FId == id);
            if (x != null)
            {
                x.FStatus = 0; // 執行軟刪除
                _context.SaveChanges();
            }
            return RedirectToAction("List");
        }
        public IActionResult Edit(int? id)
        {
            var x = _context.TProducts.FirstOrDefault(p => p.FId == id);
            if (x == null)
            {
                return RedirectToAction("List");
            }
            CProductwrap vm = new CProductwrap();
            vm.FId = x.FId;
            vm.FName = x.FName;
            vm.FProduct = x.FProduct;
            vm.FPrice = x.FPrice;
            vm.FCategoryId = x.FCategoryId;
            vm.FDescription = x.FDescription;

            // 處理 byte 轉型問題 (如果資料庫是 byte，ViewModel 是 int)
            vm.FStatus = (byte)x.FStatus;

            // 3. 傳送包裝好的「vm」而不是「x」
            return View(vm);
            
        }
        [HttpPost]
        public IActionResult Edit(CProductwrap uiProd)
        {
            var dbprod = _context.TProducts.FirstOrDefault(p => p.FId == uiProd.FId);
            if (dbprod != null)
            {
                dbprod.FName = uiProd.FName;
                dbprod.FProduct = uiProd.FProduct;
                dbprod.FPrice = uiProd.FPrice;
                dbprod.FStatus = uiProd.FStatus;
                dbprod.FCategoryId = uiProd.FCategoryId;
                dbprod.FDescription = uiProd.FDescription;
                _context.SaveChanges();
            }
            return RedirectToAction("List");
        }
        public ActionResult Create()
        {
            var allCategories = _context.TProductCategories.ToList();
            // 1. 抓出所有「子分類」(fParentId 不是 NULL 的)
            var list = _context.TProductCategories
                .Where(c => c.FParentId != null)
                .ToList()
                .Select(c => new {
                    ID = c.FId,
                    // 抓出父類別名稱 + 自己的名稱，例如：女裝-洋裝
                    FullName = _context.TProductCategories.FirstOrDefault(p => p.FId.ToString() == c.FParentId)?.FName + "-" + c.FName
                })
            .ToList();

            // 放到 ViewBag 傳給前端，指定 ID 為值，FullName 為顯示文字
            ViewBag.fCategoryId = new SelectList(list, "ID", "FullName");

            // 2. 抓出顏色選單 (顯示名稱 FName，傳回 ID FId)
            // 假設資料表是 _context.TProductColors
            ViewBag.FColorId = new SelectList(_context.TProductColors, "FId", "FName");

            // 3. 抓出尺寸選單 (顯示名稱 FName，傳回 ID FId)
            // 假設資料表是 _context.TProductSizes
            ViewBag.FSizeId = new SelectList(_context.TProductSizes, "FId", "FName");


            return View(new CProductwrap());
        }

        [HttpPost]
        //處理存檔用
        public ActionResult Create(CProductwrap p)
        {
            // ✨ 關鍵修正：將前端傳回來的「攤平屬性」塞進真正的 product 實體中
            // 否則 p.product.FCategoryId 會是 null，導致後面 Find 失敗
            p.product.FCategoryId = p.FCategoryId;
            p.product.FName = p.FName;
            p.product.FPrice = p.FPrice;
            p.product.FDescription = p.FDescription;

            // --- 自動產生貨號開始 ---
            var category = _context.TProductCategories.Find(p.product.FCategoryId);
            if (category == null) return View(p); // 安全檢查

            // 注意：如果 FParentId 是 string，轉 int 要小心 null
            if (string.IsNullOrEmpty(category.FParentId)) return View(p);

            int parentId = int.Parse(category.FParentId);
            var parent = _context.TProductCategories.Find(parentId);
            string prefix = parent.FCodePrefix + category.FCodePrefix;

            var lastProduct = _context.TProducts
                .Where(x => x.FProduct.StartsWith(prefix + "-"))
                .OrderByDescending(x => x.FProduct)
                .FirstOrDefault();

            int nextNum = 1;
            if (lastProduct != null)
            {
                string numStr = lastProduct.FProduct.Replace(prefix + "-", "");
                if (int.TryParse(numStr, out int result)) nextNum = result + 1;
            }

            // 將產生的貨號存入實體
            p.product.FProduct = prefix + "-" + nextNum.ToString("000");
            p.product.FCreatedAt = DateTime.Now;
            p.product.FStatus = 1; // 1 通常代表上架
                                   // --- 自動產生貨號結束 ---

            // 儲存商品主表
            _context.TProducts.Add(p.product);
            _context.SaveChanges();

            // --- 3. 存入規格與庫存 ---
            // 這裡使用 p.product.FId (剛存完後自動取得的自增主鍵)
            var variant = new TProductVariant
            {
                // 🔹 只給純數字 ID，斷開物件關聯，避免 EF 亂猜欄位名稱
                FProductId = p.product.FId,
                FColorId = p.FColorId,
                FSizeId = p.FSizeId,
                FStock = p.FStock,
                // SKU 自動組合
                FSkuCode = $"{p.product.FProduct}-{p.FColorId}-{p.FSizeId}"
            };

            // 3. ✨ 方法三：明確指定狀態為 Added，並直接存檔
            // 這行會強制 EF 把這個物件當作純資料處理，不理會它跟 TProduct 的導航屬性
            _context.Entry(variant).State = Microsoft.EntityFrameworkCore.EntityState.Added;

            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                // 如果還是噴錯，可以在這裡打斷點看 ex.InnerException
                throw ex;
            }

            return RedirectToAction("List");
        }
    }
}

