using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/configurations")]
    [ApiController]
    [Authorize]
    public class ConfigurationsController : ControllerBase
    {
        // GET api/configurations
        // This endpoint serves as the access point for the Configurations nav button.
        [AccessPoint("Configurations", "View Configurations")]
        [HttpGet]
        public IActionResult GetConfigurations()
        {
            return Ok(new { message = "Access granted" });
        }
    }
}
