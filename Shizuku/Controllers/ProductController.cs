using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shizuku.Models;
using Shizuku.Services;
using Shizuku.ViewModels;
using System.Drawing;

namespace Shizuku.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductService _productService;

        //Controller 的建構子有接 ProductService
        public ProductController(ProductService productService, DbShizukuDemoContext context)
        {
            _productService = productService;
        }

        /// <summary>商品列表頁</summary>
        public IActionResult List(string txtKeyword)
        {
            var datas = _productService.GetProductList(txtKeyword);
            ViewBag.Keyword = txtKeyword;
            return View(datas);
        }

        /// <summary>軟刪除商品</summary>
        public IActionResult Delete(int id)
        {
            _productService.SoftDelete(id);
            return RedirectToAction("List");
        }

        /// <summary>編輯頁（GET）</summary>
        public IActionResult Edit(int id)
        {
            var dto = _productService.GetForEdit(id);

            if (dto == null) return RedirectToAction("List");

            ViewBag.Variants = _productService.GetVariants(id);
            return View(dto);

        }

        /// <summary>編輯頁（POST）</summary>
        [HttpPost]
        public async Task<IActionResult> Edit(ProductEditDto dto,
    List<int> variantIds, List<int> variantStocks)
        {
            _productService.Update(dto);

            // 儲存圖片
            if (dto.fPhoto != null)
                await _productService.SaveImageAsync(dto.fId, dto.fPhoto);

            // 組合規格庫存並批次更新
            var variants = variantIds
                .Select((id, i) => new VariantEditDto
                {
                    fId = id,
                    fStock = variantStocks.ElementAtOrDefault(i)
                }).ToList();

            _productService.UpdateVariantStocks(variants);

            return RedirectToAction("List");
        }


        /// <summary>新增頁（GET），準備下拉選單</summary>
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new ProductCreateDto());
        }

        /// <summary>新增頁（POST）</summary>
        [HttpPost]
        public async Task<IActionResult> Create(ProductCreateDto dto)
        {
            int? newId = _productService.Create(dto);

            if (newId == null)
            {
                PopulateDropdowns();
                return View(dto);
            }

            if (dto.fPhoto != null)
                await _productService.SaveImageAsync(newId.Value, dto.fPhoto);

            return RedirectToAction("List");
        }

        /// <summary>將下拉選單資料寫入 ViewBag（私有輔助方法）</summary>
        private void PopulateDropdowns()
        {
            var (categories, colors, sizes) = _productService.GetDropdownData();

            ViewBag.fCategoryId = new SelectList(categories, "ID", "FullName");
            ViewBag.fColorId = new SelectList(colors, "FId", "FName");
            ViewBag.fSizeId = new SelectList(sizes, "FId", "FName");
        }
    }
}
