using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Ping OK");
        }
    }
}
