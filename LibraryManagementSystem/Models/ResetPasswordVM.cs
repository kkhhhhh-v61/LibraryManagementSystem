using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class ResetPasswordVM
    {
        [Required]
        public string Token { get; set; }

        [Required] 
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}