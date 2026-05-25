using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shizuku.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeApiController : ControllerBase
    {
        private readonly EmployeeService _employeeService;
        private readonly IConfiguration _configuration;

        // 建構子注入：注入 EmployeeService 與 IConfiguration，讀取系統 JWT 設定自行產生 Token
        public EmployeeApiController(EmployeeService employeeService, IConfiguration configuration)
        {
            _employeeService = employeeService;
            _configuration = configuration;
        }

        // 員工後台登入 API (POST /api/EmployeeApi/login)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] EmployeeLoginDto request)
        {
            try
            {
                // 關注點分離：將身分與在職驗證移入 Service
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

                // 直接讀取 appsettings.json 裡的 JWT 設定，自行產生帶有 "Admin" 角色的後台 Token
                var token = GenerateEmployeeToken(employee.FId, employee.FName ?? "", employee.FEmail ?? "", "Admin");

                // 登入成功：回傳員工資料並附帶 token
                return Ok(new
                {
                    success = true,
                    message = "登入成功",
                    data = new
                    {
                        fId = employee.FId,
                        fName = employee.FName,
                        fNumber = employee.FNumber,
                        isEmployee = true
                    },
                    token = token
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

        /// <summary>
        /// 本地端專用：自行讀取 JWT 設定，簽發包含 "Admin" 角色宣告的員工 Token
        /// </summary>
        private string GenerateEmployeeToken(int fId, string fName, string fEmail, string role)
        {
            var secretKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, fId.ToString()), // 使用者 ID
                new Claim(JwtRegisteredClaimNames.Email, fEmail),      // Email
                new Claim("fName", fName),                             // 姓名
                new Claim(ClaimTypes.Role, role),                      // 🌟 注入 Admin 角色，供 Authorize(Roles = "Admin") 驗證
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1), // 1天後過期
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}


