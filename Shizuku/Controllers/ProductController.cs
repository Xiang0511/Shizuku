using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
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
        public IActionResult List([FromQuery] string? keyword, [FromQuery] int? categoryId)
        {
            var datas = _productService.GetProductList(keyword, categoryId);
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
                    Data = new { id = newId }
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
    }
}