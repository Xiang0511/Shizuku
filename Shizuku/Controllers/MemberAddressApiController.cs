using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shizuku.DTOs;
using Shizuku.Services;

namespace Shizuku.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberAddressApiController : ControllerBase
    {
        private readonly MemberService _memberService;

        public MemberAddressApiController(MemberService memberService)
        {
            _memberService = memberService;
        }

        // 取得地址列表
        // GET: api/Address/5
        [HttpGet("{memberId}")]
        public async Task<ActionResult<ApiResponse<List<MemberAddressDto>>>> Get(int memberId)
        {
            var addresses = await _memberService.GetAddressesAsync(memberId);

            return Ok(new ApiResponse<List<MemberAddressDto>>
            {
                Success = true,
                Message = "讀取地址簿成功",
                Data = addresses
            });
        }

        // 更新整份地址清單
        // PUT: api/Address/5
        [HttpPut("{memberId}")]
        public async Task<ActionResult<ApiResponse<string>>> Put(int memberId, [FromBody] List<MemberAddressDto> addresses)
        {
            if (addresses == null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "傳送的資料格式有誤",
                    Data = null
                });
            }

            var success = await _memberService.UpdateAddressesAsync(memberId, addresses);

            if (!success)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = $"找不到 ID 為 {memberId} 的會員",
                    Data = null
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "地址簿更新成功",
                Data = "Updated Successfully"
            });
        }

    }
}
