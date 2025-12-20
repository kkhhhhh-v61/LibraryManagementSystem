using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services;

public interface IWishlistService
{
    void Toggle(string userId, string bookId);
    List<Wishlist> GetUserWishlist(string userId);
    bool IsInWishlist(string userId, string bookId);
}

public class WishlistService(LMSDB db) : IWishlistService
{
    public void Toggle(string userId, string bookId)
    {
        var existing = db.Wishlists.Find(userId, bookId);
        if (existing != null)
        {
            db.Wishlists.Remove(existing);
        }
        else
        {
            db.Wishlists.Add(new Wishlist
            {
                UserId = userId,
                BookId = bookId,
                DateAdded = DateTime.Now
            });
        }
        db.SaveChanges();
    }

    public List<Wishlist> GetUserWishlist(string userId)
    {
        return db.Wishlists
            .Include(w => w.Book)
            .ThenInclude(b => b.Authors)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.DateAdded)
            .ToList();
    }

    public bool IsInWishlist(string userId, string bookId)
    {
        return db.Wishlists.Any(w => w.UserId == userId && w.BookId == bookId);
    }
}