using Microsoft.AspNetCore.Identity;

namespace DotnetAuthWebAPi.Domain.Entities
{
    public class ApplicationUser:IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime CrateAt { get; set; }
        public DateTime UpdateAt { get; set; }
    }
}
