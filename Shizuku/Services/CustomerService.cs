using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shizuku.DTOs;
using Shizuku.Models;
using Shizuku.Wraps;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

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
                    FMemberId = dto.MemberId,    //  修正：把 0 換成 dto.MemberId，讓前端傳來的 ID 順利存進資料庫！
                    FCategoryId = dto.CategoryId == 0 ? 1 : dto.CategoryId,
                    FGuestName = dto.Name,
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
            // 防呆：如果傳來的訊息是空的，直接回傳預設訊息
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return "不好意思，我不太明白您的意思。您可以嘗試詢問關於運費、退換貨或門市的問題，或是填寫聯絡表單，我們會盡快由專人為您解答。";
            }

            // 1. 將客人的訊息去除前後空白，並「全部轉成小寫」，避免大小寫比對失敗
            string searchMsg = userMessage.Trim().ToLower();

            // 2. 從資料庫把問答集撈出來 (因為題庫通常不多，全部撈出來比對效能很 OK)
            var faqs = await _db.TChatbotFaqs.ToListAsync();

            // 3. 用 LINQ 找出第一個匹配的答案
            var matchedFaq = faqs.FirstOrDefault(faq =>
                !string.IsNullOrWhiteSpace(faq.fKeyword) &&      // 防呆：確保資料庫裡的關鍵字不是空的
                searchMsg.Contains(faq.fKeyword.ToLower())       // 關鍵：將資料庫關鍵字也轉小寫後進行模糊比對
            );

            // 4. 如果有找到對應的答案，就回傳
            if (matchedFaq != null)
            {
                return matchedFaq.fAnswer;
            }

            // 5. 如果所有的關鍵字都沒匹配到，回傳預設訊息
            return "不好意思，我不太明白您的意思。您可以嘗試詢問關於運費、退換貨或門市的問題，或是填寫聯絡表單，我們會盡快由專人為您解答。";
        }
        // 根據會員 ID 撈取歷史客服紀錄
        public async Task<List<TicketHistoryDto>> GetMemberTicketsAsync(int memberId)
        {
            var list = await _db.TTicketsCustomers
                .Where(t => t.FMemberId == memberId)
                .OrderByDescending(t => t.FCreatedAt)
                .Select(t => new TicketHistoryDto
                {
                    Id = t.FId,
                    Category = "問題分類 " + t.FCategoryId.ToString(), // 若有關聯表可換成分類名稱
                    Subject = t.FSubject,
                    Content = t.FDescription, // 客人的發問內容
                    CreateTime = t.FCreatedAt.HasValue ? t.FCreatedAt.Value.ToString("yyyy-MM-dd HH:mm") : "",
                    Status = (t.FStatus == "Closed" || t.FStatus == "已處理") ? 1 : 0
                })
                .ToListAsync();

            return list;
        }
        /// <summary>
        /// 後台專用：撈取全站所有的客服表單紀錄
        /// </summary>
        public async Task<List<AdminTicketListDto>> GetAllTicketsForAdminAsync()
        {
            var list = await _db.TTicketsCustomers
                .OrderByDescending(t => t.FCreatedAt) // 讓最新發問的表單排在最上面
                .Select(t => new AdminTicketListDto
                {
                    Id = t.FId,
                    MemberId = t.FMemberId,
                    // 防呆：如果客人沒填名字或信箱，就給預設值
                    GuestName = string.IsNullOrEmpty(t.FGuestName) ? "無名氏" : t.FGuestName,
                    Email = string.IsNullOrEmpty(t.FGuestEmail) ? "無" : t.FGuestEmail,
                    Category = "問題分類 " + t.FCategoryId.ToString(),
                    Subject = t.FSubject,
                    Content = t.FDescription,
                    Status = string.IsNullOrEmpty(t.FStatus) ? "待處理" : t.FStatus,
                    CreateTime = t.FCreatedAt.HasValue ? t.FCreatedAt.Value.ToString("yyyy-MM-dd HH:mm") : ""
                })
                .ToListAsync();

            return list;
        }
        /// <summary>
        /// 後台專用：更新客服表單狀態
        /// </summary>
        public async Task<bool> UpdateTicketStatusAsync(int ticketId, string newStatus)
        {
            try
            {
                // 1. 去資料庫把那筆表單找出來
                var ticket = await _db.TTicketsCustomers.FirstOrDefaultAsync(t => t.FId == ticketId);

                if (ticket == null) return false; // 防呆：找不到就不做事

                // 2. 更新狀態，並順手記錄最後修改時間
                ticket.FStatus = newStatus;
                ticket.FUpdatedAt = DateTime.Now;

                // 3. 存檔
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}