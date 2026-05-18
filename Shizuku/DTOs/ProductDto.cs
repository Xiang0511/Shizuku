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
        public decimal? fMinPrice { get; set; }

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
        public int fId { get; set; }  
        public string fColor { get; set; } = string.Empty;
        public string fSize { get; set; } = string.Empty;
        public int fStock { get; set; }
        public decimal? fPrice { get; set; }
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
        public int fId { get; set; }
        public string fullName { get; set; } = string.Empty;
    }
    /// <summary>規格庫存編輯用 DTO</summary>
    public class VariantEditDto
    {
        public int fId { get; set; }          // tProductVariants.fId
        public string? fColor { get; set; }    // 顏色名稱（顯示用）
        public string? fSize { get; set; }     // 尺寸名稱（顯示用）
        public int fStock { get; set; }       // 可編輯
        public string? fSkuCode { get; set; }  // 唯讀顯示
        public decimal? fPrice { get; set; }
    }
    // =============================================
    // 後台 Dashboard 用
    // =============================================

    /// <summary>商品銷售分析 DTO</summary>
    public class ProductSalesDto
    {
        public int fProductId { get; set; }
        public string fProductName { get; set; } = string.Empty;
        public string fProduct { get; set; } = string.Empty;
        public int fTotalSold { get; set; }
        public decimal fTotalRevenue { get; set; }
        public string fStatus { get; set; } = string.Empty;
    }

    /// <summary>Dashboard 統計 DTO</summary>
    public class ProductStatsDto
    {
        public int fTotalProducts { get; set; }
        public int fActiveProducts { get; set; }
        public int fOfflineProducts { get; set; }
        public int fTotalStock { get; set; }
        public int fLowStockCount { get; set; }
        public int fSoldOutCount { get; set; }
        public decimal fTotalRevenue { get; set; }
        public List<ProductSalesDto> fHotProducts { get; set; } = new();
        public List<ProductSalesDto> fSlowProducts { get; set; } = new();
        public List<CategoryStatDto> fCategoryStats { get; set; } = new();
    }

    /// <summary>分類統計 DTO</summary>
    public class CategoryStatDto
    {
        public string fCategoryName { get; set; } = string.Empty;
        public int fProductCount { get; set; }
    }

    /// <summary>庫存總覽 DTO</summary>
    public class InventoryDto
    {
        public int fProductId { get; set; }
        public string fProductName { get; set; } = string.Empty;
        public int fVariantId { get; set; }
        public string fSkuCode { get; set; } = string.Empty;
        public int fStock { get; set; }
        public string fColor { get; set; } = string.Empty;
        public string fSize { get; set; } = string.Empty;
        public string fStockStatus { get; set; } = string.Empty;
    }
    /// <summary>進貨紀錄 DTO</summary>
    public class StockRecordDto
    {
        public int fId { get; set; }
        public int fVariantId { get; set; }
        public string fColor { get; set; } = string.Empty;
        public string fSize { get; set; } = string.Empty;
        public string fProductName { get; set; } = string.Empty;
        public string fType { get; set; } = string.Empty;
        public int fQuantity { get; set; }
        public decimal? fCostPrice { get; set; }
        public string? fNote { get; set; }
        public DateTime fCreatedAt { get; set; }
    }

    /// <summary>新增進貨 DTO</summary>
    public class StockRecordCreateDto
    {
        public int fVariantId { get; set; }
        public int fQuantity { get; set; }
        public decimal? fCostPrice { get; set; }
        public string? fNote { get; set; }
        public string fType { get; set; } = "進貨";
    }

    // 規格庫存（子列）
    public class InventoryVariantDto
    {
        public int fVariantId { get; set; }
        public string fSkuCode { get; set; } = string.Empty;
        public string fColor { get; set; } = string.Empty;
        public string fSize { get; set; } = string.Empty;
        public int fStock { get; set; }
        public decimal fPrice { get; set; }
        public decimal? fCostPrice { get; set; }
        public string fStockStatus { get; set; } = string.Empty;
    }

    // 商品庫存（主列）
    public class InventoryProductDto
    {
        public int fProductId { get; set; }
        public string fProductName { get; set; } = string.Empty;
        public string fProduct { get; set; } = string.Empty;
        public string? fImage { get; set; }
        public int fTotalStock { get; set; }
        public List<InventoryVariantDto> fVariants { get; set; } = new();
    }
    /// <summary>進貨單列表 DTO</summary>
    public class PurchaseOrderDto
    {
        public int fId { get; set; }
        public string fOrderNo { get; set; } = string.Empty;
        public string? fSupplier { get; set; }
        public string? fPaymentMethod { get; set; }
        public string? fNote { get; set; }
        public int fTotalQuantity { get; set; }
        public decimal fTotalAmount { get; set; }
        public int fItemCount { get; set; }
        public DateTime fCreatedAt { get; set; }
    }

    /// <summary>進貨單明細 DTO</summary>
    public class PurchaseOrderDetailDto
    {
        public int fId { get; set; }
        public int fVariantId { get; set; }
        public string fProductName { get; set; } = string.Empty;
        public string fColor { get; set; } = string.Empty;
        public string fSize { get; set; } = string.Empty;
        public int fQuantity { get; set; }
        public decimal? fCostPrice { get; set; }
        public decimal? fAmount { get; set; }
        public string? fNote { get; set; }
    }

    /// <summary>進貨單詳細（含明細）</summary>
    public class PurchaseOrderFullDto
    {
        public int fId { get; set; }
        public string fOrderNo { get; set; } = string.Empty;
        public string? fSupplier { get; set; }
        public string? fPaymentMethod { get; set; }
        public string? fNote { get; set; }
        public int fTotalQuantity { get; set; }
        public decimal fTotalAmount { get; set; }
        public DateTime fCreatedAt { get; set; }
        public List<PurchaseOrderDetailDto> fDetails { get; set; } = new();
    }

    /// <summary>新增進貨單 DTO</summary>
    public class PurchaseOrderCreateDto
    {
        public string? fSupplier { get; set; }
        public string? fPaymentMethod { get; set; }
        public string? fNote { get; set; }
        public List<PurchaseOrderDetailCreateDto> fDetails { get; set; } = new();
    }

    /// <summary>新增進貨單明細 DTO</summary>
    public class PurchaseOrderDetailCreateDto
    {
        public int fVariantId { get; set; }
        public int fQuantity { get; set; }
        public decimal? fCostPrice { get; set; }
        public string? fNote { get; set; }
    }
}