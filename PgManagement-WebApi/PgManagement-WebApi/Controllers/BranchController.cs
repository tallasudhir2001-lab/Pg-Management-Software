using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/branch")]
    [ApiController]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public BranchController(IBranchService branchService)
        {
            _branchService = branchService;
        }

        [AccessPoint("Branch", "Toggle Branch View")]
        [HttpGet("can-toggle")]
        public IActionResult ToggleBranchView()
        {
            var branchId = User.FindFirst("branchId")?.Value;
            return Ok(new { canViewBranch = !string.IsNullOrEmpty(branchId) });
        }

        [HttpGet("user-pgs")]
        public async Task<IActionResult> GetUserBranchPgs()
        {
            var branchId = User.FindFirst("branchId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(branchId) || string.IsNullOrEmpty(userId))
                return Ok(new List<object>());

            var pgs = await _branchService.GetUserBranchPgsAsync(userId, branchId);
            return Ok(pgs);
        }
    }
}
