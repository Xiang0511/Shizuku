using Microsoft.AspNetCore.Mvc;
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
        public IActionResult List([FromQuery] string? keyword)
        {
            var datas = _productService.GetProductList(keyword);
            return Ok(datas);
        }

        /// <summary>取得單筆商品（編輯用）</summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var dto = _productService.GetForEdit(id);

            if (dto == null) return NotFound(new { message = "找不到商品" });

            return Ok(dto);
        }

        /// <summary>取得商品規格庫存</summary>
        [HttpGet("{id}/variants")]
        public IActionResult GetVariants(int id)
        {
            var variants = _productService.GetVariants(id);
            return Ok(variants);
        }

        /// <summary>新增商品</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            try
            {
                int? newId = _productService.Create(dto);

                if (newId == null)
                    return BadRequest(new { message = "新增失敗，請確認分類是否正確" });

                return Ok(new { message = "新增成功", id = newId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        [HttpPost("{id}/image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return BadRequest(new { message = "請選擇圖片" });

            var imageUrl = await _productService.SaveImageAsync(id, photo);

            if (imageUrl == null)
                return BadRequest(new { message = "圖片上傳失敗" });

            return Ok(new { message = "圖片上傳成功", imageUrl });
        }
        // 另外處理圖片


        /// <summary>更新商品</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(int id,
            [FromForm] ProductEditDto dto,
            [FromForm] List<int> variantIds,
            [FromForm] List<int> variantStocks)
        {
            dto.fId = id;

            bool success = _productService.Update(dto);

            if (!success) return NotFound(new { message = "找不到商品" });

            if (dto.fPhoto != null)
                await _productService.SaveImageAsync(id, dto.fPhoto);

            var variants = variantIds
                .Select((vid, i) => new VariantEditDto
                {
                    fId = vid,
                    fStock = variantStocks.ElementAtOrDefault(i)
                }).ToList();

            _productService.UpdateVariantStocks(variants);

            return Ok(new { message = "更新成功" });
        }


        /// <summary>軟刪除商品</summary>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            bool success = _productService.SoftDelete(id);

            if (!success) return NotFound(new { message = "找不到商品" });

            return Ok(new { message = "刪除成功" });
        }

        /// <summary>取得下拉選單資料（顏色、尺寸、分類）</summary>
        [HttpGet("dropdowns")]
        public IActionResult GetDropdowns()
        {
            var (categories, colors, sizes) = _productService.GetDropdownData();

            return Ok(new
            {
                categories,
                colors,
                sizes
            });
        }
    }
}