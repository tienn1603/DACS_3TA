using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_DACS.DTOs;
using web_DACS.Services.Interfaces;

namespace web_DACS.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _accountService.RegisterAsync(
                request.Username,
                request.Email,
                request.Password,
                request.FullName,
                request.PhoneNumber,
                request.Address);

            if (!result.Status) return result;
            return result;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _accountService.LoginAsync(request.Username, request.Password);
            if (!result.Status) return Unauthorized(result);
            return Ok(result);
        }
    }
}
