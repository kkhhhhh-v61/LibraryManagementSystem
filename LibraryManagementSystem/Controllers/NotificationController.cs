using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibraryManagementSystem.Controllers;

#nullable disable warnings

[Authorize]
public class NotificationController(INotificationService notificationService, LMSDB db) : Controller
{
    [HttpGet]
    public IActionResult GetNotifications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                     db.Users.FirstOrDefault(u => u.Username == User.Identity.Name)?.Id;

        if (userId == null) return Unauthorized();

        var notifications = notificationService.GetUserNotifications(userId);
        var unreadCount = notificationService.GetUnreadCount(userId);

        // Return a JSON object containing the HTML partial and the count
        // We render the partial view to a string or just return the partial directly if using AJAX load
        // For simplicity in this setup, we will return the PartialView and handle count separately or embed it.
        
        ViewBag.UnreadCount = unreadCount;
        return PartialView("_NotificationList", notifications);
    }

    [HttpGet]
    public IActionResult GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                     db.Users.FirstOrDefault(u => u.Username == User.Identity.Name)?.Id;
        return Json(new { count = userId != null ? notificationService.GetUnreadCount(userId) : 0 });
    }

    [HttpPost]
    public IActionResult MarkAsRead(int id)
    {
        notificationService.MarkAsRead(id);
        return Ok();
    }

    [HttpPost]
    public IActionResult MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                     db.Users.FirstOrDefault(u => u.Username == User.Identity.Name)?.Id;
        
        if (userId != null)
        {
            notificationService.MarkAllAsRead(userId);
        }
        return Ok();
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        notificationService.Delete(id);
        return Ok();
    }

    [HttpPost]
    public IActionResult ClearAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                     db.Users.FirstOrDefault(u => u.Username == User.Identity.Name)?.Id;
        
        if (userId != null)
        {
            notificationService.DeleteAll(userId);
        }
        return Ok();
    }

    [HttpPost]
    public IActionResult TestCreate(string? userId, string message, string? link)
    {
        if (string.IsNullOrEmpty(userId) || userId == "current")
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                     db.Users.FirstOrDefault(u => u.Username == User.Identity.Name)?.Id;
        }

        if (userId != null && !string.IsNullOrWhiteSpace(message))
        {
            notificationService.Notify(userId, message, link);
            TempData["ToastMessage"] = "Test notification sent.";
            TempData["ToastType"] = "success";
        }
        
        string referer = Request.Headers["Referer"].ToString();
        return Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
    }
}