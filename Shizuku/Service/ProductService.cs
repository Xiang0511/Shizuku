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
        public List<ProductListDto> GetProductList(string keyword)
        {
            var query = _context.TProducts.Where(p => p.FStatus != 0);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p =>
                    p.FName.Contains(keyword) ||
                    p.FProduct.Contains(keyword));
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

            var variant = new TProductVariant
            {
                FProductId = newProduct.FId,
                FColorId = dto.fColorId,
                FSizeId = dto.fSizeId,
                FStock = dto.fStock,
                FSkuCode = $"{productCode}-{dto.fColorId}-{dto.fSizeId}"
            };

            _context.Entry(variant).State = EntityState.Added;
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
    }
}