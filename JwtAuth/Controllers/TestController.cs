using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult Public()
        {
            return Ok(new { message = "Cette page est accessible à tous" });
        }

        [HttpGet("authenticated")]
        [Authorize]
        public IActionResult Authenticated()
        {
            return Ok(new { message = "Cette page nécessite une authentification" });
        }

        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Ok(new { message = "Cette page est réservée aux administrateurs" });
        }

        [HttpGet("manager-only")]
        [Authorize(Roles = "Manager")]
        public IActionResult ManagerOnly()
        {
            return Ok(new { message = "Cette page est réservée aux managers" });
        }

        [HttpGet("admin-or-manager")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult AdminOrManager()
        {
            return Ok(new { message = "Cette page est accessible aux admins et managers" });
        }
    }
}