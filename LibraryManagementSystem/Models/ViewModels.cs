using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using static LibraryManagementSystem.Models.Room;

namespace LibraryManagementSystem.Models;

#nullable disable warnings

/* ------------------------------ LAI CHEE KIT ------------------------------ */


/* ------------------------------ TAN KAI HAO ------------------------------ */

public class BookInsertVM
{
    public IFormFile? Image { get; set; }

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

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, 100, ErrorMessage = "{0} must be between 1 and 100.")]
    public int Edition { get; set; }

    [DisplayName("Genre(s)")]
    [Required(ErrorMessage = "At least one Genre is required.")]
    public List<string> SelectedGenreIds { get; set; } = new();

    public List<SelectListItem> GenreList { get; set; } = new();

    [DisplayName("Author(s)")]
    [Required(ErrorMessage = "At least one Author is required.")]
    [MaxLength(10, ErrorMessage = "A Book cannot have more than 10 authors.")]
    public List<string> SelectedAuthorIds { get; set; } = new();

    public List<SelectListItem> AuthorList { get; set; } = new();

    public bool IsFeatured { get; set; } = false;

    public bool IsArchived { get; set; } = false;
}

public class GenreVM
{
    [DisplayName("Genre Name")]
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "{0} must be between 3 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z]+(?:[\s-&][a-zA-Z]+)*$",
    ErrorMessage = "{0} must start and end with a letter, and symbols (- and &) must be between letters.")]
    public string Name { get; set; }
}


public class AuthorVM
{
    [DisplayName("Author Name")]
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "{0} must be between 1 and 100 characters.")]
    [RegularExpression(@"^[a-zA-Z]+(?:(?:[\s\-\']|\. ?)[a-zA-Z]+)*\.?$", 
    ErrorMessage = "{0} must start with a letter, and symbols (', -, .) must be between letters.")]
    public string Name { get; set; }
}


/* ------------------------------ JASON TYE HONG YI ------------------------------ */


/* ------------------------------ BRIAN OOI SHU YI ------------------------------ */

public class RoomCategoryCreateVM
{
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, ErrorMessage = "{0} must be between 1 and 50 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(500, ErrorMessage = "{0} cannot exceed 500 characters.")]
    public string Description { get; set; }

    [DisplayName("Hourly Rate")]
    [Required(ErrorMessage = "{0} is required.")]
    [Range(0, 1000, ErrorMessage = "{0} must be between RM 0 and RM 1000.")]
    public decimal HourlyRate { get; set; }
}

public class RoomCreateVM
{
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, ErrorMessage = "{0} cannot exceed 50 characters.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(100, ErrorMessage = "{0} cannot exceed 100 characters.")]
    public string Location { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, 30, ErrorMessage = "Capacity must be between 1 and 30.")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Please select a status.")]
    public RoomStatus Status { get; set; } = RoomStatus.AVAILABLE;

    [DisplayName("Category")]
    [Required(ErrorMessage = "Please select a category.")]
    public string RoomCategoryId { get; set; }

    [DisplayName("Room Image")]
    public IFormFile? Image { get; set; }
}

public class RoomIndexVM
{
    public List<RoomCategory> Categories { get; set; } = new();

    public RoomCategoryCreateVM CategoryInput { get; set; } = new();

    public RoomCreateVM RoomInput { get; set; } = new();
}