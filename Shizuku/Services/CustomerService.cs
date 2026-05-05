using Shizuku.Models;
using Shizuku.Wraps;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Shizuku.DTOs;

namespace Shizuku.Services
{
    public class CustomerService
    {
        private readonly DbShizukuDemoContext _db;

        public CustomerService(DbShizukuDemoContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 取得封裝後的案件清單
        /// </summary>
        public List<CTicketCustomerWrap> GetTickets(string txtKeyword, string status = "")
        {
            var query = _db.TTicketsCustomers
                           .Include(t => t.FCategory)
                           .Where(p => p.FIsDeleted != true);

            // 狀態篩選
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.FStatus == status);
            }

            // 關鍵字篩選
            if (!string.IsNullOrEmpty(txtKeyword))
            {
                query = query.Where(p => p.FSubject.Contains(txtKeyword) ||
                                        (p.FCategory != null && p.FCategory.FName.Contains(txtKeyword)));
            }

            // 將資料撈出後，轉型為 Wrap 物件回傳
            return query.ToList()
                        .Select(t => new CTicketCustomerWrap(t))
                        .ToList();
        }

        /// <summary>
        /// 取得封裝後的分類清單
        /// </summary>
        public List<CTicketCategoryWrap> GetCategories(string txtKeyword)
        {
            var query = _db.TTicketCategories.Where(c => c.FIsDeleted != true);

            if (!string.IsNullOrEmpty(txtKeyword))
            {
                query = query.Where(c => c.FName.Contains(txtKeyword) || c.FDescription.Contains(txtKeyword));
            }

            return query.ToList()
                        .Select(c => new CTicketCategoryWrap(c))
                        .ToList();
        }

        /// <summary>
        /// 取得單一案件並封裝
        /// </summary>
        public CTicketCustomerWrap GetTicketById(int id)
        {
            var ticket = _db.TTicketsCustomers.FirstOrDefault(t => t.FId == id);

            if (ticket == null)
            {
                return null;
            }

            return new CTicketCustomerWrap(ticket);
        }

        /// <summary>
        /// 取得下拉選單用分類資料
        /// </summary>
        public List<SelectListItem> GetCategorySelectList()
        {
            return _db.TTicketCategories
                      .Where(c => c.FIsDeleted != true)
                      .Select(c => new SelectListItem
                      {
                          Value = c.FId.ToString(),
                          Text = c.FName
                      }).ToList();
        }

        /// <summary>
        /// 儲存案件修改
        /// </summary>
        public void UpdateTicket(CTicketCustomerWrap wrap)
        {
            if (wrap == null)
            {
                return;
            }

            // 從 Wrap 裡面拿出原始的 Entity 進行存檔
            var x = _db.TTicketsCustomers.FirstOrDefault(p => p.FId == wrap.Entity.FId);

            if (x == null)
            {
                return;
            }

            x.FCategoryId = wrap.Entity.FCategoryId;
            x.FSubject = wrap.Entity.FSubject;
            x.FStatus = wrap.Entity.FStatus;
            x.FPriority = wrap.Entity.FPriority;
            x.FAssignedAgentId = wrap.Entity.FAssignedAgentId;
            x.FUpdatedAt = DateTime.Now;

            _db.SaveChanges();
        }

        /// <summary>
        /// 軟刪除案件
        /// </summary>
        public void DeleteTicket(int id)
        {
            var x = _db.TTicketsCustomers.FirstOrDefault(t => t.FId == id);

            if (x == null)
            {
                return;
            }

            x.FIsDeleted = true;
            _db.SaveChanges();
        }

        /// <summary>
        /// 取得單一分類並封裝
        /// </summary>
        public CTicketCategoryWrap GetCategoryById(int id)
        {
            var category = _db.TTicketCategories.FirstOrDefault(c => c.FId == id);

            if (category == null)
            {
                return null;
            }

            return new CTicketCategoryWrap(category);
        }

        /// <summary>
        /// 儲存分類修改
        /// </summary>
        public void UpdateCategory(CTicketCategoryWrap wrap)
        {
            if (wrap == null)
            {
                return;
            }

            var x = _db.TTicketCategories.FirstOrDefault(c => c.FId == wrap.Entity.FId);

            if (x == null)
            {
                return;
            }

            x.FName = wrap.Entity.FName;
            x.FDescription = wrap.Entity.FDescription;
            _db.SaveChanges();
        }

        /// <summary>
        /// 軟刪除分類
        /// </summary>
        public void DeleteCategory(int id)
        {
            var x = _db.TTicketCategories.FirstOrDefault(c => c.FId == id);

            if (x == null)
            {
                return;
            }

            x.FIsDeleted = true;
            _db.SaveChanges();
        }
        /// <summary>
		/// 接收 Vue 前台表單並存入資料庫
		/// </summary>
		public bool CreateTicketFromVue(VueTicketDto dto)
        {
            // 早失敗原則
            if (dto == null)
            {
                return false;
            }

            // 將前端沒有對應的欄位，組合進主旨裡讓客服人員看
            string combinedSubject = $"[訪客: {dto.LastName}{dto.FirstName} | {dto.Email}] {dto.Subject} - 內容: {dto.Description}";

            var newTicket = new TTicketsCustomer
            {
                FMemberId = 0, // 假設 0 代表未登入的訪客
                FCategoryId = dto.CategoryId == 0 ? 1 : dto.CategoryId, // 若沒選分類則預設 1
                FSubject = combinedSubject,
                FStatus = "待處理",
                FPriority = "中",
                FCreatedAt = DateTime.Now,
                FIsDeleted = false
            };

            _db.TTicketsCustomers.Add(newTicket);
            _db.SaveChanges();

            return true;
        }
       
		
    }
}