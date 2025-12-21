using System.Security.Claims;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Extensions;

namespace LibraryManagementSystem.Controllers;

[Authorize]
public class FineController(LMSDB db, IFineService fineService, INotificationService ns) : Controller
{
    [Authorize(Roles = "MEMBER")]
    public IActionResult Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var fines = fineService.GetUserFines(userId);
        return View(fines);
    }

    [Authorize(Roles = "MEMBER")]
    public IActionResult Records(string? search, string? status, DateOnly? dateFrom, DateOnly? dateTo, int page = 1, string sort = "IssuedDate", string order = "desc")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var query = db.Fines
            .Include(f => f.Loan)
            .ThenInclude(l => l.Copy)
            .ThenInclude(c => c.Book)
            .Where(f => f.Loan.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(f => f.Loan.Copy.Book.Title.Contains(search) || f.Id.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            bool isPaid = status == "Paid";
            query = query.Where(f => f.IsPaid == isPaid);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(f => DateOnly.FromDateTime(f.IssuedDate) >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(f => DateOnly.FromDateTime(f.IssuedDate) <= dateTo.Value);
        }

        bool isDesc = order == "desc";
        query = sort switch
        {
            "Book" => isDesc ? query.OrderByDescending(f => f.Loan.Copy.Book.Title) : query.OrderBy(f => f.Loan.Copy.Book.Title),
            "Amount" => isDesc ? query.OrderByDescending(f => f.Amount) : query.OrderBy(f => f.Amount),
            "IssuedDate" => isDesc ? query.OrderByDescending(f => f.IssuedDate) : query.OrderBy(f => f.IssuedDate),
            "Status" => isDesc ? query.OrderByDescending(f => f.IsPaid) : query.OrderBy(f => f.IsPaid),
            _ => isDesc ? query.OrderByDescending(f => f.IssuedDate) : query.OrderBy(f => f.IssuedDate)
        };

        int pageSize = 10;
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        if (totalItems <= 1)
        {
            dateFrom ??= DateOnly.FromDateTime(DateTime.Today);
            dateTo ??= DateOnly.FromDateTime(DateTime.Today);
        }

        ViewBag.TotalItems = totalItems;
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var allDates = db.Fines.Where(f => f.Loan.UserId == userId).Select(f => f.IssuedDate);
        ViewBag.MinDate = allDates.Any() ? allDates.Min().ToString("yyyy-MM-dd") : null;
        ViewBag.MaxDate = allDates.Any() ? allDates.Max().ToString("yyyy-MM-dd") : null;
        ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
        ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new FineManagementVM
        {
            Fines = items,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchTerm = search,
            SortColumn = sort,
            SortOrder = order,
            FilterStatus = status,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        if (Request.IsAjax())
        {
            return PartialView("_TableFineRecords", vm);
        }

        return View("Records", vm);
    }

    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult ManageFine(string? search, string? status, DateOnly? dateFrom, DateOnly? dateTo, int page = 1, string sort = "IssuedDate", string order = "desc")
    {
        var query = db.Fines
            .Include(f => f.Loan)
            .ThenInclude(l => l.User)
            .Include(f => f.Loan)
            .ThenInclude(l => l.Copy)
            .ThenInclude(c => c.Book)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(f => f.Loan.User.Username.Contains(search) || f.Loan.Copy.Book.Title.Contains(search) || f.Id.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            bool isPaid = status == "Paid";
            query = query.Where(f => f.IsPaid == isPaid);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(f => DateOnly.FromDateTime(f.IssuedDate) >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(f => DateOnly.FromDateTime(f.IssuedDate) <= dateTo.Value);
        }

        bool isDesc = order == "desc";
        query = sort switch
        {
            "Id" => isDesc ? query.OrderByDescending(f => f.Id) : query.OrderBy(f => f.Id),
            "User" => isDesc ? query.OrderByDescending(f => f.Loan.User.Username) : query.OrderBy(f => f.Loan.User.Username),
            "Amount" => isDesc ? query.OrderByDescending(f => f.Amount) : query.OrderBy(f => f.Amount),
            "IssuedDate" => isDesc ? query.OrderByDescending(f => f.IssuedDate) : query.OrderBy(f => f.IssuedDate),
            "Status" => isDesc ? query.OrderByDescending(f => f.IsPaid) : query.OrderBy(f => f.IsPaid),
            _ => isDesc ? query.OrderByDescending(f => f.IssuedDate) : query.OrderBy(f => f.IssuedDate)
        };

        int pageSize = 10;
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        if (totalItems <= 1)
        {
            dateFrom ??= DateOnly.FromDateTime(DateTime.Today);
            dateTo ??= DateOnly.FromDateTime(DateTime.Today);
        }

        ViewBag.TotalItems = totalItems;
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var allDates = db.Fines.Select(f => f.IssuedDate);
        ViewBag.MinDate = allDates.Any() ? allDates.Min().ToString("yyyy-MM-dd") : null;
        ViewBag.MaxDate = allDates.Any() ? allDates.Max().ToString("yyyy-MM-dd") : null;
        ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
        ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new FineManagementVM
        {
            Fines = items,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchTerm = search,
            SortColumn = sort,
            SortOrder = order,
            FilterStatus = status,
            DateFrom = dateFrom,
            DateTo = dateTo,
            CanDelete = items.ToDictionary(f => f.Id, f => true)
        };

        if (Request.IsAjax())
        {
            return PartialView("_TableFine", vm);
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult WaiveFine(string id)
    {
        var fine = db.Fines.Include(f => f.Loan).FirstOrDefault(f => f.Id == id);
        if (fine == null)
        {
            TempData["ToastMessage"] = "Fine not found.";
            TempData["ToastType"] = "error";
            return RedirectToAction("ManageFine");
        }

        if (fine.IsPaid)
        {
            TempData["ToastMessage"] = "Fine is already paid.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("ManageFine");
        }

        fine.IsPaid = true;
        db.SaveChanges();

        // Notify user
        ns.Notify(fine.Loan.UserId, $"Your fine (ID: {fine.Id}) of RM {fine.Amount:F2} has been waived by an administrator.", "/Fine/Records");

        TempData["ToastMessage"] = "Fine has been waived successfully and the user has been notified.";
        TempData["ToastType"] = "success";
        return RedirectToAction("ManageFine");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult DeleteFine(string id)
    {
        var fine = db.Fines.Find(id);
        if (fine == null)
        {
            TempData["ToastMessage"] = "Fine not found.";
            TempData["ToastType"] = "error";
        }
        else
        {
            db.Fines.Remove(fine);
            db.SaveChanges();
            TempData["ToastMessage"] = $"Fine '{fine.Id}' deleted successfully.";
            TempData["ToastType"] = "success";
        }
        return RedirectToAction("ManageFine");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult BatchDeleteFine(List<string> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            TempData["ToastMessage"] = "No items selected.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("ManageFine");
        }

        var fines = db.Fines.Where(f => ids.Contains(f.Id)).ToList();
        db.Fines.RemoveRange(fines);
        db.SaveChanges();

        TempData["ToastMessage"] = $"Successfully deleted {fines.Count} fine(s).";
        TempData["ToastType"] = "success";

        return RedirectToAction("ManageFine");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult BatchWaiveFine(List<string> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            TempData["ToastMessage"] = "No items selected.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("ManageFine");
        }

        var fines = db.Fines.Include(f => f.Loan).Where(f => ids.Contains(f.Id) && !f.IsPaid).ToList();

        if (fines.Count == 0)
        {
            TempData["ToastMessage"] = "No unpaid fines found in selection.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("ManageFine");
        }

        foreach (var fine in fines)
        {
            fine.IsPaid = true;
            ns.Notify(fine.Loan.UserId, $"Your fine (ID: {fine.Id}) of RM {fine.Amount:F2} has been waived by an administrator.", "/Fine/Records");
        }

        db.SaveChanges();

        TempData["ToastMessage"] = $"Successfully waived {fines.Count} fine(s).";
        TempData["ToastType"] = "success";

        return RedirectToAction("ManageFine");
    }
}