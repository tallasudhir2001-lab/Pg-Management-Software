using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var result = await _userService.GetUsersAsync(pgId, page, pageSize);
            return Ok(result);
        }
    }
}
