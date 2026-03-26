using System.ComponentModel.DataAnnotations;

namespace Airline.ViewModels.Admin
{
    public class AccountFormModel
    {
        public int? UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        public string? Address { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [Range(0, 120)]
        public int? Age { get; set; }

        [StringLength(20)]
        public string? Cccd { get; set; }

        public string? Password { get; set; }

        [Required]
        [StringLength(10)]
        public string Role { get; set; } = "USER";

        [Range(0, int.MaxValue)]
        public int? SkyMiles { get; set; }
    }
}