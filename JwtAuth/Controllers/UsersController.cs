using JwtAuthApi.Models;
using JwtAuthApi.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpGet("list")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();

                var userList = new List<object>();

                foreach (var user in users)
                {
                    // IMPORTANT: Récupérer les rôles pour chaque utilisateur
                    var roles = await _userManager.GetRolesAsync(user);

                    userList.Add(new
                    {
                        id = user.Id,
                        userName = user.UserName,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        emailConfirmed = user.EmailConfirmed,
                        roles = roles.ToList() // Convertir en List pour la sérialisation
                    });

                    // Log pour debug
                    Console.WriteLine($"User: {user.UserName}, Roles: {string.Join(", ", roles)}");
                }

                Console.WriteLine($"Returning {userList.Count} users");
                return Ok(userList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUsers: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Essayer plusieurs façons de récupérer le username
            var username = User.Identity?.Name
                           ?? User.FindFirst(ClaimTypes.Name)?.Value
                           ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Token invalide - username introuvable" });
            }

            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                Roles = roles
            });
        }
        // POST: api/users/create
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterDto model)
        {
            try
            {
                Console.WriteLine($"CreateUser called - Username: {model.Username}, Email: {model.Email}");

                if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                {
                    return BadRequest(new { message = "Username, Email, and Password are required" });
                }

                var existingUser = await _userManager.FindByNameAsync(model.Username);
                if (existingUser != null)
                {
                    return BadRequest(new { message = $"Username '{model.Username}' is already taken" });
                }

                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true // Auto-confirmer pour les admins
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Failed to create user: {errors}", errors = result.Errors });
                }

                Console.WriteLine($"User '{user.UserName}' created successfully with ID: {user.Id}");
                return Ok(new { message = "User created successfully", userId = user.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in CreateUser: {ex.Message}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
        // PUT: api/users/update/{id}
        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto model)
        {
            try
            {
                Console.WriteLine($"UpdateUser called - ID: {id}");
                Console.WriteLine($"Data - Email: {model.Email}, EmailConfirmed: {model.EmailConfirmed}, ChangePassword: {!string.IsNullOrEmpty(model.Password)}");

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID '{id}' not found" });
                }

                // Mettre à jour les informations de base
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.EmailConfirmed = model.EmailConfirmed;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Failed to update user: {errors}", errors = result.Errors });
                }

                // ✅ Si un nouveau mot de passe est fourni, le changer
                if (!string.IsNullOrEmpty(model.Password))
                {
                    Console.WriteLine($"Changing password for user '{user.UserName}'");

                    // Supprimer l'ancien mot de passe
                    await _userManager.RemovePasswordAsync(user);

                    // Ajouter le nouveau mot de passe
                    var passwordResult = await _userManager.AddPasswordAsync(user, model.Password);

                    if (!passwordResult.Succeeded)
                    {
                        var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                        return BadRequest(new { message = $"Failed to change password: {errors}", errors = passwordResult.Errors });
                    }

                    Console.WriteLine($"Password changed successfully for user '{user.UserName}'");
                }

                Console.WriteLine($"User '{user.UserName}' updated successfully");
                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateUser: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // DELETE: api/users/delete/{id}
        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                Console.WriteLine($"DeleteUser called - ID: {id}");

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID '{id}' not found" });
                }

                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Failed to delete user: {errors}", errors = result.Errors });
                }

                Console.WriteLine($"User '{user.UserName}' deleted successfully");
                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in DeleteUser: {ex.Message}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        //[HttpDelete("{username}")]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> DeleteUser(string username)
        //{
        //    var user = await _userManager.FindByNameAsync(username);

        //    if (user == null)
        //    {
        //        return NotFound(new { message = "Utilisateur non trouvé" });
        //    }

        //    var result = await _userManager.DeleteAsync(user);

        //    if (result.Succeeded)
        //    {
        //        return Ok(new { message = "Utilisateur supprimé avec succès" });
        //    }

        //    return BadRequest(result.Errors);
        //}

        // POST: api/users/assign-role
        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            try
            {
                // Log pour debug
                Console.WriteLine($"AssignRole called - UserId: {request?.UserId}, RoleName: {request?.RoleName}");

                if (request == null)
                {
                    return BadRequest(new { message = "Request body is null" });
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest(new { message = "UserId is required and cannot be empty" });
                }

                if (string.IsNullOrEmpty(request.RoleName))
                {
                    return BadRequest(new { message = "RoleName is required and cannot be empty" });
                }

                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID '{request.UserId}' not found" });
                }

                if (!await _roleManager.RoleExistsAsync(request.RoleName))
                {
                    return BadRequest(new { message = $"Role '{request.RoleName}' does not exist" });
                }

                if (await _userManager.IsInRoleAsync(user, request.RoleName))
                {
                    return BadRequest(new { message = $"User '{user.UserName}' already has role '{request.RoleName}'" });
                }

                var result = await _userManager.AddToRoleAsync(user, request.RoleName);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Failed to assign role: {errors}", errors = result.Errors });
                }

                Console.WriteLine($"Role '{request.RoleName}' successfully assigned to user '{user.UserName}'");
                return Ok(new { message = $"Role '{request.RoleName}' assigned successfully to user '{user.UserName}'" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AssignRole: {ex.Message}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        // POST: api/users/remove-role
        [HttpPost("remove-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request)
        {
            try
            {
                Console.WriteLine($"RemoveRole called - UserId: {request?.UserId}, RoleName: {request?.RoleName}");

                if (request == null)
                {
                    return BadRequest(new { message = "Request body is null" });
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest(new { message = "UserId is required and cannot be empty" });
                }

                if (string.IsNullOrEmpty(request.RoleName))
                {
                    return BadRequest(new { message = "RoleName is required and cannot be empty" });
                }

                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID '{request.UserId}' not found" });
                }

                if (!await _userManager.IsInRoleAsync(user, request.RoleName))
                {
                    return BadRequest(new { message = $"User '{user.UserName}' does not have role '{request.RoleName}'" });
                }

                var result = await _userManager.RemoveFromRoleAsync(user, request.RoleName);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Failed to remove role: {errors}", errors = result.Errors });
                }

                Console.WriteLine($"Role '{request.RoleName}' successfully removed from user '{user.UserName}'");
                return Ok(new { message = $"Role '{request.RoleName}' removed successfully from user '{user.UserName}'" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in RemoveRole: {ex.Message}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
        // DTO
        public class AssignRoleRequest
        {
            [JsonPropertyName("userId")]
            public string UserId { get; set; }

            [JsonPropertyName("roleName")]
            public string RoleName { get; set; }
        }
        public class UpdateUserDto
        {
            [JsonPropertyName("username")]
            public string? Username { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("firstName")]
            public string? FirstName { get; set; }

            [JsonPropertyName("lastName")]
            public string? LastName { get; set; }

            [JsonPropertyName("emailConfirmed")]
            public bool EmailConfirmed { get; set; }

            [JsonPropertyName("password")]
            public string? Password { get; set; }
        }
    }
}