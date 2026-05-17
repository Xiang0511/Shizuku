using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;

namespace Shizuku.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeApiController : ControllerBase
    {
        private readonly EmployeeService _employeeService;

        // 建構子注入：消除對資料庫內容的直接耦合，將身分認證職責委託給專責服務 EmployeeService，提升架構解耦性
        public EmployeeApiController(EmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        // 員工後台登入 API (POST /api/EmployeeApi/login)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] EmployeeLoginDto request)
        {
            try
            {
                // 關注點分離：將身分與在職驗證移入 Service，Controller 只負責封裝與處理回應
                var loginResult = await _employeeService.ValidateLoginAsync(request.FNumber, request.FPassword);

                if (!loginResult.Success || loginResult.Data == null)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = false,
                        Message = loginResult.Message,
                        Data = null
                    });
                }

                var employee = loginResult.Data;

                // 登入成功：回傳員工資料，並加上 isEmployee 標記，供前端 Vue 做後台權限路由判斷
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "登入成功",
                    Data = new
                    {
                        fId = employee.FId,
                        fName = employee.FName,
                        fNumber = employee.FNumber,
                        isEmployee = true
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"登入處置失敗: {ex.Message}",
                    Data = null
                });
            }
        }
    }
}
