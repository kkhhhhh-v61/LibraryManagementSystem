﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Extensions;

namespace LibraryManagementSystem.Controllers
{
    [Authorize]
    public class LoanController(LMSDB db) : Controller
    {
        public IActionResult History(string? search, string? status, int page = 1, string sort = "LoanDate", string order = "desc")
        {
            var username = User.Identity?.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToAction("Login", "Account");

            var query = db.Loans
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .Include(l => l.Review)
                .Where(l => l.UserId == user.Id)
                .AsQueryable();

            // Search (Book Title or Loan ID)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(l => l.Copy.Book.Title.Contains(search) || l.Id.Contains(search));
            }

            // Filter Status
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Loan.LoanStatus>(status, true, out var statusEnum))
            {
                query = query.Where(l => l.Status == statusEnum);
            }

            // Sorting
            bool isDesc = order == "desc";
            query = sort switch
            {
                "Id" => isDesc ? query.OrderByDescending(l => l.Id) : query.OrderBy(l => l.Id),
                "Book" => isDesc ? query.OrderByDescending(l => l.Copy.Book.Title) : query.OrderBy(l => l.Copy.Book.Title),
                "LoanDate" => isDesc ? query.OrderByDescending(l => l.LoanDate) : query.OrderBy(l => l.LoanDate),
                "DueDate" => isDesc ? query.OrderByDescending(l => l.LoanDue) : query.OrderBy(l => l.LoanDue),
                "Status" => isDesc ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
                _ => isDesc ? query.OrderByDescending(l => l.LoanDate) : query.OrderBy(l => l.LoanDate)
            };

            // Pagination
            int pageSize = 10;
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalItems = totalItems;
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new LoanHistoryVM
            {
                Loans = items,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = search,
                SortColumn = sort,
                SortOrder = order,
                FilterStatus = status
            };

            if (Request.IsAjax())
            {
                return PartialView("_TableHistory", vm);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string bookId)
        {
            var username = User.Identity?.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null) return RedirectToAction("Login", "Account");

            // Check if the user already has an active loan for this book title.
            var hasActiveLoan = db.Loans
                .Include(l => l.Copy)
                .Any(l => l.UserId == user.Id &&
                          l.Copy.BookId == bookId &&
                          (l.Status == Loan.LoanStatus.RESERVED ||
                           l.Status == Loan.LoanStatus.IN_LOAN ||
                           l.Status == Loan.LoanStatus.OVERDUE));

            if (hasActiveLoan)
            {
                TempData["ToastMessage"] = "You already have an active loan for this book.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("BookDetails", "Book", new { id = bookId });
            }

            // Find an available copy
            var copy = db.Copies.FirstOrDefault(c => c.BookId == bookId && c.Status == Copy.CopyStatus.AVAILABLE);

            if (copy == null)
            {
                TempData["ToastMessage"] = "Sorry, no copies are currently available.";
                TempData["ToastType"] = "error";
                return RedirectToAction("BookDetails", "Book", new { id = bookId });
            }

            // Create Loan
            var lastId = db.Loans.OrderByDescending(l => l.Id).Select(l => l.Id).FirstOrDefault();
            var loan = new Loan
            {
                Id = Helper.GenerateNewId(lastId, 'L'),
                UserId = user.Id,
                CopyId = copy.Id,
                LoanDate = DateOnly.FromDateTime(DateTime.Now),
                PickupDue = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
                LoanDue = DateOnly.FromDateTime(DateTime.Now.AddDays(14)),
                Status = Loan.LoanStatus.RESERVED
            };

            copy.Status = Copy.CopyStatus.RESERVED;
            db.Loans.Add(loan);
            db.SaveChanges();

            return RedirectToAction("Confirmation", new { id = loan.Id });
        }

        [HttpPost]
        [Authorize]
        public IActionResult BatchCancelLoan(List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                TempData["ToastMessage"] = "No items selected.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("History");
            }

            var username = User.Identity?.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToAction("Login", "Account");

            var loans = db.Loans
                .Include(l => l.Copy)
                .Where(l => ids.Contains(l.Id) && l.UserId == user.Id && l.Status == Loan.LoanStatus.RESERVED)
                .ToList();

            if (loans.Count == 0)
            {
                TempData["ToastMessage"] = "No cancellable loans found in selection.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("History");
            }

            foreach (var loan in loans)
            {
                if (loan.Copy != null) loan.Copy.Status = Copy.CopyStatus.AVAILABLE;
            }

            db.Loans.RemoveRange(loans);
            db.SaveChanges();

            TempData["ToastMessage"] = $"Successfully cancelled {loans.Count} loan(s).";
            TempData["ToastType"] = "success";

            return RedirectToAction("History");
        }

        [HttpPost]
        [Authorize]
        public IActionResult CancelLoan(string id)
        {
            var username = User.Identity?.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToAction("Login", "Account");

            var loan = db.Loans
                .Include(l => l.Copy)
                .FirstOrDefault(l => l.Id == id && l.UserId == user.Id && l.Status == Loan.LoanStatus.RESERVED);

            if (loan != null)
            {
                if (loan.Copy != null) loan.Copy.Status = Copy.CopyStatus.AVAILABLE;
                db.Loans.Remove(loan);
                db.SaveChanges();
                TempData["ToastMessage"] = "Loan cancelled successfully.";
                TempData["ToastType"] = "success";
            }

            return RedirectToAction("History");
        }

        public IActionResult Confirmation(string id)
        {
            var username = User.Identity?.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return RedirectToAction("Login", "Account");

            var loan = db.Loans
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .Include(l => l.User)
                .FirstOrDefault(l => l.Id == id);

            if (loan == null || loan.UserId != user.Id)
            {
                return RedirectToAction("History");
            }

            return View(loan);
        }

        [HttpPost]
        public async Task<IActionResult> MergedConfirmation(List<string> ids)
        {
            if (ids == null || ids.Count <= 1)
            {
                TempData["ToastMessage"] = "Please select more than one loan to merge.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("History");
            }

            var username = User.Identity?.Name;
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return RedirectToAction("Login", "Account");

            var loans = await db.Loans
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .Include(l => l.User)
                .Where(l => ids.Contains(l.Id) && l.UserId == user.Id && (l.Status == Loan.LoanStatus.RESERVED || l.Status == Loan.LoanStatus.IN_LOAN || l.Status == Loan.LoanStatus.OVERDUE))
                .OrderBy(l => l.LoanDate)
                .ToListAsync();

            if (!loans.Any())
            {
                return RedirectToAction("History");
            }

            return View(loans);
        }

        [Authorize(Roles = "ADMIN, STAFF")]
        public IActionResult ManageLoan(string? search, string? status, DateOnly? dateFrom, DateOnly? dateTo, int page = 1, string sort = "Id", string order = "desc")
        {
            var query = db.Loans
                .Include(l => l.User)
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(l => l.Id.Contains(search) ||
                                         l.User.Username.Contains(search) ||
                                         l.Copy.Book.Title.Contains(search) ||
                                         l.Copy.Id.Contains(search));
            }

            // Filter Status
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Loan.LoanStatus>(status, true, out var statusEnum))
            {
                query = query.Where(l => l.Status == statusEnum);
            }

            // Filter Date (LoanDate)
            if (dateFrom.HasValue)
            {
                query = query.Where(l => l.LoanDate >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                query = query.Where(l => l.LoanDate <= dateTo.Value);
            }

            // Sorting
            bool isDesc = order == "desc";
            query = sort switch
            {
                "Id" => isDesc ? query.OrderByDescending(l => l.Id) : query.OrderBy(l => l.Id),
                "User" => isDesc ? query.OrderByDescending(l => l.User.Username) : query.OrderBy(l => l.User.Username),
                "Book" => isDesc ? query.OrderByDescending(l => l.Copy.Book.Title) : query.OrderBy(l => l.Copy.Book.Title),
                "LoanDate" => isDesc ? query.OrderByDescending(l => l.LoanDate) : query.OrderBy(l => l.LoanDate),
                "DueDate" => isDesc ? query.OrderByDescending(l => l.LoanDue) : query.OrderBy(l => l.LoanDue),
                "PickupDate" => isDesc ? query.OrderByDescending(l => l.PickupDate) : query.OrderBy(l => l.PickupDate),
                "PickupDue" => isDesc ? query.OrderByDescending(l => l.PickupDue) : query.OrderBy(l => l.PickupDue),
                "ReturnDate" => isDesc ? query.OrderByDescending(l => l.ReturnDate) : query.OrderBy(l => l.ReturnDate),
                "Status" => isDesc ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
                _ => isDesc ? query.OrderByDescending(l => l.Id) : query.OrderBy(l => l.Id)
            };

            // Pagination
            int pageSize = 10;
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalItems = db.Loans.Count();
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new LoanManagementVM
            {
                ExistingLoans = items,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = search,
                SortColumn = sort,
                SortOrder = order,
                FilterStatus = status,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            // Date limits for date picker
            var allDates = db.Loans.Select(l => l.LoanDate);
            ViewBag.MinDate = allDates.Any() ? allDates.Min().ToString("yyyy-MM-dd") : null;
            ViewBag.MaxDate = allDates.Any() ? allDates.Max().ToString("yyyy-MM-dd") : null;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            if (Request.IsAjax())
            {
                return PartialView("_TableLoan", vm);
            }

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public IActionResult DeleteLoan(string id)
        {
            var loan = db.Loans.Find(id);
            if (loan != null)
            {
                db.Loans.Remove(loan);
                db.SaveChanges();
                TempData["ToastMessage"] = "Loan record deleted successfully.";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Loan not found.";
                TempData["ToastType"] = "error";
            }
            return RedirectToAction("ManageLoan");
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public IActionResult BatchDeleteLoan(List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                TempData["ToastMessage"] = "No items selected.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("ManageLoan");
            }

            var loans = db.Loans.Where(l => ids.Contains(l.Id)).ToList();
            db.Loans.RemoveRange(loans);
            db.SaveChanges();

            TempData["ToastMessage"] = $"Successfully deleted {loans.Count} loan record(s).";
            TempData["ToastType"] = "success";

            return RedirectToAction("ManageLoan");
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public IActionResult EditLoan([Bind(Prefix = "EditForm")] LoanEditVM vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["ToastMessage"] = "Invalid data provided. Please check the values.";
                TempData["ToastType"] = "error";
                return RedirectToAction("ManageLoan");
            }

            // Server-side validation for pickup date
            if (vm.PickupDate.HasValue && vm.PickupDate.Value > vm.PickupDue)
            {
                TempData["ToastMessage"] = "Actual Pickup date cannot be after the pickup deadline.";
                TempData["ToastType"] = "error";
                return RedirectToAction("ManageLoan");
            }

            if (vm.LoanDue < vm.PickupDue)
            {
                TempData["ToastMessage"] = "Due Date cannot be before Pickup Deadline.";
                TempData["ToastType"] = "error";
                return RedirectToAction("ManageLoan");
            }

            var loan = db.Loans.Find(vm.Id);
            if (loan == null)
            {
                TempData["ToastMessage"] = "Loan record not found.";
                TempData["ToastType"] = "error";
                return RedirectToAction("ManageLoan");
            }

            loan.LoanDate = vm.LoanDate;
            loan.PickupDue = vm.PickupDue;
            loan.PickupDate = vm.PickupDate;
            loan.LoanDue = vm.LoanDue;
            loan.ReturnDate = vm.ReturnDate;

            // Automatically determine the status based on the dates
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (loan.ReturnDate.HasValue)
            {
                loan.Status = Loan.LoanStatus.RETURNED;
            }
            else if (loan.PickupDate.HasValue)
            {
                loan.Status = (loan.LoanDue < today) ? Loan.LoanStatus.OVERDUE : Loan.LoanStatus.IN_LOAN;
            }
            else // Not picked up yet
            {
                loan.Status = (loan.PickupDue < today) ? Loan.LoanStatus.EXPIRED : Loan.LoanStatus.RESERVED;
            }

            db.SaveChanges();
            TempData["ToastMessage"] = "Loan updated successfully.";
            TempData["ToastType"] = "success";
            return RedirectToAction("ManageLoan");
        }

        [Authorize(Roles = "ADMIN, STAFF")]
        public IActionResult Process()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN, STAFF")]
        public IActionResult GetUserReservations(string userId)
        {
            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            var reservations = db.Loans
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .Where(l => l.UserId == user.Id && l.Status == Loan.LoanStatus.RESERVED)
                .ToList();

            return PartialView("_ListPickup", reservations);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN, STAFF")]
        public IActionResult ConfirmPickup(List<string> loanIds)
        {
            if (loanIds == null || !loanIds.Any())
            {
                return Json(new { success = false, message = "No items selected." });
            }

            var loans = db.Loans
                .Include(l => l.Copy)
                .Where(l => loanIds.Contains(l.Id) && l.Status == Loan.LoanStatus.RESERVED)
                .ToList();

            foreach (var loan in loans)
            {
                loan.Status = Loan.LoanStatus.IN_LOAN;
                loan.PickupDate = DateOnly.FromDateTime(DateTime.Now);
                if (loan.Copy != null) loan.Copy.Status = Copy.CopyStatus.BORROWED;
            }

            db.SaveChanges();
            return Json(new { success = true, count = loans.Count, message = $"Successfully processed {loans.Count} pickup(s)." });
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN, STAFF")]
        public IActionResult ProcessReturn(string barcode)
        {
            var copy = db.Copies.FirstOrDefault(c => c.Barcode == barcode);
            if (copy == null) return Json(new { success = false, message = "Copy not found." });

            var loan = db.Loans
                .Include(l => l.User)
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .FirstOrDefault(l => l.CopyId == copy.Id && 
                                     (l.Status == Loan.LoanStatus.IN_LOAN || l.Status == Loan.LoanStatus.OVERDUE));

            if (loan == null) return Json(new { success = false, message = "No active loan found for this copy." });

            loan.Status = Loan.LoanStatus.RETURNED;
            loan.ReturnDate = DateOnly.FromDateTime(DateTime.Now);
            copy.Status = Copy.CopyStatus.AVAILABLE;

            db.SaveChanges();

            return Json(new { 
                success = true, 
                message = "Book returned successfully.",
                bookTitle = loan.Copy.Book.Title,
                username = loan.User.Username
            });
        }
    }
}
