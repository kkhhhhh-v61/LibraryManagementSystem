using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class HomeController(LMSDB db) : Controller
{
    public IActionResult Index()
    {
        // Fetch Featured Books
        var featuredBooks = db.Books
            .Where(b => b.IsFeatured && !b.IsArchived)
            .Include(b => b.Authors)
            .OrderBy(b => b.Title)
            .Take(10) // Limit to 10
            .ToList();

        // Fetch New Arrivals
        var newArrivals = db.Books
            .Where(b => !b.IsArchived)
            .OrderByDescending(b => b.DateAdded)
            .Take(10) // Limit to 10
            .Include(b => b.Authors)
            .ToList();

        // Fetch Top Rated Books
        var topRatedStats = db.Reviews
            .Include(r => r.Loan).ThenInclude(l => l.Copy)
            .GroupBy(r => r.Loan.Copy.BookId)
            .Select(g => new { BookId = g.Key, Average = g.Average(r => r.Rating), Count = g.Count() })
            .OrderByDescending(x => x.Average)
            .ThenByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var topRatedBookIds = topRatedStats.Select(x => x.BookId).ToList();

        var topRatedBooks = db.Books
            .Where(b => topRatedBookIds.Contains(b.Id) && !b.IsArchived)
            .Include(b => b.Authors)
            .ToList()
            .OrderBy(b => topRatedBookIds.IndexOf(b.Id))
            .ToList();

        // Fetch Recommended Books (Logged-in only)
        var recommendedBooks = new List<Book>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user != null)
            {
                // 1. Find user's top 3 genres based on loan history
                var topGenreIds = db.Loans
                    .Where(l => l.UserId == user.Id)
                    .SelectMany(l => l.Copy.Book.Genres.Select(g => g.Id))
                    .GroupBy(id => id)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .Take(3)
                    .ToList();

                if (topGenreIds.Any())
                {
                    // 2. Get IDs of books user has already borrowed (to exclude them)
                    var borrowedBookIds = db.Loans
                        .Where(l => l.UserId == user.Id)
                        .Select(l => l.Copy.BookId)
                        .Distinct()
                        .ToList();

                    // 3. Fetch random books from these genres that the user hasn't read
                    recommendedBooks = db.Books
                        .Include(b => b.Authors)
                        .Where(b => !b.IsArchived && 
                                    !borrowedBookIds.Contains(b.Id) && 
                                    b.Genres.Any(g => topGenreIds.Contains(g.Id)))
                        .OrderBy(x => Guid.NewGuid()) // Randomize selection
                        .Take(10)
                        .ToList();
                }
            }
        }

        // Get ratings for all these books
        var bookIds = featuredBooks.Select(b => b.Id)
            .Union(newArrivals.Select(b => b.Id))
            .Union(topRatedBooks.Select(b => b.Id))
            .Union(recommendedBooks.Select(b => b.Id))
            .Distinct()
            .ToList();

        var ratingsData = db.Reviews
            .Include(r => r.Loan)
            .ThenInclude(l => l.Copy)
            .Where(r => bookIds.Contains(r.Loan.Copy.BookId))
            .GroupBy(r => r.Loan.Copy.BookId)
            .Select(g => new { BookId = g.Key, Average = g.Average(r => r.Rating) })
            .ToDictionary(x => x.BookId, x => x.Average);

        ViewBag.FeaturedBooks = featuredBooks;
        ViewBag.NewArrivals = newArrivals;
        ViewBag.TopRatedBooks = topRatedBooks;
        ViewBag.RecommendedBooks = recommendedBooks;
        ViewBag.BookRatings = ratingsData;

        return View();
    }
}
