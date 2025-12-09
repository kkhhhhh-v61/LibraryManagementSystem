using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models;

#nullable disable warnings

public class BookInsertVM
{
    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(255)]
    public string Title { get; set; }
}