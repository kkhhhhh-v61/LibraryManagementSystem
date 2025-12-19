using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using static LibraryManagementSystem.Helper;
using static LibraryManagementSystem.Models.Room;
using static LibraryManagementSystem.Models.Booking;

namespace LibraryManagementSystem.Models;

#nullable disable warnings

/* ------------------------------ LAI CHEE KIT ------------------------------ */

public class LoginVM
{
    [DisplayName("Username")]
    [Required(ErrorMessage = "{0} is required.")]
    public string Username { get; set; }

    [DisplayName("Password")]
    [Required(ErrorMessage = "{0} is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}

public class RegisterVM
{
    [DisplayName("Username")]
    // Prevents empty submissions
    [Required(ErrorMessage = "{0} is required.")]
    // Enforces length constraints for database consistency
    [StringLength(20, MinimumLength = 3, ErrorMessage = "{0} must be between 3 and 20 characters.")]
    // Allow only: Letters (a-z), Numbers (0-9), and Underscores (_)
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "{0} can only contain letters, numbers, and underscores.")]
    public string Username { get; set; }

    [DisplayName("Password")]
    [Required(ErrorMessage = "{0} is required.")]
    // Minimum 8 characters for security
    [StringLength(100, MinimumLength = 8, ErrorMessage = "{0} must be at least 8 characters long.")]
    // Strong Password Policy: Must contain 1 Upper, 1 Lower, 1 Digit, 1 Special Char
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "{0} must have 1 Uppercase, 1 Lowercase, 1 Number, and 1 Special Character.")]
    // Hides the characters in the input field
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [DisplayName("Confirm Password")]
    [Required(ErrorMessage = "{0} is required.")]
    [DataType(DataType.Password)]
    // Ensures this field matches the 'Password' field exactly
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; }

    [DisplayName("First Name")]
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "{0} must be between 1 and 50 characters.")]
    // Allow only: Letters, Spaces, and Hyphens (e.g. "Mary-Anne")
    [RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "{0} can only contain letters.")]
    public string FirstName { get; set; }

    [DisplayName("Last Name")]
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "{0} must be between 1 and 50 characters.")]
    // Allow only: Letters, Spaces, and Hyphens
    [RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "{0} can only contain letters.")]
    public string LastName { get; set; }

    [DisplayName("Email")]
    [Required(ErrorMessage = "{0} is required.")]
    // Built-in check for basic format
    [EmailAddress(ErrorMessage = "Invalid Email Address.")]
    // STRICT check: Requires @ symbol, domain name, and extension (like .com)
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email must contain a valid domain (e.g., user@example.com).")]
    public string Email { get; set; }

    [DisplayName("Contact No.")]
    [Required(ErrorMessage = "{0} is required.")]
    // Flexible Phone Format: Supports optional country code (+), brackets (), and dashes (-)
    [RegularExpression(@"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$",
        ErrorMessage = "Please enter a valid phone number (e.g., 012-3456789).")]
    public string ContactNo { get; set; }

    [DisplayName("Profile Photo")]
    // Custom validation logic for file type/size is handled in the Controller/Helper
    public IFormFile? ProfilePhoto { get; set; }
}

public class ForgotPasswordVM
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress]
    public required string Email { get; set; }
}

public class ResetPasswordVM
{
    [Required]
    public string Token { get; set; }

    [Required]
    [EmailAddress]
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

public class ProfileVM
{
    public string Username { get; set; } // Read-only usually
    public string Role { get; set; }     // Read-only

    [Display(Name = "Credibility Score")]
    public int CredibilityScore { get; set; } // Read-only

    [Required(ErrorMessage = "First Name is required.")]
    [MaxLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Last Name is required.")]
    [MaxLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [RegularExpression(@"^(\+?6?01)[02-46-9]-*[0-9]{7}$|^(\+?6?01)[1]-*[0-9]{8}$",
        ErrorMessage = "Invalid Malaysian phone number format.")]
    [Display(Name = "Phone Number")]
    public string ContactNo { get; set; }

    [Display(Name = "Public Profile")]
    public bool IsProfilePublic { get; set; }

    public string? ProfilePhotoUrl { get; set; }
}


/* ------------------------------ TAN KAI HAO ------------------------------ */

public class BookManagementVM
{
    public List<Book> ExistingBooks { get; set; } = new();

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public string? SearchTerm { get; set; }

    public string SortColumn { get; set; } = "Title";

    public string SortOrder { get; set; } = "asc";

    public string? SelectedAuthorId { get; set; }

    public string? SelectedGenreId { get; set; }

    public bool? FilterFeatured { get; set; }

    public bool? FilterArchived { get; set; }

    public List<SelectListItem> AuthorList { get; set; } = new();

    public List<SelectListItem> GenreList { get; set; } = new();
}

public class BookInsertVM
{
    [DisplayName("Cover Image")]
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
    [MinLength(1, ErrorMessage = "At least one Genre is required.")]
    public List<string> SelectedGenreIds { get; set; } = new();

    public List<SelectListItem> GenreList { get; set; } = new();

    [DisplayName("Author(s)")]
    [MinLength(1, ErrorMessage = "At least one Author is required.")]
    [MaxLength(10, ErrorMessage = "A book cannot have more than 10 authors.")]
    public List<string> SelectedAuthorIds { get; set; } = new();

    public List<SelectListItem> AuthorList { get; set; } = new();

    [DisplayName("Featured")]
    public bool IsFeatured { get; set; } = false;

    [DisplayName("Archived")]
    public bool IsArchived { get; set; } = false;
}

public class BookEditVM
{
    [DisplayName("Book ID")]
    public string Id { get; set; }

    [DisplayName("Cover Image")]
    public IFormFile? Image { get; set; }

    public string? ExistingImageURL { get; set; }

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
    [MinLength(1, ErrorMessage = "At least one Genre is required.")]
    public List<string> SelectedGenreIds { get; set; } = new();

    public List<SelectListItem> GenreList { get; set; } = new();

    [DisplayName("Author(s)")]
    [MinLength(1, ErrorMessage = "At least one Author is required.")]
    [MaxLength(10, ErrorMessage = "A book cannot have more than 10 authors.")]
    public List<string> SelectedAuthorIds { get; set; } = new();

    public List<SelectListItem> AuthorList { get; set; } = new();

    [DisplayName("Featured")]
    public bool IsFeatured { get; set; } = false;

    [DisplayName("Archived")]
    public bool IsArchived { get; set; } = false;
}

public class BookDetailVM
{
    public Book Book { get; set; }
    public List<Copy> Copies { get; set; } = new();

    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? SearchTerm { get; set; }
    public string SortColumn { get; set; } = "Id";
    public string SortOrder { get; set; } = "asc";
}

public class GenreManagementVM
{
    public List<Genre> ExistingGenres { get; set; } = new();

    public GenreVM Form { get; set; } = new();

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public string? SearchTerm { get; set; }

    public string SortColumn { get; set; } = "Name";

    public string SortOrder { get; set; } = "asc";
}

public class GenreVM
{
    public string? Id { get; set; }

    [DisplayName("Genre Name")]
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "{0} must be between 3 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z]+(?:[\s-&][a-zA-Z]+)*$",
    ErrorMessage = "{0} must start and end with a letter, and symbols (- and &) must be between letters.")]
    public string Name { get; set; }
}

public class AuthorManagementVM
{
    public List<Author> ExistingAuthors { get; set; } = new();

    public AuthorVM Form { get; set; } = new();

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public string? SearchTerm { get; set; }

    public string SortColumn { get; set; } = "Name";
    
    public string SortOrder { get; set; } = "asc";
}


public class AuthorVM
{
    public string? Id { get; set; }

    [DisplayName("Author Name")]
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "{0} must be between 1 and 100 characters.")]
    [RegularExpression(@"^[a-zA-Z]+(?:(?:[\s\-\']|\. ?)[a-zA-Z]+)*\.?$",
    ErrorMessage = "{0} must start with a letter, and symbols (', -, .) must be between letters.")]
    public string Name { get; set; }
}



/* ------------------------------ JASON TYE HONG YI ------------------------------ */

public class ReservationVM
{
    [Display(Name = "Reservation ID")]
    public string? Id { get; set; }

    [DisplayName("User")]
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(6, ErrorMessage = "{0} must be 6 characters.")]
    public string UserId { get; set; }

    [DisplayName("Book")]
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(6, ErrorMessage = "{0} must be 6 characters.")]
    public string BookId { get; set; }

    [DisplayName("Copy")]
    [StringLength(6, ErrorMessage = "{0} must be 6 characters.")]
    public string? CopyId { get; set; }

    [DisplayName("Reservation Date")]
    public DateTime ReservationDate { get; set; } = DateTime.Now;

    [DisplayName("Reservation Due")]
    public DateTime? ReservationDue { get; set; }

    [DisplayName("Reservation Status")]
    [Required(ErrorMessage = "{0} is required.")]
    public Reservation.ReservationStatus Status { get; set; } = Reservation.ReservationStatus.PENDING;

    [DisplayName("User")]
    public List<SelectListItem> UserList { get; set; } = new();

    [DisplayName("Book")]
    public List<SelectListItem> BookList { get; set; } = new();
}



/* ------------------------------ BRIAN OOI SHU YI ------------------------------ */

public class RoomCategoryCreateVM
{
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, ErrorMessage = "{0} must be between 1 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9 ]+$", ErrorMessage = "{0} can only contain alphabets and numbers.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(500, ErrorMessage = "{0} cannot exceed 500 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9-,._' ]+$", ErrorMessage = "{0} can only contain alphabets, punctuations and numbers.")]
    public string Description { get; set; }

    [DisplayName("Hourly Rate")]
    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, 100, ErrorMessage = "{0} must be between RM 1 and RM 100.")]
    public decimal HourlyRate { get; set; }

    [DisplayName("Room Image")]
    public IFormFile? Image { get; set; }
}

public class RoomCreateVM
{
    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, ErrorMessage = "{0} cannot exceed 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9 ]+$", ErrorMessage = "{0} can only contain alphabets and numbers.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(100, ErrorMessage = "{0} cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9,. ]+$", ErrorMessage = "{0} can only contain alphabets, punctuations and numbers.")]
    public string Location { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, 30, ErrorMessage = "Capacity must be between 1 and 30.")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Please select a status.")]
    public RoomStatus Status { get; set; } = RoomStatus.AVAILABLE;

    [DisplayName("Category")]
    [Required(ErrorMessage = "Please select a category.")]
    public string RoomCategoryId { get; set; }
}

public class RoomCategoryUpdateVM
{
    [Required]
    public string Id { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, ErrorMessage = "{0} must be between 1 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9 ]+$", ErrorMessage = "{0} can only contain alphabets and numbers.")]
    public string Name { get; set; }

    [DisplayName("Hourly Rate")]
    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, 100, ErrorMessage = "{0} must be between RM 1 and RM 100.")]
    public decimal HourlyRate { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(500, ErrorMessage = "{0} cannot exceed 500 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9-,._' ]+$", ErrorMessage = "{0} can only contain alphabets, punctuations and numbers.")]
    public string Description { get; set; }

    [DisplayName("Category Image")]
    public IFormFile? Image { get; set; }

    public string? ExistingImageURL { get; set; }
}

public class RoomUpdateVM
{
    [Required]
    public string RoomId { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(50, ErrorMessage = "{0} cannot exceed 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9 ]+$", ErrorMessage = "{0} can only contain alphabets and numbers.")]
    public string Name { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [StringLength(100, ErrorMessage = "{0} cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9,. ]+$", ErrorMessage = "{0} can only contain alphabets, punctuations and numbers.")]
    public string Location { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [Range(1, 30, ErrorMessage = "Capacity must be between 1 and 30.")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Please select a status.")]
    public RoomStatus Status { get; set; }
}

public class RoomIndexVM
{
    public List<RoomCategory> Categories { get; set; } = new();

    public RoomCategoryCreateVM CategoryInput { get; set; } = new();

    public RoomCreateVM RoomInput { get; set; } = new();
}

public class RoomUpdatePageVM
{
    public RoomCategory CategoryDisplay { get; set; } = new();

    public RoomCategoryUpdateVM CategoryUpdate { get; set; } = new();

    public RoomUpdateVM RoomUpdate { get; set; } = new();
}


/* -------------------- Booking ------------------ */
public class BookingCreateVM : IValidatableObject
{
    [DisplayName("Room Category")]
    [Required(ErrorMessage = "System Error: Room Category is missing.")]
    public string RoomCategoryId { get; set; }

    [DisplayName("Date")]
    [Required(ErrorMessage = "Please select a date.")]
    [DataType(DataType.Date)]
    public DateTime BookingDate { get; set; } = DateTime.Today;

    [DisplayName("Start Time")]
    [Required(ErrorMessage = "Please select a start time.")]
    public TimeSpan StartTime { get; set; }

    [DisplayName("End Time")]
    [Required(ErrorMessage = "Please select an end time.")]
    public TimeSpan EndTime { get; set; }

    public List<SelectListItem> CategoryList { get; set; } = new();

    public int OpenHour { get; set; } = 9;
    public int CloseHour { get; set; } = 21;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BookingDate.Date < DateTime.Today)
            yield return new ValidationResult("Cannot book dates in the past.", new[] { nameof(BookingDate) });

        if (BookingDate.Date == DateTime.Today)
        {
            if (StartTime < DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(-1)))
                yield return new ValidationResult("Start time has already passed.", new[] { nameof(StartTime) });
        }

        if (EndTime <= StartTime)
            yield return new ValidationResult("End time must be later than start time.", new[] { nameof(EndTime) });

        TimeSpan duration = EndTime - StartTime;
        if (duration.TotalHours > 2)
            yield return new ValidationResult("Maximum booking duration is 2 hours.", new[] { nameof(EndTime) });
        if (duration.TotalMinutes < 30)
            yield return new ValidationResult("Minimum booking duration is 30 minutes.", new[] { nameof(EndTime) });

        if (StartTime.Hours < OpenHour || StartTime.Hours >= CloseHour)
            yield return new ValidationResult($"Operating hours: {OpenHour}:00 - {CloseHour}:00", new[] { nameof(StartTime) });

        if (EndTime.TotalHours > CloseHour)
            yield return new ValidationResult($"Library closes at {CloseHour}:00.", new[] { nameof(EndTime) });
    }
}