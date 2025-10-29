using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JwtAuthApi.Models.DTOs
{
    public class RegisterDto
    {
        [JsonPropertyName("username")]
        [Required]
        public string Username { get; set; }

        [JsonPropertyName("email")]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [JsonPropertyName("password")]
        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("emailConfirmed")]
        public bool EmailConfirmed { get; set; } = true;
    }
}