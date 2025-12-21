﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Extensions;
using Microsoft.AspNetCore.Authorization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using LibraryManagementSystem.Services;

namespace LibraryManagementSystem.Controllers;

public class BookController(LMSDB db, IWebHostEnvironment en, INotificationService ns, IReservationService rs) : Controller
{
    public IActionResult Catalogue(string? search, string? genreId, string? authorId, int? rating, int? year, int? edition)
    {
        var nonArchivedBooks = db.Books.Where(b => !b.IsArchived);

        var genres = nonArchivedBooks
            .SelectMany(b => b.Genres)
            .Distinct()
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem(g.Name, g.Id)).ToList();
        var authors = nonArchivedBooks
            .SelectMany(b => b.Authors)
            .Distinct()
            .OrderBy(a => a.Name)
            .Select(a => new SelectListItem(a.Name, a.Id)).ToList();
        var years = nonArchivedBooks.Select(b => b.PublicationYear).Distinct().OrderByDescending(y => y).Select(y => new SelectListItem(y.ToString(), y.ToString())).ToList();
        var editions = nonArchivedBooks.Select(b => b.Edition).Distinct().OrderBy(e => e).Select(e => new SelectListItem(e.ToString(), e.ToString())).ToList();

        ViewBag.Genres = genres;
        ViewBag.Authors = authors;
        ViewBag.Years = years;
        ViewBag.Editions = editions;

        var query = db.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .Where(b => !b.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(b => b.Title.Contains(search) || b.ISBN.Contains(search) || b.Authors.Any(a => a.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(genreId))
        {
            query = query.Where(b => b.Genres.Any(g => g.Id == genreId));
        }

        if (!string.IsNullOrWhiteSpace(authorId))
        {
            query = query.Where(b => b.Authors.Any(a => a.Id == authorId));
        }

        if (year.HasValue)
        {
            query = query.Where(b => b.PublicationYear == year.Value);
        }

        if (edition.HasValue)
        {
            query = query.Where(b => b.Edition == edition.Value);
        }

        var books = query.OrderByDescending(b => b.DateAdded).ToList();

        // Calculate Ratings
        var bookIds = books.Select(b => b.Id).ToList();
        var ratingsData = db.Reviews
            .Include(r => r.Loan)
            .ThenInclude(l => l.Copy)
            .Where(r => bookIds.Contains(r.Loan.Copy.BookId))
            .GroupBy(r => r.Loan.Copy.BookId)
            .Select(g => new { BookId = g.Key, Average = g.Average(r => r.Rating) })
            .ToList();

        var ratingDict = ratingsData.ToDictionary(x => x.BookId, x => x.Average);

        if (rating.HasValue)
        {
            books = books.Where(b => ratingDict.ContainsKey(b.Id) && ratingDict[b.Id] >= rating.Value).ToList();
        }

        ViewBag.BookRatings = ratingDict;

        if (Request.IsAjax())
        {
            return PartialView("_GridCatalogue", books);
        }

        return View(books);
    }

    public IActionResult BookDetails(string id, int? rating, int page = 1)
    {
        var book = db.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .FirstOrDefault(b => b.Id == id);

        if (book == null)
        {
            TempData["ToastMessage"] = "Book not found.";
            TempData["ToastType"] = "error";
            return RedirectToAction("Catalogue");
        }

        // Availability
        var copies = db.Copies.Where(c => c.BookId == id).ToList();
        ViewBag.TotalCopies = copies.Count;
        ViewBag.AvailableCopies = copies.Count(c => c.Status == Copy.CopyStatus.AVAILABLE);

        // Reviews Query
        var reviewsQuery = db.Reviews
            .Include(r => r.Loan)
            .ThenInclude(l => l.User)
            .Where(r => r.Loan.Copy.BookId == id);

        // Global Stats
        int totalReviews = reviewsQuery.Count();
        double avgRating = totalReviews > 0 ? reviewsQuery.Average(r => r.Rating) : 0.0;
        ViewBag.AverageRating = avgRating;
        ViewBag.TotalReviews = totalReviews;

        Review? userReview = null;
        string? currentUserId = null;
        ViewBag.HasActiveLoan = false;
        ViewBag.ActiveLoanId = null;
        ViewBag.HasActiveReservation = false;
        ViewBag.ActiveReservationId = null;

        if (User.Identity!.IsAuthenticated)
        {
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                currentUserId = user.Id;
                var activeLoan = db.Loans
                    .Include(l => l.Copy)
                    .FirstOrDefault(l => l.UserId == user.Id &&
                              l.Copy.BookId == id &&
                              (l.Status == Loan.LoanStatus.RESERVED ||
                               l.Status == Loan.LoanStatus.IN_LOAN ||
                               l.Status == Loan.LoanStatus.OVERDUE));
                
                if (activeLoan != null)
                {
                    ViewBag.HasActiveLoan = true;
                    ViewBag.ActiveLoanId = activeLoan.Id;
                }

                var activeReservation = db.Reservations.FirstOrDefault(r => r.UserId == user.Id && r.BookId == id && (r.Status == Reservation.ReservationStatus.PENDING || r.Status == Reservation.ReservationStatus.READY));

                if (activeReservation != null)
                {
                    ViewBag.HasActiveReservation = true;
                    ViewBag.ActiveReservationId = activeReservation.Id;
                }

                // Find user's review for this book
                userReview = reviewsQuery.FirstOrDefault(r => r.Loan.UserId == user.Id);
            }
        }

        ViewBag.CurrentUserId = currentUserId;

        // Filtering
        if (rating.HasValue)
        {
            reviewsQuery = reviewsQuery.Where(r => r.Rating == rating.Value);

            // If user review doesn't match filter, ignore it for pinning
            if (userReview != null && userReview.Rating != rating.Value)
            {
                userReview = null;
            }
        }

        // Exclude user review from main query to avoid duplication
        if (userReview != null)
        {
            reviewsQuery = reviewsQuery.Where(r => r.Id != userReview.Id);
        }

        // Sorting & Pagination
        int pageSize = 5;
        int filteredCount = reviewsQuery.Count();
        int totalPages = (int)Math.Ceiling(filteredCount / (double)pageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        // Ensure at least 1 page if we have a pinned review but no other reviews
        if (totalPages == 0 && userReview != null) totalPages = 1;

        var pagedReviews = reviewsQuery.OrderByDescending(r => r.ReviewDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // Pin user review to the top if on page 1
        if (page == 1 && userReview != null)
        {
            pagedReviews.Insert(0, userReview);
        }

        ViewBag.Reviews = pagedReviews;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.CurrentRating = rating;
        ViewBag.BookId = id;

        if (Request.IsAjax())
        {
            return PartialView("_ReviewList", pagedReviews);
        }

        return View("BookDetail", book);
    }

    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult ManageBook(string? search, string? authorId, string? genreId, bool? featured, bool? archived, DateOnly? dateFrom, DateOnly? dateTo, int page = 1, string sort = "Title", string order = "asc")
    {
        var query = db.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(b => b.Title.Contains(search) || b.ISBN.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(authorId))
        {
            query = query.Where(b => b.Authors.Any(a => a.Id == authorId));
        }

        if (!string.IsNullOrWhiteSpace(genreId))
        {
            query = query.Where(b => b.Genres.Any(g => g.Id == genreId));
        }

        if (featured.HasValue && featured.Value)
        {
            query = query.Where(b => b.IsFeatured);
        }

        if (archived.HasValue && archived.Value)
        {
            query = query.Where(b => b.IsArchived);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(b => b.DateAdded >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(b => b.DateAdded <= dateTo.Value);
        }

        bool isDesc = order == "desc";
        query = sort switch
        {
            "Id" => isDesc ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Id),
            "Title" => isDesc ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
            "DateAdded" => isDesc ? query.OrderByDescending(b => b.DateAdded) : query.OrderBy(b => b.DateAdded),
            _ => isDesc ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title)
        };

        int pageSize = 10;
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        if (totalItems <= 1)
        {
            dateFrom ??= DateOnly.FromDateTime(DateTime.Today);
            dateTo ??= DateOnly.FromDateTime(DateTime.Today);
        }

        ViewBag.TotalItems = db.Books.Count();
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var bookIds = items.Select(b => b.Id).ToList();
        var activeCopiesBookIds = db.Copies
            .Where(c => bookIds.Contains(c.BookId) && (c.Status == Copy.CopyStatus.RESERVED || c.Status == Copy.CopyStatus.BORROWED))
            .Select(c => c.BookId)
            .Distinct()
            .ToHashSet();

        var vm = new BookManagementVM
        {
            ExistingBooks = items,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchTerm = search,
            SortColumn = sort,
            SortOrder = order,
            SelectedAuthorId = authorId,
            SelectedGenreId = genreId,
            FilterFeatured = featured,
            FilterArchived = archived,
            AuthorList = db.Books.SelectMany(b => b.Authors).Distinct().OrderBy(a => a.Name).Select(a => new SelectListItem(a.Name, a.Id)).ToList(),
            GenreList = db.Books.SelectMany(b => b.Genres).Distinct().OrderBy(g => g.Name).Select(g => new SelectListItem(g.Name, g.Id)).ToList(),
            CanDelete = items.ToDictionary(b => b.Id, b => !activeCopiesBookIds.Contains(b.Id))
        };

        var allDates = db.Books.Select(b => b.DateAdded);
        ViewBag.MinDate = allDates.Any() ? allDates.Min().ToString("yyyy-MM-dd") : null;
        ViewBag.MaxDate = allDates.Any() ? allDates.Max().ToString("yyyy-MM-dd") : null;
        ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
        ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

        if (Request.IsAjax())
        {
            return PartialView("_TableBook", vm);
        }

        return View(vm);
    }

    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult InsertBook()
    {
        var vm = new BookInsertVM
        {
            PublicationYear = DateTime.Now.Year,
            Edition = 1,
            IsFeatured = false,
            IsArchived = false,
        };
        PopulateDropdowns(vm);

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult InsertBook(BookInsertVM vm)
    {
        // 1. ISBN Validation
        string cleanISBN = vm.ISBN.Replace("-", "").Trim().Replace(" ", "").ToUpper();
        if (db.Books.Any(b => b.ISBN == cleanISBN))
        {
            ModelState.AddModelError("ISBN", "This ISBN already exists.");
        }

        // 2. Image Validation
        if (vm.Image != null)
        {
            var imageResult = Helper.ValidateImage(vm.Image);
            if (imageResult != ValidationResult.Success)
            {
                ModelState.AddModelError("Image", imageResult.ErrorMessage!);
            }
        }

        // 3. Early Exit
        if (!ModelState.IsValid)
        {
            TempData["ToastMessage"] = "Failed to add Book. Please check errors.";
            TempData["ToastType"] = "error";
            PopulateDropdowns(vm);
            return View(vm);
        }

        // 4. Save Image
        string? imageUrl = null;
        if (vm.Image != null)
        {
            if (vm.CropWidth.HasValue && vm.CropHeight.HasValue)
            {
                imageUrl = SaveCroppedImage(vm.Image, vm.CropX ?? 0, vm.CropY ?? 0, vm.CropWidth.Value, vm.CropHeight.Value, en.WebRootPath);
            }
            else
            {
                imageUrl = Helper.SaveImage(vm.Image, "books", en.WebRootPath);
            }
        }

        // 5. Generate ID
        var lastId = db.Books
                       .OrderByDescending(b => b.Id)
                       .Select(b => b.Id)
                       .FirstOrDefault();
        string newBookId = Helper.GenerateNewId(lastId, 'B');

        // 6. Create Entity
        var newBook = new Book
        {
            Id = newBookId,
            Title = vm.Title.Trim(),
            Description = vm.Description.Trim(),
            ISBN = cleanISBN,
            PublicationYear = vm.PublicationYear,
            Edition = vm.Edition,
            ImageURL = imageUrl,
            IsFeatured = vm.IsFeatured,
            IsArchived = vm.IsArchived,
            DateAdded = DateOnly.FromDateTime(DateTime.Now),
        };

        // 7. Link Relationships
        foreach (var genreId in vm.SelectedGenreIds)
        {
            var genre = db.Genres.Find(genreId);
            if (genre != null) newBook.Genres.Add(genre);
        }

        foreach (var authorId in vm.SelectedAuthorIds)
        {
            var author = db.Authors.Find(authorId);
            if (author != null) newBook.Authors.Add(author);
        }

        // 8. Save Changes
        db.Books.Add(newBook);
        db.SaveChanges();

        // 9. Success Feedback
        TempData["ToastMessage"] = $"Book '{vm.Title}' added successfully.";
        TempData["ToastType"] = "success";
        TempData["BookInsertSuccess"] = true;

        return RedirectToAction("ManageBook");
    }

    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult EditBook(string id, string? returnUrl)
    {
        var book = db.Books
            .Include(b => b.Genres)
            .Include(b => b.Authors)
            .FirstOrDefault(b => b.Id == id);

        if (book == null)
        {
            TempData["ToastMessage"] = "Book not found.";
            TempData["ToastType"] = "error";
            return RedirectToAction("ManageBook");
        }

        var vm = new BookEditVM
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description,
            ExistingImageURL = book.ImageURL,
            ISBN = book.ISBN,
            PublicationYear = book.PublicationYear,
            Edition = book.Edition,
            IsFeatured = book.IsFeatured,
            IsArchived = book.IsArchived,
            SelectedGenreIds = book.Genres.Select(g => g.Id).ToList(),
            SelectedAuthorIds = book.Authors.Select(a => a.Id).ToList()
        };

        ViewBag.ReturnUrl = returnUrl;
        PopulateDropdowns(vm);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult EditBook(BookEditVM vm)
    {
        // 1. ISBN Validation
        string cleanISBN = vm.ISBN.Replace("-", "").Trim().Replace(" ", "").ToUpper();
        if (db.Books.Any(b => b.ISBN == cleanISBN && b.Id != vm.Id))
        {
            ModelState.AddModelError("ISBN", "This ISBN is already assigned to another book.");
        }

        // 2. Image Validation
        if (vm.Image != null)
        {
            var imageResult = Helper.ValidateImage(vm.Image);
            if (imageResult != ValidationResult.Success)
            {
                ModelState.AddModelError("Image", imageResult.ErrorMessage!);
            }
        }

        // 3. Early Exit
        if (!ModelState.IsValid)
        {
            TempData["ToastMessage"] = "Failed to update Book. Please check errors.";
            TempData["ToastType"] = "error";
            var existingBook = db.Books.AsNoTracking().FirstOrDefault(b => b.Id == vm.Id);
            if (existingBook != null) vm.ExistingImageURL = existingBook.ImageURL;
            PopulateDropdowns(vm);
            return View(vm);
        }

        var book = db.Books
            .Include(b => b.Genres)
            .Include(b => b.Authors)
            .FirstOrDefault(b => b.Id == vm.Id);

        if (book == null)
        {
            TempData["ToastMessage"] = "Book not found.";
            TempData["ToastType"] = "error";
            return RedirectToAction("ManageBook");
        }

        // 4. Save Image
        if (vm.Image != null)
        {
            DeleteImage(book.ImageURL);
            if (vm.CropWidth.HasValue && vm.CropHeight.HasValue)
            {
                book.ImageURL = SaveCroppedImage(vm.Image, vm.CropX ?? 0, vm.CropY ?? 0, vm.CropWidth.Value, vm.CropHeight.Value, en.WebRootPath);
            }
            else
            {
                book.ImageURL = Helper.SaveImage(vm.Image, "books", en.WebRootPath);
            }
        }

        // 5. Update Properties
        book.Title = vm.Title.Trim();
        book.Description = vm.Description.Trim();
        book.ISBN = cleanISBN;
        book.PublicationYear = vm.PublicationYear;
        book.Edition = vm.Edition;
        book.IsFeatured = vm.IsFeatured;
        book.IsArchived = vm.IsArchived;

        // 6. Update Relationships
        book.Genres.Clear();
        foreach (var genreId in vm.SelectedGenreIds)
        {
            var genre = db.Genres.Find(genreId);
            if (genre != null) book.Genres.Add(genre);
        }

        book.Authors.Clear();
        foreach (var authorId in vm.SelectedAuthorIds)
        {
            var author = db.Authors.Find(authorId);
            if (author != null) book.Authors.Add(author);
        }

        // 7. Save Changes
        db.SaveChanges();

        // 8. Success Feedback
        TempData["ToastMessage"] = $"Book '{vm.Title}' updated successfully.";
        TempData["ToastType"] = "success";
        TempData["BookInsertSuccess"] = true; // Clears session storage for image preview

        return RedirectToAction("ManageBook");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult DeleteBook(string id)
    {
        var book = db.Books.Find(id);
        if (book != null)
        {
            DeleteImage(book.ImageURL);

            // TODO: Modify this logic after implementing Copy and Loans
            db.Books.Remove(book);
            db.SaveChanges();
            TempData["ToastMessage"] = $"Book '{book.Title}' deleted successfully.";
            TempData["ToastType"] = "success";
        }
        else
        {
            TempData["ToastMessage"] = "Book not found.";
            TempData["ToastType"] = "error";
        }
        return RedirectToAction("ManageBook");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult BatchDeleteBook(List<string> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            TempData["ToastMessage"] = "No items selected.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("ManageBook");
        }

        var books = db.Books.Where(b => ids.Contains(b.Id)).ToList();

        foreach (var book in books)
        {
            DeleteImage(book.ImageURL);
        }

        db.Books.RemoveRange(books);
        db.SaveChanges();

        TempData["ToastMessage"] = $"Successfully deleted {books.Count} book(s).";
        TempData["ToastType"] = "success";

        return RedirectToAction("ManageBook");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult AddCopy(string bookId, int quantity, string? returnUrl)
    {
        if (quantity < 1)
        {
            TempData["ToastMessage"] = "Quantity must be at least 1.";
            TempData["ToastType"] = "error";
            return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction("ManageBook");
        }

        var book = db.Books.Find(bookId);
        if (book == null)
        {
            TempData["ToastMessage"] = "Book not found.";
            TempData["ToastType"] = "error";
            return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction("ManageBook");
        }

        var addedCopies = new List<object>();
        var lastCopy = db.Copies.OrderByDescending(c => c.Id).FirstOrDefault();
        string? lastId = lastCopy?.Id;

        for (int i = 0; i < quantity; i++)
        {
            string newId = Helper.GenerateNewId(lastId, 'C');
            lastId = newId;

            var copy = new Copy { Id = newId, BookId = bookId, Status = Copy.CopyStatus.AVAILABLE, Barcode = $"{bookId}-{newId}" };
            db.Copies.Add(copy);
            addedCopies.Add(new { copy.Id, copy.Barcode });
        }

        db.SaveChanges();

        foreach (var obj in addedCopies)
        {
            var copyId = obj.GetType().GetProperty("Id")?.GetValue(obj, null) as string;
            if (copyId != null) rs.AssignNextReservation(bookId, copyId);
        }

        TempData["ToastMessage"] = $"{quantity} copies added successfully.";
        TempData["ToastType"] = "success";
        TempData["AddedCopies"] = System.Text.Json.JsonSerializer.Serialize(addedCopies);

        if (!string.IsNullOrEmpty(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("ManageBook");
    }

    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult GetCopiesList(string bookId)
    {
        var copies = db.Copies.Where(c => c.BookId == bookId).OrderBy(c => c.Id).ToList();
        return PartialView("_ListCopy", copies);
    }

    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult ManageBookDetail(string id, string? search, string? status, int page = 1, string sort = "Id", string order = "asc")
    {
        var book = db.Books
            .Include(b => b.Authors)
            .Include(b => b.Genres)
            .FirstOrDefault(b => b.Id == id);

        if (book == null)
        {
            TempData["ToastMessage"] = "Book not found.";
            TempData["ToastType"] = "error";
            return RedirectToAction("ManageBook");
        }

        var query = db.Copies.Where(c => c.BookId == id).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.Id.Contains(search) || c.Barcode.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Copy.CopyStatus>(status, true, out var statusEnum))
        {
            query = query.Where(c => c.Status == statusEnum);
        }

        bool isDesc = order == "desc";
        query = sort switch
        {
            "Id" => isDesc ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id),
            "Barcode" => isDesc ? query.OrderByDescending(c => c.Barcode) : query.OrderBy(c => c.Barcode),
            "Status" => isDesc ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            _ => isDesc ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id)
        };

        int pageSize = 10;
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.TotalItems = db.Copies.Count(c => c.BookId == id);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var copies = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new BookDetailVM
        {
            Book = book,
            Copies = copies,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchTerm = search,
            SortColumn = sort,
            SortOrder = order
        };

        ViewBag.FilterStatus = status;

        if (Request.IsAjax())
        {
            return PartialView("_TableCopy", vm);
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult DeleteCopy(string id, string bookId)
    {
        var copy = db.Copies.Find(id);
        if (copy != null)
        {
            if (copy.Status != Copy.CopyStatus.AVAILABLE)
            {
                TempData["ToastMessage"] = "Cannot delete a copy that is not available.";
                TempData["ToastType"] = "error";
                return RedirectToAction("ManageBookDetail", new { id = bookId });
            }

            db.Copies.Remove(copy);
            db.SaveChanges();
            TempData["ToastMessage"] = "Copy deleted successfully.";
            TempData["ToastType"] = "success";
        }
        else
        {
            TempData["ToastMessage"] = "Copy not found.";
            TempData["ToastType"] = "error";
        }
        return RedirectToAction("ManageBookDetail", new { id = bookId });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult BatchDeleteCopy(List<string> ids, string bookId)
    {
        if (ids == null || ids.Count == 0)
        {
            TempData["ToastMessage"] = "No items selected.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("ManageBookDetail", new { id = bookId });
        }

        var copies = db.Copies.Where(c => ids.Contains(c.Id)).ToList();
        
        var safeToDelete = copies.Where(c => c.Status == Copy.CopyStatus.AVAILABLE).ToList();
        var unsafeItems = copies.Where(c => c.Status != Copy.CopyStatus.AVAILABLE).ToList();

        if (safeToDelete.Count > 0)
        {
            db.Copies.RemoveRange(safeToDelete);
            db.SaveChanges();
        }

        if (unsafeItems.Count > 0)
        {
            TempData["ToastMessage"] = $"Deleted {safeToDelete.Count} copy(s). Skipped {unsafeItems.Count} copy(s) because they are not available.";
            TempData["ToastType"] = "warning";
        }
        else
        {
            TempData["ToastMessage"] = $"Successfully deleted {safeToDelete.Count} copy(s).";
            TempData["ToastType"] = "success";
        }

        return RedirectToAction("ManageBookDetail", new { id = bookId });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult ToggleBookStatus(string id, string field, bool value)
    {
        var book = db.Books.Find(id);
        if (book == null) return NotFound();

        if (field == "IsFeatured") book.IsFeatured = value;
        else if (field == "IsArchived") book.IsArchived = value;
        else return BadRequest("Invalid field");

        db.SaveChanges();
        return Ok();
    }

    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult ManageGenre(string? search, bool? hasBooks, int page = 1, string sort = "Name", string order = "asc")
    {
        var query = db.Genres.Include(g => g.Books).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(g => g.Name.Contains(search));
        }

        if (hasBooks.HasValue)
        {
            if (hasBooks.Value) query = query.Where(g => g.Books.Any());
            else query = query.Where(g => !g.Books.Any());
        }

        bool isDesc = order == "desc";
        query = sort switch
        {
            "Name" => isDesc ? query.OrderByDescending(g => g.Name) : query.OrderBy(g => g.Name),
            "Id" => isDesc ? query.OrderByDescending(g => g.Id) : query.OrderBy(g => g.Id),
            "Books" => isDesc ? query.OrderByDescending(g => g.Books.Count) : query.OrderBy(g => g.Books.Count),
            _ => isDesc ? query.OrderByDescending(g => g.Name) : query.OrderBy(g => g.Name)
        };

        int pageSize = 10;
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.TotalItems = db.Genres.Count();
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new GenreManagementVM
        {
            ExistingGenres = items,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchTerm = search,
            SortColumn = sort,
            SortOrder = order,
            FilterHasBooks = hasBooks
        };

        if (Request.IsAjax())
        {
            return PartialView("_TableGenre", vm);
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult SaveGenre(GenreManagementVM vm)
    {
        // 1. Normalize Input
        string cleanName = vm.Form.Name.Trim();

        // 2. Check for Duplicates
        bool isDuplicate = db.Genres.Any(g => g.Name.ToLower() == cleanName.ToLower() && g.Id != vm.Form.Id);
        if (isDuplicate)
        {
            ModelState.AddModelError("Form.Name", "This Genre already exists.");
        }

        // 3. Early Exit
        if (!ModelState.IsValid)
        {
            vm.ExistingGenres = db.Genres.OrderBy(g => g.Name).ToList();
            TempData["ShowModal"] = true;
            return View("ManageGenre", vm);
        }

        // 4. Generate ID
        if (string.IsNullOrEmpty(vm.Form.Id))
        {
            var lastId = db.Genres.OrderByDescending(g => g.Id).Select(g => g.Id).FirstOrDefault();
            var newGenre = new Genre
            {
                Id = Helper.GenerateNewId(lastId, 'G'),
                Name = cleanName,
            };
            db.Genres.Add(newGenre);
            TempData["ToastMessage"] = $"Genre '{vm.Form.Name}' added successfully.";
        }
        else
        {
            var genre = db.Genres.Find(vm.Form.Id);
            if (genre == null) return NotFound();

            genre.Name = cleanName;
            TempData["ToastMessage"] = $"Genre '{vm.Form.Name}' updated successfully.";
        }

        // 5. Save Changes
        db.SaveChanges();
        TempData["ToastType"] = "success";

        return RedirectToAction("ManageGenre");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult DeleteGenre(string id)
    {
        // 1. Fetch Genres with all their Books
        var genre = db.Genres
                      .Include(g => g.Books)
                      .FirstOrDefault(g => g.Id == id);

        if (genre != null)
        {
            // 2. Check if any books are linked to the genre
            if (genre.Books.Count > 0)
            {
                TempData["ToastMessage"] = $"Cannot delete '{genre.Name}' because it is used by {genre.Books.Count} book(s).";
                TempData["ToastType"] = "error";
            }
            else
            {
                // 3. No books linked, safe to delete
                db.Genres.Remove(genre);
                db.SaveChanges();
                TempData["ToastMessage"] = $"Genre '{genre.Name}' deleted successfully.";
                TempData["ToastType"] = "success";
            }
        }
        else
        {
            TempData["ToastMessage"] = "Genre not found.";
            TempData["ToastType"] = "error";
        }

        return RedirectToAction("ManageGenre");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult BatchDeleteGenre(List<string> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            TempData["ToastMessage"] = "No items selected.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("ManageGenre");
        }

        // 1. Fetch all selected items with their books
        var targets = db.Genres
                        .Include(g => g.Books)
                        .Where(g => ids.Contains(g.Id))
                        .ToList();

        // 2. Separate safe and unsafe
        var safeToDelete = targets.Where(g => g.Books.Count == 0).ToList();
        var unsafeItems = targets.Where(g => g.Books.Count > 0).ToList();

        // 3. Delete safe items
        if (safeToDelete.Count > 0)
        {
            db.Genres.RemoveRange(safeToDelete);
            db.SaveChanges();
        }

        // 4. Generate feedback
        if (unsafeItems.Count > 0)
        {
            TempData["ToastMessage"] = $"Deleted {safeToDelete.Count} genre(s). Skipped {unsafeItems.Count} genre(s) because they are used by books.";
            TempData["ToastType"] = "warning";
        }
        else
        {
            TempData["ToastMessage"] = $"Successfully deleted {safeToDelete.Count} genre(s).";
            TempData["ToastType"] = "success";
        }

        return RedirectToAction("ManageGenre");
    }

    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult ManageAuthor(string? search, bool? hasBooks, int page = 1, string sort = "Name", string order = "asc")
    {
        var query = db.Authors.Include(a => a.Books).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(a => a.Name.Contains(search));
        }

        if (hasBooks.HasValue)
        {
            if (hasBooks.Value) query = query.Where(a => a.Books.Any());
            else query = query.Where(a => !a.Books.Any());
        }

        bool isDesc = order == "desc";
        query = sort switch
        {
            "Name" => isDesc ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
            "Id" => isDesc ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id),
            "Books" => isDesc ? query.OrderByDescending(a => a.Books.Count) : query.OrderBy(a => a.Books.Count),
            _ => isDesc ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name)
        };

        int pageSize = 10;
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        ViewBag.TotalItems = db.Authors.Count();
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new AuthorManagementVM
        {
            ExistingAuthors = items,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchTerm = search,
            SortColumn = sort,
            SortOrder = order,
            FilterHasBooks = hasBooks
        };

        if (Request.IsAjax())
        {
            return PartialView("_TableAuthor", vm);
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult SaveAuthor(AuthorManagementVM vm)
    {
        // 1. Normalize Input
        string cleanName = vm.Form.Name.Trim();

        // 2. Check for Duplicates
        bool isDuplicate = db.Authors.Any(a => a.Name.ToLower() == cleanName.ToLower() && a.Id != vm.Form.Id);
        if (isDuplicate)
        {
            ModelState.AddModelError("Form.Name", "This Author already exists.");
        }

        // 3. Early Exit
        if (!ModelState.IsValid)
        {
            vm.ExistingAuthors = db.Authors.OrderBy(a => a.Name).ToList();
            TempData["ShowModal"] = true;
            return View("ManageAuthor", vm);
        }

        // 4. Generate ID
        if (string.IsNullOrEmpty(vm.Form.Id))
        {
            var lastId = db.Authors.OrderByDescending(a => a.Id).Select(a => a.Id).FirstOrDefault();
            var newAuthor = new Author
            {
                Id = Helper.GenerateNewId(lastId, 'A'),
                Name = cleanName,
            };
            db.Authors.Add(newAuthor);
            TempData["ToastMessage"] = $"Author '{vm.Form.Name}' added successfully.";
        }
        else
        {
            var author = db.Authors.Find(vm.Form.Id);
            if (author == null) return NotFound();

            author.Name = cleanName;
            TempData["ToastMessage"] = $"Author '{vm.Form.Name}' updated successfully.";
        }

        // 5. Save Changes
        db.SaveChanges();
        TempData["ToastType"] = "success";

        return RedirectToAction("ManageAuthor");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult DeleteAuthor(string id)
    {
        // 1. Fetch Authors with all their Books
        var author = db.Authors
                      .Include(a => a.Books)
                      .FirstOrDefault(a => a.Id == id);

        if (author != null)
        {
            // 2. Check if any books are linked to the genre
            if (author.Books.Count > 0)
            {
                TempData["ToastMessage"] = $"Cannot delete '{author.Name}' because it is used by {author.Books.Count} book(s).";
                TempData["ToastType"] = "error";
            }
            else
            {
                // 3. No books linked, safe to delete
                db.Authors.Remove(author);
                db.SaveChanges();
                TempData["ToastMessage"] = $"Author '{author.Name}' deleted successfully.";
                TempData["ToastType"] = "success";
            }
        }
        else
        {
            TempData["ToastMessage"] = "Author not found.";
            TempData["ToastType"] = "error";
        }

        return RedirectToAction("ManageAuthor");
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public IActionResult BatchDeleteAuthor(List<string> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            TempData["ToastMessage"] = "No items selected.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("ManageGenre");
        }

        // 1. Fetch all selected items with their books
        var targets = db.Authors
                        .Include(a => a.Books)
                        .Where(a => ids.Contains(a.Id))
                        .ToList();

        // 2. Separate safe and unsafe
        var safeToDelete = targets.Where(a => a.Books.Count == 0).ToList();
        var unsafeItems = targets.Where(a => a.Books.Count > 0).ToList();

        // 3. Delete safe items
        if (safeToDelete.Count > 0)
        {
            db.Authors.RemoveRange(safeToDelete);
            db.SaveChanges();
        }

        // 4. Generate feedback
        if (unsafeItems.Count > 0)
        {
            TempData["ToastMessage"] = $"Deleted {safeToDelete.Count} author(s). Skipped {unsafeItems.Count} author(s) because they are used by books.";
            TempData["ToastType"] = "warning";
        }
        else
        {
            TempData["ToastMessage"] = $"Successfully deleted {safeToDelete.Count} author(s).";
            TempData["ToastType"] = "success";
        }

        return RedirectToAction("ManageGenre");
    }

    private void PopulateDropdowns(BookInsertVM vm)
    {
        vm.GenreList = db.Genres.OrderBy(g => g.Name).Select(g => new SelectListItem(g.Name, g.Id)).ToList();
        vm.AuthorList = db.Authors.OrderBy(a => a.Name).Select(a => new SelectListItem(a.Name, a.Id)).ToList();
    }

    private void PopulateDropdowns(BookEditVM vm)
    {
        vm.GenreList = db.Genres.OrderBy(g => g.Name).Select(g => new SelectListItem(g.Name, g.Id)).ToList();
        vm.AuthorList = db.Authors.OrderBy(a => a.Name).Select(a => new SelectListItem(a.Name, a.Id)).ToList();
    }

    private void DeleteImage(string? imageUrl)
    {
        if (!string.IsNullOrEmpty(imageUrl))
        {
            string imagePath = System.IO.Path.Combine(en.WebRootPath, "images", "books", imageUrl);
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }
    }

    private string SaveCroppedImage(IFormFile image, int x, int y, int width, int height, string webRootPath)
    {
        string filename = Guid.NewGuid().ToString("N") + ".jpg";
        string folderPath = Path.Combine(webRootPath, "images", "books");
        string fullPath = Path.Combine(folderPath, filename);

        Directory.CreateDirectory(folderPath);

        using var stream = image.OpenReadStream();
        using var img = Image.Load(stream);

        // Ensure crop rectangle is within bounds
        var cropRect = new Rectangle(x, y, width, height);
        
        img.Mutate(ctx => ctx.Crop(cropRect));
        img.Save(fullPath);

        return filename;
    }

    [HttpPost]
    [Authorize]
    public IActionResult SubmitReview(string loanId, int rating, string? comment)
    {
        var username = User.Identity?.Name;
        var user = db.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) return Json(new { success = false, message = "User not found." });

        var loan = db.Loans
            .Include(l => l.Review)
            .Include(l => l.Copy)
            .FirstOrDefault(l => l.Id == loanId && l.UserId == user.Id);

        if (loan == null) return Json(new { success = false, message = "Loan record not found." });
        if (loan.Status != Loan.LoanStatus.RETURNED) return Json(new { success = false, message = "You can only review returned books." });

        if (rating < 1 || rating > 5) return Json(new { success = false, message = "Please provide a rating." });

        if (loan.Review != null)
        {
            loan.Review.Rating = rating;
            loan.Review.Comment = comment?.Trim();
            loan.Review.ReviewDate = DateTime.Now;
        }
        else
        {
            var existingReview = db.Reviews
                .Include(r => r.Loan)
                .ThenInclude(l => l.Copy)
                .FirstOrDefault(r => r.Loan.UserId == user.Id && r.Loan.Copy.BookId == loan.Copy.BookId);

            if (existingReview != null)
            {
                existingReview.Rating = rating;
                existingReview.Comment = comment?.Trim();
                existingReview.ReviewDate = DateTime.Now;
            }
            else
            {
                var lastId = db.Reviews.OrderByDescending(r => r.Id).Select(r => r.Id).FirstOrDefault();
                var review = new Review
                {
                    Id = Helper.GenerateNewId(lastId, 'R'),
                    LoanId = loanId,
                    Rating = rating,
                    Comment = comment?.Trim(),
                    ReviewDate = DateTime.Now
                };
                db.Reviews.Add(review);
            }
        }
        
        db.SaveChanges();

        return Json(new { success = true, message = "Review submitted successfully." });
    }

    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult ManageReviews(string? search, int? rating, DateOnly? dateFrom, DateOnly? dateTo, int page = 1, string sort = "Date", string order = "desc")
    {
        var query = db.Reviews
            .Include(r => r.Loan)
            .ThenInclude(l => l.User)
            .Include(r => r.Loan)
            .ThenInclude(l => l.Copy)
            .ThenInclude(c => c.Book)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(r => r.Loan.User.Username.Contains(search) || 
                                     r.Loan.Copy.Book.Title.Contains(search) || 
                                     r.Comment.Contains(search));
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(r => DateOnly.FromDateTime(r.ReviewDate) >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(r => DateOnly.FromDateTime(r.ReviewDate) <= dateTo.Value);
        }

        bool isDesc = order == "desc";
        query = sort switch
        {
            "Date" => isDesc ? query.OrderByDescending(r => r.ReviewDate) : query.OrderBy(r => r.ReviewDate),
            "Rating" => isDesc ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
            "User" => isDesc ? query.OrderByDescending(r => r.Loan.User.Username) : query.OrderBy(r => r.Loan.User.Username),
            "Book" => isDesc ? query.OrderByDescending(r => r.Loan.Copy.Book.Title) : query.OrderBy(r => r.Loan.Copy.Book.Title),
            _ => isDesc ? query.OrderByDescending(r => r.ReviewDate) : query.OrderBy(r => r.ReviewDate)
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

        var allDates = db.Reviews.Select(r => r.ReviewDate);
        ViewBag.MinDate = allDates.Any() ? allDates.Min().ToString("yyyy-MM-dd") : null;
        ViewBag.MaxDate = allDates.Any() ? allDates.Max().ToString("yyyy-MM-dd") : null;

        var reviews = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new ReviewManagementVM
        {
            Reviews = reviews,
            CurrentPage = page,
            TotalPages = totalPages,
            SearchTerm = search,
            SortColumn = sort,
            SortOrder = order,
            FilterRating = rating,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        if (Request.IsAjax())
        {
            return PartialView("_TableReview", vm);
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult DeleteReview(string id, string reason)
    {
        var review = db.Reviews
            .Include(r => r.Loan)
            .ThenInclude(l => l.Copy)
            .ThenInclude(c => c.Book)
            .FirstOrDefault(r => r.Id == id);

        if (review == null)
        {
            TempData["ToastMessage"] = "Review not found.";
            TempData["ToastType"] = "error";
            return RedirectToAction("ManageReviews");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ToastMessage"] = "A reason is required to delete a review.";
            TempData["ToastType"] = "warning";
            return RedirectToAction("ManageReviews");
        }

        // Send Notification
        var userId = review.Loan.UserId;
        var bookTitle = review.Loan.Copy.Book.Title;
        var message = $"Your review for '{bookTitle}' was deleted by a moderator. Reason: {reason}";
        
        ns.Notify(userId, message, "/Loan/Reviews");

        db.Reviews.Remove(review);
        db.SaveChanges();

        TempData["ToastMessage"] = "Review deleted and user notified.";
        TempData["ToastType"] = "success";

        return RedirectToAction("ManageReviews");
    }
}
