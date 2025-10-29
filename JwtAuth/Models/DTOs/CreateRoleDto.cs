using System.ComponentModel.DataAnnotations;

namespace JwtAuthApi.Models.DTOs
{
    public class CreateRoleDto
    {
        [Required]
        public string RoleName { get; set; }
    }
}