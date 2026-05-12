using CarPark.Shared;
using CarPark.Shared.Enums;

namespace CarPark.Models
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public Role Role { get; set; }

        public bool MustChangePassword { get; set; } = true;
    }
}