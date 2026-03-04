// Models/DTOs/UpdateProfileDto.cs
namespace Lynqo_Backend.Models.DTOs
{
    public class UpdateProfileDto
    {
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string? ProfilePicUrl { get; set; }

        // Plain-text new password; will be hashed into password_hash
        public string? Password { get; set; }
    }
}
