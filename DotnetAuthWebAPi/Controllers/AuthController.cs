using DotnetAuthWebAPi.Domain.Contract;
using DotnetAuthWebAPi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAuthWebAPi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserServices _userService;
        public AuthController(IUserServices userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request )
        {
            var response = await _userService.RegisterAsync(request);
            return Ok(response);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var response = await _userService.LoginAsync(request);
            return Ok(response);
        }
        [HttpGet("user/{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _userService.GetByIdAsync(id);
            return Ok(response);
        }

        //refresh token
        [HttpPost("refreshToken")]
        [Authorize]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _userService.RefreshTokenAsync(request);
            return Ok(response);
        }

        //Revoke refresh token
        [HttpPost("revokeRefreshToken")]
        [Authorize]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _userService.RevokeRefreshToken(request);
           if(response!=null && response.Message== "Refresh token revoked successfully")
            {
                return Ok(response);
            }
           return BadRequest();
        }

        //Get current user
        [HttpGet("currentUser")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var response = await _userService.GetCurrentUserAsync();
            return Ok(response);
        }


    }
}
