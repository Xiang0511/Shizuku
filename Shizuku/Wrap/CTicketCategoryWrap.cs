using Shizuku.Models;
using System.ComponentModel.DataAnnotations;

namespace Shizuku.Wraps
{
    public class CTicketCategoryWrap
    {
        private TTicketCategory _category;

        // 👇 這個一定要加，接 POST 表單用的 👇
        public CTicketCategoryWrap()
        {
            _category = new TTicketCategory();
        }

        public CTicketCategoryWrap(TTicketCategory category)
        {
            _category = category;
        }

        public int fId { get => _category.FId; }

        [Display(Name = "分類名稱")]
        [Required(ErrorMessage = "請輸入分類名稱")]
        public string? fName { get => _category.FName; set => _category.FName = value; }

        [Display(Name = "描述")]
        public string? fDescription { get => _category.FDescription; set => _category.FDescription = value; }

        public TTicketCategory Entity { get => _category; }
    }
}