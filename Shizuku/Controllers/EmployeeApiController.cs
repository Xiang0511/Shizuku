using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Models;

namespace Shizuku.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeApiController : ControllerBase
    {
        private readonly DbShizukuDemoContext _db;

        public EmployeeApiController(DbShizukuDemoContext db)
        {
            _db = db;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] EmployeeLoginDto request)
        {
            // 去資料庫比對員工編號與密碼
            var employee = _db.TEmployees.FirstOrDefault(e =>
                e.FNumber == request.FNumber &&
                e.FPassword == request.FPassword
            );

            if (employee == null)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "員工編號或密碼錯誤"
                });
            }

            if (employee.FStatus != "在職")
            {
                return Ok(new ApiResponse<object>
                {
                    Success = false,
                    Message = "此帳號已停用，請聯絡管理員"
                });
            }

            // 成功：回傳員工資料，並加上 isEmployee 標記
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "登入成功",
                Data = new
                {
                    fId = employee.FId,
                    fName = employee.FName,
                    fNumber = employee.FNumber,
                    isEmployee = true   // ← 前端靠這個判斷是否為後台帳號
                }
            });
        }
    }
}
