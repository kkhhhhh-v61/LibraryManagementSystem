using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Extensions;
using LibraryManagementSystem.Services;

namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = "MEMBER")]
public class ReservationController(LMSDB db, IReservationService rs) : Controller
{
    public IActionResult Index(string? search, string? status, DateOnly? dateFrom, DateOnly? dateTo, int page = 1, string sort = "ReservationDate", string order = "desc")
    {
        var username = User.Identity?.Name;
        var user = db.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) return RedirectToAction("Login", "Account");

        var query = db.Reservations
            .Include(r => r.Book)
            .ThenInclude(b => b.Authors)
            .Where(r => r.UserId == user.Id)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(r => r.Book.Title.Contains(search));
        }

        // Filter Status
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Reservation.ReservationStatus>(status, true, out var statusEnum))
        {
            query = query.Where(r => r.Status == statusEnum);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(r => DateOnly.FromDateTime(r.ReservationDate) >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(r => DateOnly.FromDateTime(r.ReservationDate) <= dateTo.Value);
        }

        // Sorting
        bool isDesc = order == "desc";
        query = sort switch
        {
            "Book" => isDesc ? query.OrderByDescending(r => r.Book.Title) : query.OrderBy(r => r.Book.Title),
            "ReservationDate" => isDesc ? query.OrderByDescending(r => r.ReservationDate) : query.OrderBy(r => r.ReservationDate),
            "Status" => isDesc ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            _ => isDesc ? query.OrderByDescending(r => r.ReservationDate) : query.OrderBy(r => r.ReservationDate)
        };

        // Pagination
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

        var allDates = db.Reservations.Where(r => r.UserId == user.Id).Select(r => r.ReservationDate);
        ViewBag.MinDate = allDates.Any() ? allDates.Min().ToString("yyyy-MM-dd") : null;
        ViewBag.MaxDate = allDates.Any() ? allDates.Max().ToString("yyyy-MM-dd") : null;

        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // Map to VM and Calculate Queue Position
        var vms = items.Select(item => {
            int? position = null;
            if (item.Status == Reservation.ReservationStatus.PENDING)
            {
                position = db.Reservations.Count(r => r.BookId == item.BookId && 
                                                      r.Status == Reservation.ReservationStatus.PENDING && 
                                                      r.ReservationDate < item.ReservationDate) + 1;
            }
            return new ReservationVM
            {
                Id = item.Id,
                UserId = item.UserId,
                BookId = item.BookId,
                CopyId = item.CopyId,
                ReservationDate = item.ReservationDate,
                ReservationDue = item.ReservationDue,
                Status = item.Status,
                QueuePosition = position,
                BookTitle = item.Book.Title,
                BookImageURL = item.Book.ImageURL,
                BookAuthors = string.Join(", ", item.Book.Authors.Select(a => a.Name))
            };
        }).ToList();

        var model = new ReservationListVM
        {
            Reservations = vms,
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
            return PartialView("_TableReservation", model);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(string bookId)
    {
        var username = User.Identity?.Name;
        var user = db.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) return RedirectToAction("Login", "Account");

        // Check for existing active reservation or loan
        bool hasActive = db.Reservations.Any(r => r.UserId == user.Id && r.BookId == bookId && 
                                            (r.Status == Reservation.ReservationStatus.PENDING || r.Status == Reservation.ReservationStatus.READY)) ||
                         db.Loans.Any(l => l.UserId == user.Id && l.Copy.BookId == bookId && 
                                     (l.Status == Loan.LoanStatus.RESERVED || l.Status == Loan.LoanStatus.IN_LOAN));

        if (hasActive)
        {
            TempData["ToastMessage"] = "You already have an active reservation or loan for this book.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("BookDetails", "Book", new { id = bookId });
        }

        var lastId = db.Reservations.OrderByDescending(r => r.Id).Select(r => r.Id).FirstOrDefault();
        var reservation = new Reservation
        {
            Id = Helper.GenerateNewId(lastId, 'R'),
            UserId = user.Id,
            BookId = bookId,
            ReservationDate = DateTime.Now,
            Status = Reservation.ReservationStatus.PENDING
        };

        db.Reservations.Add(reservation);
        db.SaveChanges();

        TempData["ToastMessage"] = "Reservation placed successfully. You will be notified when a copy is available.";
        TempData["ToastType"] = "success";

        return RedirectToAction("BookDetails", "Book", new { id = bookId });
    }

    [HttpPost]
    public IActionResult Cancel(string id, string? returnUrl)
    {
        var reservation = db.Reservations.Find(id);
        if (reservation == null) return NotFound();

        if (reservation.Status == Reservation.ReservationStatus.READY && reservation.CopyId != null)
        {
            // Release the copy to the next person
            reservation.Status = Reservation.ReservationStatus.CANCELLED;
            db.SaveChanges(); // Save cancellation first
            
            rs.AssignNextReservation(reservation.BookId, reservation.CopyId);
        }
        else
        {
            reservation.Status = Reservation.ReservationStatus.CANCELLED;
            db.SaveChanges();
        }

        TempData["ToastMessage"] = "Reservation cancelled.";
        TempData["ToastType"] = "success";

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult BatchCancel(List<string> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            TempData["ToastMessage"] = "No items selected.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("Index");
        }

        var username = User.Identity?.Name;
        var user = db.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) return RedirectToAction("Login", "Account");

        var reservations = db.Reservations
            .Where(r => ids.Contains(r.Id) && r.UserId == user.Id && 
                        (r.Status == Reservation.ReservationStatus.PENDING || r.Status == Reservation.ReservationStatus.READY))
            .ToList();

        foreach (var res in reservations)
        {
            res.Status = Reservation.ReservationStatus.CANCELLED;
        }
        
        db.SaveChanges();

        foreach (var res in reservations)
        {
            if (res.CopyId != null) // It was READY, so release copy
            {
                rs.AssignNextReservation(res.BookId, res.CopyId);
            }
        }

        TempData["ToastMessage"] = $"Successfully cancelled {reservations.Count} reservation(s).";
        TempData["ToastType"] = "success";

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Confirm(string id)
    {
        var reservation = db.Reservations.Find(id);
        if (reservation == null || reservation.Status != Reservation.ReservationStatus.READY || reservation.CopyId == null)
        {
            return BadRequest("Invalid reservation state.");
        }

        var lastId = db.Loans.OrderByDescending(l => l.Id).Select(l => l.Id).FirstOrDefault();
        var loan = new Loan
        {
            Id = Helper.GenerateNewId(lastId, 'L'),
            UserId = reservation.UserId,
            CopyId = reservation.CopyId,
            LoanDate = DateOnly.FromDateTime(DateTime.Now),
            PickupDue = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
            LoanDue = DateOnly.FromDateTime(DateTime.Now.AddDays(14)),
            Status = Loan.LoanStatus.RESERVED
        };

        reservation.Status = Reservation.ReservationStatus.COMPLETED;
        db.Loans.Add(loan);
        db.SaveChanges();

        return RedirectToAction("Confirmation", "Loan", new { id = loan.Id });
    }
}
