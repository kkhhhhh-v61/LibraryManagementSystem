using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryManagementSystem.Controllers;

#nullable disable warnings

[Authorize]
public class WishlistController(IWishlistService wishlistService, LMSDB db) : Controller
{
    [HttpGet]
    public IActionResult GetWishlist()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var items = wishlistService.GetUserWishlist(userId);
        return PartialView("_WishlistList", items);
    }

    [HttpPost]
    public IActionResult Toggle(string bookId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        wishlistService.Toggle(userId, bookId);
        bool isInWishlist = wishlistService.IsInWishlist(userId, bookId);
        
        return Json(new { success = true, isInWishlist });
    }
    
    [HttpGet]
    public IActionResult CheckStatus(string bookId)
    {
        var userId = GetUserId();
        if (userId == null) return Json(new { isInWishlist = false });
        
        return Json(new { isInWishlist = wishlistService.IsInWishlist(userId, bookId) });
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
               db.Users.FirstOrDefault(u => u.Username == User.Identity.Name)?.Id;
    }
}