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
        // 注意：非同步的 ToListAsync() 需要用到 EntityFrameworkCore
        // 請確定你的檔案最上方有這行： using Microsoft.EntityFrameworkCore;

        /// <summary>
        /// 將 Vue 傳來的表單資料寫入資料庫 (非同步版本)
        /// </summary>
        // 改變 1：回傳型別變成 Task<bool>，名稱加上 Async 尾綴
        public async Task<bool> CreateTicketFromVueAsync(VueTicketDto dto)
        {
            if (dto == null) return false;

            try
            {
                var newTicket = new TTicketsCustomer
                {
                    FMemberId = 0,
                    FCategoryId = dto.CategoryId == 0 ? 1 : dto.CategoryId,
                    FGuestName = dto.LastName + dto.FirstName,
                    FGuestEmail = dto.Email,
                    FSubject = dto.Subject,
                    FDescription = dto.Description,
                    FStatus = "待處理",
                    FPriority = "中",
                    FCreatedAt = DateTime.Now,
                    FIsDeleted = false
                };

                _db.TTicketsCustomers.Add(newTicket);

                // 改變 2：把 SaveChanges 改成 await SaveChangesAsync()
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine($"🚨 存檔失敗啦老哥: {ex.ToString()}");
                System.Diagnostics.Debug.WriteLine("========================================");
                return false;
            }
        }

        /// <summary>
        /// 取得所有未刪除的客服問題分類 (非同步版本)
        /// </summary>
        // 改變 3：回傳型別變成 Task<object>
        public async Task<object> GetTicketCategoriesAsync()
        {
            // 改變 4：前面加 await，最後面改成 ToListAsync()
            var categories = await _db.TTicketCategories
                .Where(c => c.FIsDeleted == false || c.FIsDeleted == null)
                .Select(c => new
                {
                    id = c.FId,
                    name = c.FName
                })
                .ToListAsync();

            return categories;
        }
        public async Task<string> GetBotResponseAsync(string userMessage)
        {
            // 1. 從資料庫的 tChatbotFaq 資料表取出所有的問答資料
            // 這邊會對應你剛剛在 DbContext 註冊的 TChatbotFaqs
            var faqs = await _db.TChatbotFaqs.ToListAsync();

            // 2. 遍歷每一筆資料，比對客人的訊息是否包含關鍵字
            foreach (var faq in faqs)
            {
                // 檢查客人的訊息是否包含資料表中的 FKeyword 欄位內容
                if (userMessage.Contains(faq.fKeyword))
                {
                    // 如果匹配成功，回傳資料庫中 FAnswer 欄位的答案
                    return faq.fAnswer;
                }
            }

            // 3. 如果資料庫中所有的關鍵字都沒匹配到，則回傳預設訊息
            // 這裡已經移除所有表情符號
            return "不好意思，我不太明白您的意思。您可以嘗試詢問關於運費、退換貨或門市的問題，或是填寫聯絡表單，我們會盡快由專人為您解答。";
        }
    }
}