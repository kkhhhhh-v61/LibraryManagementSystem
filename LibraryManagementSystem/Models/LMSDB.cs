using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Models;

#nullable disable warnings

public class LMSDB(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Copy> Copies { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomCategory> RoomCategories { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Fine> Fines { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

}

/* ------------------------------ LAI CHEE KIT ------------------------------ */

public class User
{
    public enum UserRole
    {
        ADMIN,
        STAFF,
        MEMBER
    }

    public enum UserStatus
    {
        ACTIVE,     // Normal user
        INACTIVE,   // Deleted/Banned by Admin
        LOCKED      // Temporary lock due to too many failed login attempts
    }

    [Key]
    [MaxLength(6)]
    public string Id { get; set; } // PK: e.g., "U00001"

    [MaxLength(50)]
    public string Username { get; set; }

    [MaxLength(255)]
    public string PasswordHash { get; set; }

    [MaxLength(50)]
    public string FirstName { get; set; }

    [MaxLength(50)]
    public string LastName { get; set; }

    [MaxLength(255)]
    public string Email { get; set; }

    [MaxLength(15)]
    public string ContactNo { get; set; }

    public UserRole Role { get; set; }

    [MaxLength(255)]
    public string? ImageURL { get; set; } 

    public UserStatus Status { get; set; }

    // Feature: Credibility System
    // Logic: Starts at 100. -1 for late return, -10 for booking no-show.
    public int CredibilityScore { get; set; } = 100;

    // Feature: Security (Login Blocking)
    // Logic: Increment this on wrong password. If > 3, set Status to LOCKED or use LockoutEnd.
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; } // The time when they can try logging in again.

    // Feature: User Profile Sharing
    // Logic: If false, other members cannot view their profile details.
    public bool IsProfilePublic { get; set; } = true;

    public List<Loan> Loans { get; set; } = [];          
    public List<Booking> Bookings { get; set; } = [];    
    public List<Wishlist> Wishlists { get; set; } = [];  
    public List<Fine> Fines { get; set; } = [];         
    public List<PasswordResetToken> PasswordResetTokens { get; set; } = []; 
}

// Feature: Password Recovery (Email with token link).
public class PasswordResetToken
{
    [Key]
    public int Id { get; set; }

    [MaxLength(6)]
    public string UserId { get; set; }

    [MaxLength(255)]
    public string Token { get; set; } // The random secure string sent to email

    public DateTime ExpiryDate { get; set; } 

    public bool IsUsed { get; set; } = false; // Prevents re-using the same link

    public User User { get; set; }
}




/* ------------------------------ TAN KAI HAO ------------------------------ */

public class Book
{
    [Key]
    [MaxLength(6)]
    public string Id { get; set; }

    [MaxLength(255)]
    public string Title { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; }

    [MaxLength(13)]
    public string ISBN { get; set; }

    public int PublicationYear { get; set; }

    public int Edition { get; set; }

    [MaxLength(100)]
    public string? ImageURL { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsArchived { get; set; }

    public DateOnly DateAdded { get; set; }

    public List<Genre> Genres { get; set; } = [];

    public List<Author> Authors { get; set; } = [];
}

public class Genre
{
    [Key]
    [MaxLength(6)]
    public string Id { get; set; }
    
    [MaxLength(50)]
    public string Name { get; set; }

    public List<Book> Books { get; set; } = [];
}

public class Author
{
    [Key]
    [MaxLength(6)]
    public string Id { get; set; }
    
    [MaxLength(100)]
    public string Name { get; set; }

    public List<Book> Books { get; set; } = [];
}

public class Copy
{
    public enum CopyStatus
    {
        AVAILABLE,
        RESERVED,
        BORROWED
    }

    [Key]
    [MaxLength(6)]
    public string Id { get; set; }
    
    [MaxLength(20)]
    public string Barcode { get; set; }
    
    public CopyStatus Status { get; set; }
    
    [MaxLength(6)]
    public string BookId { get; set; }

    public Book Book { get; set; }
}

public class Review
{
    [Key]
    [MaxLength(6)]
    public string Id { get; set; }
    
    public int Rating { get; set; }
    
    [MaxLength(2000)]
    public string? Comment { get; set; }
    
    public DateTime ReviewDate { get; set; }
    
    [MaxLength(6)]
    public string LoanId { get; set; }

    public Loan Loan { get; set; }
}

public class Loan
{
    public enum LoanStatus
    {
        RESERVED,   // Set when User borrows a book (either directly or by reservation)
        EXPIRED,    // Set when User doesn't pickup the book before due date
        IN_LOAN,    // Set after User picks up the book in person
        RETURNED,   // Set after User returns the book
        OVERDUE     // Set when User doesn't return the book by due date
    }

    [Key]
    [MaxLength(6)]
    public string Id { get; set; }
    
    [MaxLength(6)]
    public string UserId { get; set; }
    
    [MaxLength(6)]
    public string CopyId { get; set; }
    
    public DateTime LoanDate { get; set; }
    
    public DateTime PickupDue { get; set; }
    
    public DateTime? PickupDate { get; set; }
    
    public DateTime LoanDue { get; set; }
    
    public DateTime? ReturnDate { get; set; }
    
    public LoanStatus Status { get; set; }

    public Copy Copy { get; set; }
    
    public User User { get; set; }
    
    public Review? Review { get; set; }
}

[PrimaryKey("UserId", "BookId")]    // Composite key
public class Wishlist
{
    [MaxLength(6)]
    public string UserId { get; set; }  // PK

    [MaxLength(6)]
    public string BookId { get; set; }  // PK

    public DateTime DateAdded { get; set; }

    public User User { get; set; }

    public Book Book { get; set; }
}



/* ------------------------------ JASON TYE HONG YI ------------------------------ */

public class Reservation
{
    public enum ReservationStatus
    {
        PENDING,    // Set when User's reservation is still waiting in line
        READY,      // Set when a copy of the book is available and reserved for the User
        CANCELLED,  // Set when User manually cancels the reservation after a copy is available
        EXPIRED     // Set when User doesn't perform any action (Borrow or Cancel) in time
    }

    [Key]
    [MaxLength(6)]
    public string Id { get; set; }

    [MaxLength(6)]
    public string UserId { get; set; }

    [MaxLength(6)]
    public string BookId { get; set; }

    [MaxLength(6)]
    public string? CopyId { get; set; }

    public DateTime ReservationDate { get; set; }

    public DateTime? ReservationDue { get; set; }

    public ReservationStatus Status { get; set; }
}

public class Payment
{
    public enum PaymentStatus
    {
        SUCCESS,
        PENDING,
        CANCELLED
    }

    public enum PaymentType
    {
        FINE,
        BOOKING
    }

    public enum PaymentMethod
    {
        E_WALLET,
        CREDIT_CARD,
        BANKING
    }

    [Key]
    [MaxLength(6)]
    public string Id { get; set; }

    public PaymentType Type { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime Date { get; set; }

    [MaxLength(6)]
    public string? FineId { get; set; }

    [MaxLength(6)]
    public string? BookingId { get; set; }

    public Fine? Fine { get; set; }

    public Booking? Booking { get; set; }
}

public class Fine
{
    [Key]
    [MaxLength(6)]
    public string Id { get; set; }

    [Precision(8,2)]
    public decimal Amount { get; set; }
    
    public DateTime IssuedDate { get; set; }
    
    public DateTime NotificationDate { get; set; }
    
    public DateTime DueDate { get; set; }
    
    [Precision(8,2)]
    public decimal OverdueRate { get; set; }
    
    public bool IsPaid { get; set; }
    
    [MaxLength(6)]
    public string LoanId { get; set; }

    public Loan Loan { get; set; }
}




/* ------------------------------ BRIAN OOI SHU YI ------------------------------ */
public class RoomCategory
{
    [Key]
    [MaxLength(3)]
    public string Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    [Precision(5, 2)]
    public decimal HourlyRate { get; set; }

    [MaxLength(100)]
    public string? ImageURL { get; set; }

    public List<Room> Rooms { get; set; } = [];
}

public class Room
{
    public enum RoomStatus
    {
        AVAILABLE,      // The room that user can select
        MAINTENANCE,    // The room are in maintenance
        UNAVAILABLE     // The room that user cant select
    }

    [Key]
    [MaxLength(3)]
    public string Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Location { get; set; }

    public int Capacity { get; set; }

    public RoomStatus Status { get; set; }

    [MaxLength(3)]
    public string RoomCategoryId { get; set; }

    public RoomCategory RoomCategory { get; set; }

    public List<Booking> Bookings { get; set; } = [];
}

public class Booking
{
    public enum BookingStatus
    {
        PENDING,        // The user still making payment
        CONFIRMED,      // The user done making payment
        COMPLETED,      // The status confirmed change to completed after the booking time and no be cancel
        CANCELLED       // The user cancel  the booking after make payment(making refund)
    }

    [Key]
    [MaxLength(6)]
    public string Id { get; set; }

    [MaxLength(3)]
    public string RoomId { get; set; }

    public Room Room { get; set; }

    [MaxLength(6)]
    public string UserId { get; set; }

    public User User { get; set; }

    public DateTime BookingDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [Precision(8, 2)]
    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; }
        
    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(250)]
    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public bool IsReminderSent { get; set; } = false;
}
