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
        /// 將 Vue 傳來的表單資料寫入資料庫
        /// </summary>
        public bool CreateTicketFromVue(VueTicketDto dto)
        {
            // 1. 防呆檢查 (早失敗原則)
            if (dto == null)
            {
                return false;
            }

            try
            {
                // 2. 建立新的實體物件，將 DTO 的資料精準對應到資料庫的新欄位
                var newTicket = new TTicketsCustomer
                {
                    // 預設值設定
                    FMemberId = 0, // 訪客沒登入，預設給 0
                    FCategoryId = dto.CategoryId == 0 ? 1 : dto.CategoryId, // 如果沒選分類，預設給 1

                    // ✨ 這裡就是我們剛剛辛苦建的新欄位，完美對應！
                    FGuestName = dto.LastName + dto.FirstName, // 姓與名合併
                    FGuestEmail = dto.Email,
                    FSubject = dto.Subject,
                    FDescription = dto.Description,

                    // 系統自動填寫的欄位
                    FStatus = "待處理",
                    FPriority = "中",
                    FCreatedAt = DateTime.Now,
                    FIsDeleted = false
                };

                // 3. 存入資料庫
                _db.TTicketsCustomers.Add(newTicket);
                _db.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                // 實務上這裡可以把 ex.Message 寫進 log 裡
                Console.WriteLine($"存檔失敗: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 取得所有未刪除的客服問題分類 (給 Vue 下拉選單用)
        /// </summary>
        public object GetTicketCategories()
        {
            // 去資料庫的 TTicketCategories 表抓資料
            // 從你的截圖看到有 fIsDeleted 欄位，我們只抓 False (沒被刪除) 的分類
            var categories = _db.TTicketCategories
                .Where(c => c.FIsDeleted == false || c.FIsDeleted == null)
                .Select(c => new
                {
                    id = c.FId,       // 對應前端需要的 id
                    name = c.FName    // 對應前端需要的 name
                })
                .ToList();

            return categories;
        }
    }
}