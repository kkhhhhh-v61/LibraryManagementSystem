using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress]
        public required string Email { get; set; }
    }
}