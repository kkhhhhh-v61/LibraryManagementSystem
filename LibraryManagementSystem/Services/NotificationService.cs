namespace LibraryManagementSystem.Services;

public interface INotificationService
{
    void Notify(string userId, string message, string? link = null);
    List<Notification> GetUserNotifications(string userId);
    int GetUnreadCount(string userId);
    void MarkAsRead(int notificationId);
    void MarkAllAsRead(string userId);
    void Delete(int notificationId);
    void DeleteAll(string userId);
}

public class NotificationService(LMSDB db) : INotificationService
{
    public void Notify(string userId, string message, string? link = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            Link = link,
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        db.Notifications.Add(notification);
        db.SaveChanges();
    }

    public List<Notification> GetUserNotifications(string userId)
    {
        return db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20) // Limit to last 20 notifications
            .ToList();
    }

    public int GetUnreadCount(string userId)
    {
        return db.Notifications.Count(n => n.UserId == userId && !n.IsRead);
    }

    public void MarkAsRead(int notificationId)
    {
        var note = db.Notifications.Find(notificationId);
        if (note != null && !note.IsRead)
        {
            note.IsRead = true;
            db.SaveChanges();
        }
    }

    public void MarkAllAsRead(string userId)
    {
        var notes = db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
        if (notes.Any())
        {
            foreach (var note in notes)
            {
                note.IsRead = true;
            }
            db.SaveChanges();
        }
    }

    public void Delete(int notificationId)
    {
        var note = db.Notifications.Find(notificationId);
        if (note != null)
        {
            db.Notifications.Remove(note);
            db.SaveChanges();
        }
    }

    public void DeleteAll(string userId)
    {
        var notes = db.Notifications.Where(n => n.UserId == userId).ToList();
        if (notes.Any())
        {
            db.Notifications.RemoveRange(notes);
            db.SaveChanges();
        }
    }
}