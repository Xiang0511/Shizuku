using Shizuku.Models;
using System.ComponentModel.DataAnnotations;

namespace Shizuku.Wraps
{
	public class CTicketCustomerWrap
	{
		private TTicketsCustomer _ticket;

        // 👇 新增這個空白建構子 (給 POST 存檔接資料用的) 👇
        public CTicketCustomerWrap()
        {
            _ticket = new TTicketsCustomer();
        }

        public CTicketCustomerWrap(TTicketsCustomer ticket)
		{
			_ticket = ticket;
		}

		public int fId { get => _ticket.FId; }

		// 修正：會員編號在 DB 是 int
		[Display(Name = "會員編號")]
		public int fMemberId { get => _ticket.FMemberId; set => _ticket.FMemberId = value; }

		// 修正：訂單編號在 DB 是 int? (Nullable int)
		[Display(Name = "訂單編號")]
		public int? fOrderId { get => _ticket.FOrderId; set => _ticket.FOrderId = value; }

		[Display(Name = "案件主旨")]
		[Required(ErrorMessage = "主旨不可空白")]
		public string? fSubject { get => _ticket.FSubject; set => _ticket.FSubject = value; }

		[Display(Name = "處理狀態")]
		public string? fStatus { get => _ticket.FStatus; set => _ticket.FStatus = value; }

		[Display(Name = "優先等級")]
		public string? fPriority { get => _ticket.FPriority; set => _ticket.FPriority = value; }

		public TTicketsCustomer Entity { get => _ticket; }
	}
}