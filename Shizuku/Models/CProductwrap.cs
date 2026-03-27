using Shizuku.ViewModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Shizuku.Models
{
    public class CProductwrap
    {
        private TProduct _prod;
        public TProduct product
        {
            get { return _prod; }
            set { _prod = value; }
        }
        public CProductwrap()
        {
            _prod=new TProduct();
            Variants = new List<ProductVariantItem>();
        }
        // --- 3. TProduct 原始欄位包裝 (讓 View 好選、好顯示) ---

        public int FId { get => _prod.FId; set => _prod.FId = value; }

        [DisplayName("商品名稱")]
        [Required(ErrorMessage = "請填寫商品名稱")]
        public string FName { get => _prod.FName; set => _prod.FName = value; }

        [DisplayName("價格")]
        [DisplayFormat(DataFormatString = "{0:C0}")]
        public decimal FPrice { get => _prod.FPrice; set => _prod.FPrice = value; }

        // --- 4. 圖片處理 (從 TProductImage 跨表抓取的欄位) ---

        [DisplayName("商品主圖")]
        public string? FImage { get; set; } // 用來存資料庫中的 fImageUrl

        [DisplayName("上傳新圖片")]
        public IFormFile? Photo { get; set; } // 用於新增/修改時接收檔案

        [DisplayName("產品分類")]
        public int FCategoryId
        {
            get => _prod.FCategoryId;
            set => _prod.FCategoryId = value;
        }

        [DisplayName("產品描述")]
        public string? FDescription
        {
            get => _prod.FDescription;
            set => _prod.FDescription = value;
        }

        // --- 5. 規格處理 (List 顯示規格用) ---

        [DisplayName("產品規格與庫存")]
        public List<ProductVariantItem> Variants { get; set; }

        [DisplayName("顏色")]
        public string? FColor { get; set; }

        [DisplayName("尺寸")]
        public string? FSize { get; set; }

        [DisplayName("初始庫存")]
        [Range(0, 9999, ErrorMessage = "庫存不可為負數")]
        public int FStock { get; set; }

        [DisplayName("顏色")]
        public int FColorId { get; set; }  // 接收顏色下拉選單選中的 ID

        [DisplayName("尺寸")]
        public int FSizeId { get; set; }   // 接收尺寸下拉選單選中的 ID



        [DisplayName("商品編號")]
        public string FProduct { get => _prod.FProduct; set => _prod.FProduct = value; }

        [DisplayName("上架狀態")]
        public byte FStatus { get => _prod.FStatus; set => _prod.FStatus = (byte)value; }
    }


    // 規格項目的小類別 (可以放在同個檔案或分開)
    public class ProductVariantItem
    {
        public string? Color { get; set; }


        public string? Size { get; set; }
        public int Stock { get; set; }
    }
}

 