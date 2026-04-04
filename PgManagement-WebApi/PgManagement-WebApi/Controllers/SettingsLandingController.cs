using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/settings")]
    [ApiController]
    [Authorize]
    public class SettingsLandingController : ControllerBase
    {
        // GET api/settings
        // This endpoint serves as the access point for the Settings nav button.
        [AccessPoint("Settings", "View Settings")]
        [HttpGet]
        public IActionResult GetSettings()
        {
            return Ok(new { message = "Access granted" });
        }
    }
}
