using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

#nullable disable warnings

public class BookInsertVM
{
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(255, ErrorMessage = "{0} must be between 1 and 255 characters.")]
    public string Title { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "{0} must be between 10 and 2000 characters")]
    public string Description { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(17, MinimumLength = 10, ErrorMessage = "{0} must be between 10 and 17 characters.")]
    [ISBNValidation]
    public string ISBN { get; set; }

    [DisplayName("Publication Year")]
    [Required(ErrorMessage = "{0} is required.")]
    [Range(1450, 2100, ErrorMessage = "{0} must be between 1450 and 2100.")]
    public int PublicationYear { get; set; }
}