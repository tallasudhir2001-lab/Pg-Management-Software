using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/version")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString(3) ?? "1.0.0";

            return Ok(new
            {
                version,
                buildDate = System.IO.File.GetLastWriteTimeUtc(assembly.Location).ToString("yyyy-MM-dd")
            });
        }
    }
}
