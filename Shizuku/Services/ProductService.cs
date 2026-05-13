using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.ViewModels;

namespace Shizuku.Services
{
    public class ProductService
    {
        private readonly DbShizukuDemoContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductService(DbShizukuDemoContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>取得商品列表，支援關鍵字搜尋（商品名稱或貨號）</summary>
        public List<ProductListDto> GetProductList(string keyword, int? categoryId = null)
        {
            var query = _context.TProducts.Where(p => p.FStatus != 0);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p =>
                    p.FName.Contains(keyword) ||
                    p.FProduct.Contains(keyword));
            }
            // 加上分類篩選
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.FCategoryId == categoryId.Value);
            }

            return query.Select(p => new ProductListDto
            {
                fId = p.FId,
                fName = p.FName,
                fProduct = p.FProduct,
                fPrice = p.FPrice,
                fStatus = (byte)p.FStatus,

                fImage = _context.TProductImages
                    .Where(img => img.FProductId == p.FId)
                    .OrderByDescending(img => img.FIsMain)
                    .Select(img => img.FImageUrl)
                    .FirstOrDefault(),

                Variants = _context.TProductVariants
                    .Where(v => v.FProductId == p.FId)
                    .Select(v => new VariantSummaryDto
                    {
                        fColor = _context.TProductColors
                            .Where(c => c.FId == v.FColorId)
                            .Select(c => c.FName)
                            .FirstOrDefault() ?? "無顏色",

                        fSize = _context.TProductSizes
                            .Where(s => s.FId == v.FSizeId)
                            .Select(s => s.FName)
                            .FirstOrDefault() ?? "無尺寸",

                        fStock = v.FStock
                    }).ToList()

            }).ToList();
        }

        /// <summary>軟刪除（將 fStatus 設為 0）</summary>
        public bool SoftDelete(int id)
        {
            var product = _context.TProducts.FirstOrDefault(p => p.FId == id);

            if (product == null) return false;

            product.FStatus = 0;
            _context.SaveChanges();
            return true;
        }

        /// <summary>依 ID 取得編輯用 DTO；找不到回傳 null</summary>
        public ProductEditDto GetForEdit(int id)
        {
            var product = _context.TProducts.FirstOrDefault(p => p.FId == id);

            if (product == null) return null;

            return new ProductEditDto
            {
                fId = product.FId,
                fName = product.FName,
                fProduct = product.FProduct,
                fPrice = product.FPrice,
                fCategoryId = product.FCategoryId,
                fDescription = product.FDescription,
                fStatus = (byte)product.FStatus,
                // 同時撈出目前主圖路徑
                fImage = _context.TProductImages
                    .Where(img => img.FProductId == product.FId && img.FIsMain == 1)
                    .Select(img => img.FImageUrl)
                    .FirstOrDefault()

            };
        }

        /// <summary>更新商品基本資料</summary>
        public bool Update(ProductEditDto dto)
        {
            var product = _context.TProducts.FirstOrDefault(p => p.FId == dto.fId);

            if (product == null) return false;

            product.FName = dto.fName;
            product.FProduct = dto.fProduct;
            product.FPrice = dto.fPrice;
            product.FStatus = dto.fStatus;
            product.FCategoryId = dto.fCategoryId;
            product.FDescription = dto.fDescription;

            _context.SaveChanges();
            return true;
        }

        /// <summary>建立新商品並自動產生貨號，同時新增一筆規格庫存</summary>
        public int? Create(ProductCreateDto dto)
        {
            var category = _context.TProductCategories.Find(dto.fCategoryId);

            // Guard: 分類不存在
            if (category == null) return null;

            // Guard: 沒有父分類則無法產生貨號前綴
            if (string.IsNullOrEmpty(category.FParentId)) return null;

            int parentId = int.Parse(category.FParentId);
            var parent = _context.TProductCategories.Find(parentId);

            if (parent == null) return null;

            string prefix = parent.FCodePrefix + category.FCodePrefix;
            string productCode = BuildProductCode(prefix);

            var newProduct = new TProduct
            {
                FName = dto.fName,
                FPrice = dto.fPrice,
                FCategoryId = dto.fCategoryId,
                FDescription = dto.fDescription,
                FProduct = productCode,
                FCreatedAt = DateTime.Now,
                FStatus = 1
            };

            _context.TProducts.Add(newProduct);
            _context.SaveChanges();

            foreach (var v in dto.Variants)
            {
                // Guard：顏色或尺寸沒選就跳過
                if (v.fColorId == 0 || v.fSizeId == 0) continue;

                var variant = new TProductVariant
                {
                    FProductId = newProduct.FId,
                    FColorId = v.fColorId,
                    FSizeId = v.fSizeId,
                    FStock = v.fStock,
                    FSkuCode = $"{productCode}-{v.fColorId}-{v.fSizeId}"
                };

                _context.Entry(variant).State = EntityState.Added;
            }

            _context.SaveChanges();
            return newProduct.FId;
        }

        /// <summary>根據前綴自動計算下一個流水號貨號</summary>
        private string BuildProductCode(string prefix)
        {
            var last = _context.TProducts
                .Where(x => x.FProduct.StartsWith(prefix + "-"))
                .OrderByDescending(x => x.FProduct)
                .FirstOrDefault();

            int nextNum = 1;

            if (last != null)
            {
                string numStr = last.FProduct.Replace(prefix + "-", "");
                if (int.TryParse(numStr, out int parsed))
                    nextNum = parsed + 1;
            }

            return $"{prefix}-{nextNum:000}";
        }
        /// <summary>取得某商品的所有規格庫存（含顏色、尺寸名稱）</summary>
        public List<VariantEditDto> GetVariants(int productId)
        {
            return _context.TProductVariants
                .Where(v => v.FProductId == productId)
                .Select(v => new VariantEditDto
                {
                    fId = v.FId,
                    fStock = v.FStock,
                    fSkuCode = v.FSkuCode,
                    fColor = _context.TProductColors
                        .Where(c => c.FId == v.FColorId)
                        .Select(c => c.FName)
                        .FirstOrDefault() ?? "無顏色",
                    fSize = _context.TProductSizes
                        .Where(s => s.FId == v.FSizeId)
                        .Select(s => s.FName)
                        .FirstOrDefault() ?? "無尺寸"
                }).ToList();
        }

        /// <summary>批次更新規格庫存數量</summary>
        public void UpdateVariantStocks(List<VariantEditDto> variants)
        {
            foreach (var dto in variants)
            {
                var variant = _context.TProductVariants.FirstOrDefault(v => v.FId == dto.fId);

                if (variant == null) continue;

                variant.FStock = dto.fStock;
            }
            _context.SaveChanges();
        }

        /// <summary>儲存上傳圖片並寫入 tProductImages，回傳圖片路徑</summary>
        public async Task<string> SaveImageAsync(int productId, IFormFile photo)
        {
            if (photo == null || photo.Length == 0) return null;

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsFolder);

            string fileName = $"{productId}_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            string filePath = Path.Combine(uploadsFolder, fileName);
            string imageUrl = $"/uploads/products/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            // 舊主圖降為非主圖
            var oldMain = _context.TProductImages
                .Where(img => img.FProductId == productId && img.FIsMain == 1)
                .ToList();

            oldMain.ForEach(img => img.FIsMain = 0);

            // 新增主圖紀錄
            _context.TProductImages.Add(new TProductImage
            {
                FProductId = productId,
                FImageUrl = imageUrl,
                FSortOrder = 1,
                FIsMain = 1
            });

            _context.SaveChanges();
            return imageUrl;
        }
        /// <summary>取得新增頁所需的下拉選單資料</summary>
        public (List<object> categories, List<TProductColor> colors, List<TProductSize> sizes) GetDropdownData()
        {
            var categories = _context.TProductCategories
                .Where(c => c.FParentId != null)
                .ToList()
                .Select(c => new
                {
                    ID = c.FId,
                    FullName = _context.TProductCategories
                        .FirstOrDefault(p => p.FId.ToString() == c.FParentId)?.FName
                        + "-" + c.FName
                })
                .ToList<object>();

            var colors = _context.TProductColors.ToList();
            var sizes = _context.TProductSizes.ToList();

            return (categories, colors, sizes);
        }
        /// <summary>取得 Dashboard 統計數據（含真實銷售分析）</summary>
        public ProductStatsDto GetStats()
        {
            var products = _context.TProducts.ToList();
            var variants = _context.TProductVariants.ToList();

            // 從訂單明細計算每個商品的銷售數量
            var salesData = _context.TOrderDetails
                .GroupBy(od => od.FVariantId)
                .Select(g => new
                {
                    fVariantId = g.Key,
                    fTotalSold = g.Sum(od => od.FQuantity),
                    fTotalRevenue = g.Sum(od => od.FSubtotal)
                }).ToList();

            // 對應到商品
            var productSales = variants
                .GroupJoin(salesData,
                    v => v.FId,
                    s => s.fVariantId,
                    (v, s) => new { v, sales = s.FirstOrDefault() })
                .GroupBy(x => x.v.FProductId)
                .Select(g => new ProductSalesDto
                {
                    fProductId = g.Key,
                    fProductName = _context.TProducts
                        .Where(p => p.FId == g.Key)
                        .Select(p => p.FName)
                        .FirstOrDefault() ?? "",
                    fProduct = _context.TProducts
                        .Where(p => p.FId == g.Key)
                        .Select(p => p.FProduct)
                        .FirstOrDefault() ?? "",
                    fTotalSold = g.Sum(x => x.sales?.fTotalSold ?? 0),
                    fTotalRevenue = g.Sum(x => x.sales?.fTotalRevenue ?? 0),
                    fStatus = g.Sum(x => x.sales?.fTotalSold ?? 0) >= 100 ? "熱銷"
                                  : g.Sum(x => x.sales?.fTotalSold ?? 0) >= 20 ? "普通"
                                  : "滯銷"
                })
                .OrderByDescending(p => p.fTotalSold)
                .ToList();

            return new ProductStatsDto
            {
                fTotalProducts = products.Count(p => p.FStatus != 0),
                fActiveProducts = products.Count(p => p.FStatus == 1),
                fOfflineProducts = products.Count(p => p.FStatus == 2),
                fTotalStock = variants.Sum(v => v.FStock),
                fLowStockCount = variants.Count(v => v.FStock > 0 && v.FStock <= 5),
                fSoldOutCount = variants.Count(v => v.FStock == 0),
                fTotalRevenue = salesData.Sum(s => s.fTotalRevenue),
                fHotProducts = productSales.Take(5).ToList(),
                fSlowProducts = productSales
                    .Where(p => p.fStatus == "滯銷")
                    .TakeLast(5).ToList(),
                fCategoryStats = _context.TProductCategories
                    .Where(c => c.FParentId != null)
                    .Select(c => new CategoryStatDto
                    {
                        fCategoryName = c.FName,
                        fProductCount = _context.TProducts
                            .Count(p => p.FCategoryId == c.FId && p.FStatus != 0)
                    }).ToList()
            };
        }

        /// <summary>取得所有商品規格庫存總覽</summary>
        public List<InventoryDto> GetInventory()
        {
            return _context.TProductVariants
                .Where(v => v.TProduct.FStatus != 0)
                .Select(v => new InventoryDto
                {
                    fProductId = v.FProductId,
                    fProductName = v.TProduct.FName,
                    fVariantId = v.FId,
                    fSkuCode = v.FSkuCode,
                    fStock = v.FStock,
                    fColor = _context.TProductColors
                        .Where(c => c.FId == v.FColorId)
                        .Select(c => c.FName)
                        .FirstOrDefault() ?? "無顏色",
                    fSize = _context.TProductSizes
                        .Where(s => s.FId == v.FSizeId)
                        .Select(s => s.FName)
                        .FirstOrDefault() ?? "無尺寸",
                    fStockStatus = v.FStock == 0 ? "售完"
                                 : v.FStock <= 5 ? "低庫存"
                                 : "正常"
                }).ToList();
        }

        /// <summary>扣除庫存 (下單用)</summary>
        public async Task<bool> DeductStockAsync(int variantId, int quantity)
        {
            var variant = await _context.TProductVariants.FindAsync(variantId);
            if (variant == null || variant.FStock < quantity)
            {
                return false; // 找不到規格或庫存不足
            }
            variant.FStock -= quantity;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>回補庫存 (取消訂單用)</summary>
        public async Task<bool> RestoreStockAsync(int variantId, int quantity)
        {
            var variant = await _context.TProductVariants.FindAsync(variantId);
            if (variant == null) return false;
            variant.FStock += quantity;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>取得最新價格與庫存 (結帳預檢用)</summary>
        public async Task<List<ProductCheckDto>> GetLatestInfoAsync(List<int> variantIds)
        {
            return await _context.TProductVariants
            .Where(v => variantIds.Contains(v.FId))
            .Select(v => new ProductCheckDto
            {
            VariantId = v.FId,
            // 優先取規格價格，若無則取商品主表價格
            LatestPrice = v.FPrice ?? (v.TProduct.FPrice ?? 0),
            CurrentStock = v.FStock,
            ProductName = v.TProduct.FName
            }).ToListAsync();
        }
    }
}