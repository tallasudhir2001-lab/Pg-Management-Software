using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/branch")]
    [ApiController]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BranchController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Checks if the current user can toggle branch view.
        /// The AccessPoint filter enforces permission; reaching here means allowed.
        /// </summary>
        [AccessPoint("Branch", "Toggle Branch View")]
        [HttpGet("can-toggle")]
        public IActionResult ToggleBranchView()
        {
            var branchId = User.FindFirst("branchId")?.Value;
            return Ok(new { canViewBranch = !string.IsNullOrEmpty(branchId) });
        }

        /// <summary>
        /// Returns all PGs the current user has access to within their branch.
        /// Used by the frontend to display PG names when branch view is active.
        /// </summary>
        [HttpGet("user-pgs")]
        public async Task<IActionResult> GetUserBranchPgs()
        {
            var branchId = User.FindFirst("branchId")?.Value;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(branchId) || string.IsNullOrEmpty(userId))
                return Ok(new List<object>());

            var pgs = await _context.UserPgs
                .Where(up => up.UserId == userId && up.PG.BranchId == branchId)
                .Select(up => new
                {
                    pgId = up.PgId,
                    name = up.PG.Name
                })
                .ToListAsync();

            return Ok(pgs);
        }
    }
}
