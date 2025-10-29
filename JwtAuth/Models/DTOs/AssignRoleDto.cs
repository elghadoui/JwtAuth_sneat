using System.ComponentModel.DataAnnotations;

namespace JwtAuthApi.Models.DTOs
{
    public class AssignRoleDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string RoleName { get; set; }
    }
}