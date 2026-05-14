using Microsoft.EntityFrameworkCore;
using Shizuku.Models;

namespace Shizuku.Services
{
    public class EmployeeService
    {
        private readonly DbShizukuDemoContext _db;

        // 透過依賴注入 (DI) 取得資料庫實體
        public EmployeeService(DbShizukuDemoContext db)
        {
            _db = db;
        }

        // 1. 取得員工列表 (含關鍵字與狀態過濾)
        public List<TEmployee> GetEmployees(string keyword, string statusFilter)
        {
            var query = _db.TEmployees.AsQueryable();

            if (statusFilter == "Active")
            {
                query = query.Where(p => p.FStatus != "離職");
            }
            else if (statusFilter == "Resigned")
            {
                query = query.Where(p => p.FStatus == "離職");
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p =>
                    p.FNumber.Contains(keyword) ||
                    p.FName.Contains(keyword) ||
                    p.FAddress.Contains(keyword) ||
                    p.FPhone.Contains(keyword) ||
                    p.FEmail.Contains(keyword) ||
                    p.FStatus.Contains(keyword) ||
                    p.FDepartmentId.ToString().Contains(keyword) ||
                    p.FHireDate.ToString().Contains(keyword)
                );
            }

            return query.ToList();
        }

        // 2. 取得單一員工
        public TEmployee GetEmployeeById(int id)
        {
            return _db.TEmployees.FirstOrDefault(p => p.FId == id);
        }

        // 3. 取得所有部門 (供下拉選單)
        public List<TDepartment> GetAllDepartments()
        {
            return _db.TDepartments.ToList();
        }

        // 4. 取得所有職位 (供下拉選單)
        public List<TPosition> GetAllPositions()
        {
            return _db.TPositions.ToList();
        }

        // 5. 新增員工與自動產生編號
        public void CreateEmployee(TEmployee p)
        {
            p.FStatus = "在職";
            p.FCreatedAt = DateTime.Now;
            p.FUpdatedAt = DateTime.Now;

            // --- 自動生成 EMP+流水號 邏輯開始 ---
            if (string.IsNullOrEmpty(p.FNumber))
            {
                var lastEmployee = _db.TEmployees
                    .Where(e => e.FNumber.StartsWith("EMP"))
                    .OrderByDescending(e => e.FNumber)
                    .FirstOrDefault();

                int nextNumber = 1;
                if (lastEmployee != null)
                {
                    string lastNumStr = lastEmployee.FNumber.Replace("EMP", "");
                    if (int.TryParse(lastNumStr, out int lastId))
                    {
                        nextNumber = lastId + 1;
                    }
                }
                p.FNumber = $"EMP{nextNumber:D3}";
            }

            _db.TEmployees.Add(p);
            _db.SaveChanges();
        }

        // 6. 更新員工資料
        public void UpdateEmployee(TEmployee e)
        {
            TEmployee dbEmployee = _db.TEmployees.FirstOrDefault(p => p.FId == e.FId);
            if (dbEmployee != null)
            {
                dbEmployee.FName = e.FName;
                dbEmployee.FPassword = e.FPassword;
                dbEmployee.FPhone = e.FPhone;
                dbEmployee.FEmail = e.FEmail;
                dbEmployee.FAddress = e.FAddress;
                dbEmployee.FDepartmentId = e.FDepartmentId;
                dbEmployee.FPositionId = e.FPositionId;
                dbEmployee.FStatus = e.FStatus; 
                dbEmployee.FUpdatedAt = DateTime.Now;

                _db.SaveChanges();
            }
        }

        // 7. 軟刪除員工 (離職)
        public void SoftDeleteEmployee(int id)
        {
            TEmployee x = _db.TEmployees.FirstOrDefault(p => p.FId == id);
            if (x != null)
            {
                x.FStatus = "離職";
                x.FUpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
    }
}
