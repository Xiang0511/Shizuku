using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
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
        public IActionResult Login([FromBody] MemberLoginViewModel vm)
        {
            if (vm == null || string.IsNullOrEmpty(vm.FEmail) || string.IsNullOrEmpty(vm.FPassword))
            {
                return BadRequest(new { success = false, message = "請輸入帳號密碼" });
            }

            var member = _memberService.Login(vm.FEmail, vm.FPassword);

            if (member == null)
            {
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
    }
}
