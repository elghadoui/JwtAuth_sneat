using JwtAuthApi.Models;
using JwtAuthApi.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto model)
        {
            if (await _roleManager.RoleExistsAsync(model.RoleName))
            {
                return BadRequest(new { message = "Ce rôle existe déjà" });
            }

            var role = new IdentityRole(model.RoleName);
            var result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                return Ok(new { message = $"Rôle '{model.RoleName}' créé avec succès" });
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("list")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<string>>> GetRoles()
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return Ok(roles);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleDto model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé" });
            }

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
            {
                return NotFound(new { message = "Rôle non trouvé" });
            }

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);

            if (result.Succeeded)
            {
                return Ok(new { message = $"Rôle '{model.RoleName}' attribué à {model.Username}" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] AssignRoleDto model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé" });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, model.RoleName);

            if (result.Succeeded)
            {
                return Ok(new { message = $"Rôle '{model.RoleName}' retiré de {model.Username}" });
            }

            return BadRequest(result.Errors);
        }


        [HttpGet("user/{username}")]
        public async Task<IActionResult> GetUserRoles(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }
    }
}
