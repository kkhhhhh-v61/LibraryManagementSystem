using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class BookController(LMSDB db, IWebHostEnvironment en) : Controller
{
    public IActionResult Browse()
    {
        return View();
    }

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

        return RedirectToAction("Browse"); // TODO: Redirect to Book Maintenance
    }

    public IActionResult ManageGenre(string sortOrder)
    {
        ViewData["CurrentSort"] = sortOrder;
        ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewData["IdSortParm"] = sortOrder == "id" ? "id_desc" : "id";
        ViewData["CountSortParm"] = sortOrder == "count" ? "count_desc" : "count";

        var genres = db.Genres.Include(g => g.Books).AsQueryable();

        switch (sortOrder)
        {
            case "name_desc":
                genres = genres.OrderByDescending(g => g.Name);
                break;
            case "id":
                genres = genres.OrderBy(g => g.Id);
                break;
            case "id_desc":
                genres = genres.OrderByDescending(g => g.Id);
                break;
            case "count":
                genres = genres.OrderBy(g => g.Books.Count);
                break;
            case "count_desc":
                genres = genres.OrderByDescending(g => g.Books.Count);
                break;
            default:
                genres = genres.OrderBy(g => g.Name);
                break;
        }

        var vm = new GenreManagementVM
        {
            ExistingGenres = genres.ToList(),
        };

        return View(vm);
    }

    [HttpPost]
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

    public IActionResult ManageAuthor(string sortOrder)
    {
        ViewData["CurrentSort"] = sortOrder;
        ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewData["IdSortParm"] = sortOrder == "id" ? "id_desc" : "id";
        ViewData["CountSortParm"] = sortOrder == "count" ? "count_desc" : "count";

        var authors = db.Authors.Include(a => a.Books).AsQueryable();

        switch (sortOrder)
        {
            case "name_desc":
                authors = authors.OrderByDescending(a => a.Name);
                break;
            case "id":
                authors = authors.OrderBy(a => a.Id);
                break;
            case "id_desc":
                authors = authors.OrderByDescending(a => a.Id);
                break;
            case "count":
                authors = authors.OrderBy(a => a.Books.Count);
                break;
            case "count_desc":
                authors = authors.OrderByDescending(a => a.Books.Count);
                break;
            default:
                authors = authors.OrderBy(a => a.Name);
                break;
        }

        var vm = new AuthorManagementVM
        {
            ExistingAuthors = authors.ToList(),
        };
        
        return View(vm);
    }

    [HttpPost]
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
        vm.GenreList = db.Genres.Select(g => new SelectListItem(g.Name, g.Id)).ToList();
        vm.AuthorList = db.Authors.Select(a => new SelectListItem(a.Name, a.Id)).ToList();
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
