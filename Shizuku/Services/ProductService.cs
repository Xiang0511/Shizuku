using Microsoft.EntityFrameworkCore;
using Shizuku.Models;
using Shizuku.ViewModels;
using Shizuku.DTOs;

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
        public List<ProductListDto> GetProductList(string keyword, int? categoryId = null, bool isAdmin = false)//,bool isAdmin = false
        {
            var query = isAdmin
       ? _context.TProducts.Where(p => p.FStatus != 0)   // 後台：顯示全部（除刪除）
       : _context.TProducts.Where(p => p.FStatus == 1);  // 前台：只顯示上架

            //= isAdmin
            query = query.OrderByDescending(p => p.FCreatedAt);
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

                fMinPrice = _context.TProductVariants
                .Where(v => v.FProductId == p.FId && v.FPrice != null)
                .Select(v => v.FPrice)
                .OrderBy(v => v)
                .FirstOrDefault() ?? p.FPrice,
                fImage = _context.TProductImages
                    .Where(img => img.FProductId == p.FId)
                    .OrderByDescending(img => img.FIsMain)
                    .Select(img => img.FImageUrl)
                    .FirstOrDefault(),

                Variants = _context.TProductVariants
                    .Where(v => v.FProductId == p.FId)
                    .Select(v => new VariantSummaryDto
                    {
                        fId = v.FId,
                        fColor = _context.TProductColors
                            .Where(c => c.FId == v.FColorId)
                            .Select(c => c.FName)
                            .FirstOrDefault() ?? "無顏色",

                        fSize = _context.TProductSizes
                            .Where(s => s.FId == v.FSizeId)
                            .Select(s => s.FName)
                            .FirstOrDefault() ?? "無尺寸",

                        fStock = v.FStock,
                        fPrice = v.FPrice
                    }).ToList()

            }).ToList();
        }
        //取得所有商品圖片
        public List<string> GetProductImages(int productId)
        {
            return _context.TProductImages
                .Where(img => img.FProductId == productId)
                .OrderByDescending(img => img.FIsMain)
                .ThenBy(img => img.FSortOrder)
                .Select(img => img.FImageUrl)
                .ToList();
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

        /// <summary>取得相關商品（同分類）</summary>
        public List<ProductListDto> GetRelatedProducts(int productId)
        {
            var product = _context.TProducts.FirstOrDefault(p => p.FId == productId);
            if (product == null) return new List<ProductListDto>();

            return _context.TProducts
                .Where(p => p.FCategoryId == product.FCategoryId
                         && p.FId != productId
                         && p.FStatus == 1)
                .Take(6)
                .Select(p => new ProductListDto
                {
                    fId = p.FId,
                    fName = p.FName,
                    fProduct = p.FProduct,
                    fPrice = p.FPrice,
                    fStatus = (byte)p.FStatus,
                    fMinPrice = _context.TProductVariants
                        .Where(v => v.FProductId == p.FId && v.FPrice != null)
                        .Select(v => v.FPrice)
                        .OrderBy(v => v)
                        .FirstOrDefault() ?? p.FPrice,
                    fImage = _context.TProductImages
                        .Where(img => img.FProductId == p.FId && img.FIsMain == 1)
                        .Select(img => img.FImageUrl)
                        .FirstOrDefault()
                }).ToList();
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
                    FPrice = v.fPrice,
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
                    fPrice = v.FPrice,
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

                // 
                if (dto.fPrice.HasValue)
                    variant.FPrice = dto.fPrice.Value;
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
        public async Task<string> SaveExtraImageAsync(int productId, IFormFile photo)
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

            // 不設主圖
            _context.TProductImages.Add(new TProductImage
            {
                FProductId = productId,
                FImageUrl = imageUrl,
                FSortOrder = 2,
                FIsMain = 0
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
                    fId = c.FId,
                    fFullName = _context.TProductCategories
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
        public List<InventoryProductDto> GetInventory()
        {
            var products = _context.TProducts
                .Where(p => p.FStatus != 0)
                .ToList();

            var result = new List<InventoryProductDto>();

            foreach (var p in products)
            {
                var variants = _context.TProductVariants
                    .Where(v => v.FProductId == p.FId)
                    .ToList();

                var variantDtos = variants.Select(v => new InventoryVariantDto
                {
                    fVariantId = v.FId,
                    fSkuCode = v.FSkuCode ?? "",
                    fColor = _context.TProductColors
                    .Where(c => c.FId == v.FColorId)
                    .Select(c => c.FName)
                    .FirstOrDefault() ?? "無顏色",
                    fSize = _context.TProductSizes
                    .Where(c => c.FId == v.FSizeId)
                    .Select(c => c.FName)
                    .FirstOrDefault() ?? "無尺寸",
                    fStock = v.FStock,
                    fPrice = v.FPrice ?? p.FPrice,
                    fCostPrice = v.FCostPrice,
                    fStockStatus = v.FStock == 0 ? "售完"
                    : v.FStock <= 5 ? "低庫存"
                    : v.FStock <= 0 && !variants.Any() ? "缺貨"  // ← 新增
                    : "正常"
                }).ToList();
                result.Add(new InventoryProductDto
                {
                    fProductId = p.FId,
                    fProductName = p.FName,
                    fProduct = p.FProduct ?? "",
                    fImage = _context.TProductImages
        .Where(img => img.FProductId == p.FId && img.FIsMain == 1)
        .Select(img => img.FImageUrl)
        .FirstOrDefault(),
                    fTotalStock = variantDtos.Sum(v => v.fStock),
                    fVariants = variantDtos
                });
            }
            return result;
        }
            /// <summary>取得進貨紀錄</summary>
public List<StockRecordDto> GetStockRecords()
        {
            return _context.TProductStockRecords
                .OrderByDescending(r => r.FCreatedAt)
                .Select(r => new StockRecordDto
                {
                    fId = r.FId,
                    fVariantId = r.FVariantId,
                    fType = r.FType,
                    fQuantity = r.FQuantity,
                    fCostPrice = r.FCostPrice,
                    fNote = r.FNote,
                    fCreatedAt = r.FCreatedAt,
                    fColor = _context.TProductColors
                        .Where(c => c.FId == _context.TProductVariants
                            .Where(v => v.FId == r.FVariantId)
                            .Select(v => v.FColorId)
                            .FirstOrDefault())
                        .Select(c => c.FName)
                        .FirstOrDefault() ?? "",
                    fSize = _context.TProductSizes
                        .Where(s => s.FId == _context.TProductVariants
                            .Where(v => v.FId == r.FVariantId)
                            .Select(v => v.FSizeId)
                            .FirstOrDefault())
                        .Select(s => s.FName)
                        .FirstOrDefault() ?? "",
                    fProductName = _context.TProducts
                        .Where(p => p.FId == _context.TProductVariants
                            .Where(v => v.FId == r.FVariantId)
                            .Select(v => v.FProductId)
                            .FirstOrDefault())
                        .Select(p => p.FName)
                        .FirstOrDefault() ?? ""
                }).ToList();
        }

        /// <summary>新增進貨紀錄並更新庫存</summary>
        public bool AddStockRecord(StockRecordCreateDto dto)
        {
            var variant = _context.TProductVariants.FirstOrDefault(v => v.FId == dto.fVariantId);
            if (variant == null) return false;

            // 新增進貨紀錄
            _context.TProductStockRecords.Add(new TProductStockRecord
            {
                FVariantId = dto.fVariantId,
                FType = dto.fType,
                FQuantity = dto.fQuantity,
                FCostPrice = dto.fCostPrice,
                FNote = dto.fNote,
                FCreatedAt = DateTime.Now
            });

            // 更新庫存
            // 完整展開的寫法：新庫存 = 舊庫存 + 增加數量
            variant.FStock = variant.FStock + dto.fQuantity;
            // 更新成本價
            if (dto.fCostPrice.HasValue)
                variant.FCostPrice = dto.fCostPrice.Value;

            _context.SaveChanges();
            return true;
        }
        /// <summary>取得所有進貨單</summary>
        public List<PurchaseOrderDto> GetPurchaseOrders()
        {
            return _context.TPurchaseOrders
                .OrderByDescending(o => o.FCreatedAt)
                .Select(o => new PurchaseOrderDto
                {
                    fId = o.FId,
                    fOrderNo = o.FOrderNo,
                    fSupplier = o.FSupplier,
                    fPaymentMethod = o.FPaymentMethod,
                    fType = o.FType,
                    fStatus = o.FStatus,
                    fInvoiceNo = o.FInvoiceNo,
                    fInvoiceDate = o.FInvoiceDate,
                    fTaxType = o.FTaxType,
                    fUntaxedAmount = o.FUntaxedAmount,
                    fTaxAmount = o.FTaxAmount,
                    fNote = o.FNote,
                    fTotalQuantity = o.FTotalQuantity,
                    fTotalAmount = o.FTotalAmount,
                    fItemCount = _context.TPurchaseOrderDetails
                        .Count(d => d.FOrderId == o.FId),
                    fCreatedAt = o.FCreatedAt
                }).ToList();
        }
        /// <summary>取得進貨單詳細</summary>
        public PurchaseOrderFullDto? GetPurchaseOrder(int id)
        {
            var order = _context.TPurchaseOrders.FirstOrDefault(o => o.FId == id);
            if (order == null) return null;

            var details = _context.TPurchaseOrderDetails
                .Where(d => d.FOrderId == id)
                .Select(d => new PurchaseOrderDetailDto
                {
                    fId = d.FId,
                    fVariantId = d.FVariantId,
                    fProductName = _context.TProducts
                        .Where(p => p.FId == _context.TProductVariants
                            .Where(v => v.FId == d.FVariantId)
                            .Select(v => v.FProductId)
                            .FirstOrDefault())
                        .Select(p => p.FName)
                        .FirstOrDefault() ?? "",
                    fSkuCode = _context.TProductVariants
                    .Where(v => v.FId == d.FVariantId)
                    .Select(v => v.FSkuCode)
                    .FirstOrDefault() ?? "",
                    fColor = _context.TProductColors
                        .Where(c => c.FId == _context.TProductVariants
                            .Where(v => v.FId == d.FVariantId)
                            .Select(v => v.FColorId)
                            .FirstOrDefault())
                        .Select(c => c.FName)
                        .FirstOrDefault() ?? "",
                    fSize = _context.TProductSizes
                        .Where(s => s.FId == _context.TProductVariants
                            .Where(v => v.FId == d.FVariantId)
                            .Select(v => v.FSizeId)
                            .FirstOrDefault())
                        .Select(s => s.FName)
                        .FirstOrDefault() ?? "",
                    fQuantity = d.FQuantity,
                    fCostPrice = d.FCostPrice,
                    fAmount = d.FAmount,
                    fNote = d.FNote
                }).ToList();

            return new PurchaseOrderFullDto
            {
                fId = order.FId,
                fOrderNo = order.FOrderNo,
                fSupplier = order.FSupplier,
                fPaymentMethod = order.FPaymentMethod,
                fNote = order.FNote,
                fType = order.FType,          // ← 已加
                fStatus = order.FStatus,        // ← 已加
                fInvoiceNo = order.FInvoiceNo,     // ← 已加
                fInvoiceDate = order.FInvoiceDate,   // ← 已加
                fTaxType = order.FTaxType,       // ← 已加
                fUntaxedAmount = order.FUntaxedAmount, // ← 已加
                fTaxAmount = order.FTaxAmount,     // ← 已加
                fTotalQuantity = order.FTotalQuantity,
                fTotalAmount = order.FTotalAmount,
                fCreatedAt = order.FCreatedAt,
                fDetails = details
            };
        }

        /// <summary>新增進貨單</summary>
        public int CreatePurchaseOrder(PurchaseOrderCreateDto dto)
        {
            // 自動生成進貨單號 PO-YYYYMMDD-XXX
            string dateStr = DateTime.Now.ToString("yyyyMMdd");
            int todayCount = _context.TPurchaseOrders
                .Count(o => o.FOrderNo.StartsWith($"PO-{dateStr}")) + 1;
            string orderNo = $"PO-{dateStr}-{todayCount:D3}";

            decimal untaxed = dto.fDetails.Sum(d => d.fQuantity * (d.fCostPrice ?? 0));
            decimal taxAmt = dto.fTaxType == "應稅" ? Math.Round(untaxed * 0.05m, 0) : 0;
            decimal total = untaxed + taxAmt;

            var order = new TPurchaseOrder
            {
                FOrderNo = orderNo,
                FSupplier = dto.fSupplier,
                FPaymentMethod = dto.fPaymentMethod,
                FType = dto.fType,          // ← 加
                FStatus = dto.fStatus,        // ← 加
                FInvoiceNo = dto.fInvoiceNo,     // ← 加
                FInvoiceDate = dto.fInvoiceDate,   // ← 加
                FTaxType = dto.fTaxType,       // ← 加
                FTaxRate = dto.fTaxRate,       // ← 加
                FUntaxedAmount = untaxed,            // ← 加
                FTaxAmount = taxAmt,             // ← 加
                FNote = dto.fNote,
                FTotalQuantity = dto.fDetails.Sum(d => d.fQuantity),
                FTotalAmount = total,// ← 改成含稅總計
                FCreatedAt = DateTime.Now
            };

            _context.TPurchaseOrders.Add(order);
            _context.SaveChanges();

            foreach (var d in dto.fDetails)
            {
                _context.TPurchaseOrderDetails.Add(new TPurchaseOrderDetail
                {
                    FOrderId = order.FId,
                    FVariantId = d.fVariantId,
                    FQuantity = d.fQuantity,
                    FCostPrice = d.fCostPrice,
                    FAmount = d.fQuantity * (d.fCostPrice ?? 0),
                    FNote = d.fNote
                });

                var variant = _context.TProductVariants
           .FirstOrDefault(v => v.FId == d.fVariantId);
                if (variant == null) continue;

                // ✨ 根據類型決定庫存增減
                switch (dto.fType)
                {
                    case "進貨":
                    case "銷售退回":
                    case "調整進":
                        variant.FStock += d.fQuantity;
                        break;
                    case "退貨":
                    case "進貨退出":
                    case "報廢":
                    case "調整出":
                        variant.FStock -= d.fQuantity;
                        break;
                }

                if (d.fCostPrice.HasValue)
                    variant.FCostPrice = d.fCostPrice.Value;
            }

            _context.SaveChanges();
            return order.FId;
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
            LatestPrice = v.FPrice ?? v.TProduct.FPrice, 
            CurrentStock = v.FStock,
            ProductName = v.TProduct.FName
            }).ToListAsync();
        }
        public void AddVariants(int productId, List<VariantInputDto> variants)
        {
            var product = _context.TProducts.Find(productId);
            if (product == null) return;

            foreach (var v in variants)
            {
                if (v.fColorId == 0 || v.fSizeId == 0) continue;
                _context.TProductVariants.Add(new TProductVariant
                {
                    FProductId = productId,
                    FColorId = v.fColorId,
                    FSizeId = v.fSizeId,
                    FStock = v.fStock,
                    FPrice = v.fPrice,
                    FSkuCode = $"{product.FProduct}-{v.fColorId}-{v.fSizeId}"
                });
            }
            _context.SaveChanges();
        }
    }
}
