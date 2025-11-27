using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/demo")]
    public class DemoController : ControllerBase
    {
        [HttpGet("fixed")]
        public IActionResult FixedDemo()
        {
            return Ok("Fixed window demo endpoint");
        }

        [HttpGet("sliding")]
        public IActionResult SlidingDemo()
        {
            return Ok("Sliding window demo endpoint");
        }

        [HttpGet("token")]
        public IActionResult TokenBucketDemo()
        {
            return Ok("Token bucket demo endpoint");
        }
    }
}
