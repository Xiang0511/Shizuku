using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Serilog;
using Shizuku.Infrastructure.Attributes;
using Shizuku.Services;
using Shizuku.ViewModels;

namespace Shizuku.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberApiController : ControllerBase
    {
        private readonly MemberService _memberService;

        public MemberApiController(MemberService memberService)
        {
            _memberService = memberService;
        }

        [HttpPost("login")]
        public virtual IActionResult Login([FromBody] MemberLoginViewModel vm)
        {
            //Log.Information("Login Log紀錄");
            if (vm == null || string.IsNullOrEmpty(vm.FEmail) || string.IsNullOrEmpty(vm.FPassword))
            {
                Log.Warning("登入失敗：前端傳入資料不完整或欄位名稱錯誤");
                return BadRequest(new { success = false, message = "請輸入帳號密碼" });
            }

            var member = _memberService.Login(vm.FEmail, vm.FPassword);

            if (member == null)
            {
                Log.Warning("登入失敗：帳號密碼錯誤");
                return Unauthorized(new { suceess = false, message = "帳號密碼錯誤" });
            ;
            }

            return Ok(new
            {
                success = true,
                message="登入成功",
                userName=member.FName
            });
        }

        [HttpGet("Lo")]
        public IActionResult Lo()
        {
            Log.Information("顯目的東西測試");
            return Ok(new
            {
                success = true,
                message = "登入成功",
            });
        }
    }
}
