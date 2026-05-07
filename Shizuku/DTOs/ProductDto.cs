using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Shizuku.ViewModels
{
    // =============================================
    // 列表頁 DTO
    // =============================================
    public class ProductListDto
    {
        [DisplayName("商品 ID")]
        public int fId { get; set; }

        [DisplayName("商品名稱")]
        public string fName { get; set; }

        [DisplayName("商品編號")]
        public string fProduct { get; set; }

        [DisplayName("價格")]
        [DisplayFormat(DataFormatString = "{0:C0}")]
        public decimal fPrice { get; set; }

        [DisplayName("上架狀態")]
        public byte fStatus { get; set; }

        [DisplayName("商品主圖")]
        public string? fImage { get; set; }

        [DisplayName("產品規格與庫存")]
        public List<VariantSummaryDto> Variants { get; set; } = new();
    }

    // =============================================
    // 規格摘要（列表頁內嵌用）
    // =============================================
    public class VariantSummaryDto
    {
        [DisplayName("顏色")]
        public string fColor { get; set; }

        [DisplayName("尺寸")]
        public string fSize { get; set; }

        [DisplayName("庫存")]
        public int fStock { get; set; }
    }

    // =============================================
    // 編輯頁 DTO（GET 顯示 + POST 接收）
    // =============================================
    public class ProductEditDto
    {
        public int fId { get; set; }

        [DisplayName("商品名稱")]
        [Required(ErrorMessage = "請填寫商品名稱")]
        public string fName { get; set; }

        [DisplayName("商品編號")]
        public string fProduct { get; set; }

        [DisplayName("價格")]
        [DisplayFormat(DataFormatString = "{0:C0}")]
        public decimal fPrice { get; set; }

        [DisplayName("上架狀態")]
        public byte fStatus { get; set; }

        [DisplayName("產品分類")]
        public int fCategoryId { get; set; }

        [DisplayName("產品描述")]
        public string? fDescription { get; set; }

        [DisplayName("商品主圖")]
        public string? fImage { get; set; }

         }

    // =============================================
    // 新增頁 DTO（含規格下拉選單欄位）
    // =============================================
    public class ProductCreateDto
    {
        [DisplayName("商品名稱")]
        [Required(ErrorMessage = "請填寫商品名稱")]
        public string fName { get; set; }

        [DisplayName("價格")]
        [Required(ErrorMessage = "請填寫價格")]
        public decimal fPrice { get; set; }

        [DisplayName("產品分類")]
        [Required(ErrorMessage = "請選擇分類")]
        public int fCategoryId { get; set; }

        [DisplayName("產品描述")]
        public string? fDescription { get; set; }

        //[DisplayName("上傳圖片")]
        //public IFormFile? fPhoto { get; set; }

        //[DisplayName("顏色")]
        //[Required(ErrorMessage = "請選擇顏色")]
        //public int fColorId { get; set; }

        //[DisplayName("尺寸")]
        //[Required(ErrorMessage = "請選擇尺寸")]
        //public int fSizeId { get; set; }

        //[DisplayName("初始庫存")]
        //[Range(0, 9999, ErrorMessage = "庫存不可為負數")]
        //public int fStock { get; set; }


        // ✨ 這個不能少，前端 Variants[0].fColorId 才有地方綁
        [DisplayName("規格列表")]
        public List<VariantInputDto> Variants { get; set; } = new();
    }
    public class VariantInputDto
    {
        public int fColorId { get; set; }
        public int fSizeId { get; set; }

        [Range(0, 9999, ErrorMessage = "庫存不可為負數")]
        public int fStock { get; set; }
    }
    /// <summary>分類下拉選單用</summary>
    public class CategoryOptionDto
    {
        public int ID { get; set; }
        public string FullName { get; set; }
    }
    /// <summary>規格庫存編輯用 DTO</summary>
    public class VariantEditDto
    {
        public int fId { get; set; }          // tProductVariants.fId
        public string? fColor { get; set; }    // 顏色名稱（顯示用）
        public string? fSize { get; set; }     // 尺寸名稱（顯示用）
        public int fStock { get; set; }       // 可編輯
        public string? fSkuCode { get; set; }  // 唯讀顯示
    }
}