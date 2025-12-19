using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace LibraryManagementSystem.Controllers;

public class BookController(LMSDB db, IWebHostEnvironment en) : Controller
{
    [Authorize(Roles = "ADMIN, STAFF")]
    public IActionResult ManageBook(string? search, string? authorId, string? genreId, int page = 1, string sort = "Title", string order = "asc")
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

        bool isDesc = order == "desc";
        query = sort switch
        {
            "Title" => isDesc ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
            _ => isDesc ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title)
        };

        int pageSize = 10;
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

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
            AuthorList = db.Authors.OrderBy(a => a.Name).Select(a => new SelectListItem(a.Name, a.Id)).ToList(),
            GenreList = db.Genres.OrderBy(g => g.Name).Select(g => new SelectListItem(g.Name, g.Id)).ToList()
        };

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
            imageUrl = Helper.SaveImage(vm.Image, "books", en.WebRootPath);
        }

        // 5. Generate ID
        var lastId = db.Books
                       .OrderByDescending(b => b.Id)
                       .Select(b => b.Id)
                       .FirstOrDefault();
        string newBookId = GenerateNewId(lastId, 'B');

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
    public IActionResult EditBook(string id)
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
            book.ImageURL = Helper.SaveImage(vm.Image, "books", en.WebRootPath);
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
    [Authorize(Roles = "ADMIN, STAFF")]
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
    [Authorize(Roles = "ADMIN, STAFF")]
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
    public IActionResult AddCopy(string bookId, int quantity)
    {
        if (quantity < 1)
        {
            TempData["ToastMessage"] = "Quantity must be at least 1.";
            TempData["ToastType"] = "error";
            return RedirectToAction("ManageBook");
        }

        var book = db.Books.Find(bookId);
        if (book == null)
        {
            TempData["ToastMessage"] = "Book not found.";
            TempData["ToastType"] = "error";
            return RedirectToAction("ManageBook");
        }

        var addedCopies = new List<object>();
        var lastCopy = db.Copies.OrderByDescending(c => c.Id).FirstOrDefault();
        string? lastId = lastCopy?.Id;

        for (int i = 0; i < quantity; i++)
        {
            string newId = GenerateNewId(lastId, 'C');
            lastId = newId;

            var copy = new Copy { Id = newId, BookId = bookId, Status = Copy.CopyStatus.AVAILABLE, Barcode = $"{bookId}-{newId}" };
            db.Copies.Add(copy);
            addedCopies.Add(new { copy.Id, copy.Barcode });
        }

        db.SaveChanges();

        TempData["ToastMessage"] = $"{quantity} copies added successfully.";
        TempData["ToastType"] = "success";
        TempData["AddedCopies"] = System.Text.Json.JsonSerializer.Serialize(addedCopies);

        return RedirectToAction("ManageBook");
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
    public IActionResult ManageGenre(string? search, int page = 1, string sort = "Name", string order = "asc")
    {
        var query = db.Genres.Include(g => g.Books).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(g => g.Name.Contains(search));
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
            SortOrder = order
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
                Id = GenerateNewId(lastId, 'G'),
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
    [Authorize(Roles = "ADMIN, STAFF")]
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
    [Authorize(Roles = "ADMIN, STAFF")]
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
    public IActionResult ManageAuthor(string? search, int page = 1, string sort = "Name", string order = "asc")
    {
        var query = db.Authors.Include(a => a.Books).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(a => a.Name.Contains(search));
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
            SortOrder = order
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
                Id = GenerateNewId(lastId, 'A'),
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
    [Authorize(Roles = "ADMIN, STAFF")]
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
    [Authorize(Roles = "ADMIN, STAFF")]
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

    private static string GenerateNewId(string? lastId, char prefixChar)
    {
        // 1. Normalize prefixChar
        char pChar = char.ToUpper(prefixChar);

        // 2. Empty table handling
        if (string.IsNullOrEmpty(lastId))
        {
            return $"{pChar}0001A";
        }

        // 3. Normalize lastId
        lastId = lastId.ToUpper();

        // 4. Validate Format
        if (lastId.Length != 6 || lastId[0] != pChar)
        {
            throw new InvalidOperationException($"Existing ID '{lastId}' is invalid. Expected format like '{pChar}0001A'.");
        }

        // 5. Parse Parts
        if (!int.TryParse(lastId.Substring(1, 4), out int currentNum))
        {
            throw new InvalidOperationException($"Existing ID '{lastId}' contains an invalid number section.");
        }
        char currentSuffix = lastId[5];

        // 6. Increment
        int nextNum = currentNum + 1;
        char nextSuffix = currentSuffix;
        if (nextNum > 9999)
        {
            nextNum = 1;
            nextSuffix++;
        }

        // 7. Overflow Guard
        if (nextSuffix > 'Z')
        {
            throw new InvalidOperationException($"ID capacity reached for prefix '{pChar}' (Max: {pChar}9999Z).");
        }

        // 8. Format Result
        return $"{pChar}{nextNum:D4}{nextSuffix}";
    }
}
