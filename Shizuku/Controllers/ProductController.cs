using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shizuku.DTOs;
using Shizuku.Models;
using Shizuku.Services;
using Shizuku.ViewModels;

namespace Shizuku.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        /// <summary>取得商品列表</summary>
        [HttpGet]
        public IActionResult List(
            [FromQuery] string? keyword,
            [FromQuery] int? categoryId,
            [FromQuery]bool isAdmin = false)
        {
            var datas = _productService.GetProductList(keyword, categoryId,isAdmin);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = datas
            });
        }

        /// <summary>取得下拉選單資料</summary>
        [HttpGet("dropdowns")]
        public IActionResult GetDropdowns()
        {
            var (categories, colors, sizes) = _productService.GetDropdownData();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = new { categories, colors, sizes }
            });
        }

        /// <summary>取得單筆商品（編輯用）</summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var dto = _productService.GetForEdit(id);

            if (dto == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "找不到商品"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = dto
            });
        }

        /// <summary>取得商品規格庫存</summary>
        [HttpGet("{id}/variants")]
        public IActionResult GetVariants(int id)
        {
            var variants = _productService.GetVariants(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = variants
            });
        }

        [HttpGet("{id}/images")]
        
        public IActionResult GetImages(int id)
        {
            var images = _productService.GetProductImages(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = images
            });
        }
        /// <summary>取得進貨單列表</summary>
        [HttpGet("purchase-orders")]
        public IActionResult GetPurchaseOrders()
        {
            var orders = _productService.GetPurchaseOrders();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = orders
            });
        }

        /// <summary>取得進貨單詳細</summary>
        [HttpGet("purchase-orders/{id}")]
        public IActionResult GetPurchaseOrder(int id)
        {
            var order = _productService.GetPurchaseOrder(id);
            if (order == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "找不到此進貨單"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = order
            });
        }
        /// <summary>取得相關商品</summary>
        [HttpGet("{id}/related")]
        public IActionResult GetRelated(int id)
        {
            var related = _productService.GetRelatedProducts(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = related
            });
        }
        /// <summary>新增商品</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            try
            {
                int? newId = _productService.Create(dto);

                if (newId == null)
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "新增失敗，請確認分類是否正確"
                    });

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "新增成功",
                    Data = new { fId = newId }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new { detail = ex.InnerException?.Message }
                });
            }
        }

        /// <summary>新增進貨單</summary>
        [HttpPost("purchase-orders")]
        public IActionResult CreatePurchaseOrder([FromBody] PurchaseOrderCreateDto dto)
        {
            if (dto.fDetails == null || dto.fDetails.Count == 0)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "請至少選擇一筆商品"
                });

            var newId = _productService.CreatePurchaseOrder(dto);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "進貨成功",
                Data = new { fId = newId }
            });
        }
        //取得進銷存報表商品與規格
        [HttpGet("inventory-report")]
        public IActionResult GetInventoryReport()
        {
            var report = _productService.GetInventoryReport();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = report
            });
        }
        // 編輯進貨單
        [HttpPut("purchase-orders/{id}/status")]
        public IActionResult UpdatePurchaseOrderStatus(int id, [FromBody] string status)
        {
            var result = _productService.UpdatePurchaseOrderStatus(id, status);
            if (!result)
                return NotFound(new ApiResponse<object> { Success = false, Message = "找不到此異動單" });

            return Ok(new ApiResponse<object> { Success = true, Message = "更新成功" });
        }
        /// <summary>上傳商品圖片</summary>
        [HttpPost("{id}/image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "請選擇圖片"
                });

            var imageUrl = await _productService.SaveImageAsync(id, photo);

            if (imageUrl == null)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "圖片上傳失敗"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "圖片上傳成功",
                Data = new { imageUrl }
            });
        }
        [HttpPost("{id}/image/extra")]
        public async Task<IActionResult> UploadExtraImage(int id, IFormFile photo)
        {
            if (photo == null) return BadRequest();

            var imageUrl = await _productService.SaveExtraImageAsync(id, photo);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "上傳成功",
                Data = imageUrl
            });
        }
        //新增新規格
        [HttpPost("{id}/variants")]
        public IActionResult AddVariants(int id, [FromBody] List<VariantInputDto> variants)
        {
            _productService.AddVariants(id, variants);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "規格新增成功"
            });
        }
        /// <summary>更新商品基本資料</summary>
        [HttpPut("{id}")]
        public IActionResult Edit(int id, [FromBody] ProductEditDto dto)
        {
            dto.fId = id;

            bool success = _productService.Update(dto);

            if (!success)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "找不到商品"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "更新成功"
            });
        }

        /// <summary>批次更新規格庫存</summary>
        [HttpPut("{id}/variants")]
        public IActionResult UpdateVariants(int id, [FromBody] List<VariantEditDto> variants)
        {
            _productService.UpdateVariantStocks(variants);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "庫存更新成功"
            });
        }

        /// <summary>軟刪除商品</summary>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            bool success = _productService.SoftDelete(id);

            if (!success)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "找不到商品"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "刪除成功"
            });
        }
        /// <summary>取得 Dashboard 統計數據</summary>
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            var stats = _productService.GetStats();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = stats
            });
        }

        /// <summary>取得庫存總覽</summary>
        [HttpGet("inventory")]
        public IActionResult GetInventory()
        {
            var inventory = _productService.GetInventory();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = inventory
            });
        }

        /// <summary>取得進貨紀錄</summary>
        [HttpGet("stock-records")]
        public IActionResult GetStockRecords([FromQuery] int? variantId = null)
        {
            var records = _productService.GetStockRecords(variantId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = records
            });
        }

        [HttpGet("variant-by-sku")]
        public IActionResult GetVariantBySku([FromQuery] string sku)
        {
            var result = _productService.GetVariantBySku(sku);

            if (result == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "找不到此規格"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "查詢成功",
                Data = result
            });
        }

        /// <summary>新增進貨紀錄</summary>
        [HttpPost("stock-records")]
        public IActionResult AddStockRecord([FromBody] StockRecordCreateDto dto)
        {
            var result = _productService.AddStockRecord(dto);
            if (!result)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "新增失敗，規格不存在"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "進貨成功"
            });
        }

        /// <summary>結帳時檢查商品庫存與價格請求</summary>
        [HttpPost("check-items")]
        public async Task<IActionResult> CheckItems([FromBody] List<int> variantIds)
        {
            if (variantIds == null || !variantIds.Any())
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "未提供商品 ID" });
            }
            var results = await _productService.GetLatestInfoAsync(variantIds);
            return Ok(new ApiResponse<List<ProductCheckDto>>
            {
                Success = true,
                Message = "檢查成功",
                Data = results
            });
        }

    }
}