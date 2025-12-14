using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.X86;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
            GenreList = db.Genres.Select(g => new SelectListItem(g.Name, g.Id)).ToList(),
            AuthorList = db.Authors.Select(a => new SelectListItem(a.Name, a.Id)).ToList(),
            PublicationYear = DateTime.Now.Year,
            Edition = 1,
            IsFeatured = false,
            IsArchived = false,
        };
        return View(vm);
    }

    [HttpPost]
    public IActionResult InsertBook(BookInsertVM vm)
    {
        string? imageUrl = null;

        // 1. Check if ISBN already exists
        if (db.Books.Any(b => b.ISBN == vm.ISBN.Replace("-", "").Trim().Replace(" ", "").ToUpper()))
        {
            ModelState.AddModelError("ISBN", "This ISBN already exists.");
        }
        
        if (!ModelState.IsValid)
        {
            vm.GenreList = db.Genres.Select(g => new SelectListItem(g.Name, g.Id)).ToList();
            vm.AuthorList = db.Authors.Select(a => new SelectListItem(a.Name, a.Id)).ToList();
            return View(vm);
        }

        if (ModelState.IsValid)
        {
            bool isNewImageUpload = vm.Image != null;

            // If the primary image is null, reconstruct it from Base64 TempImage string.
            if (vm.Image == null && !string.IsNullOrEmpty(vm.TempImage))
            {
                try
                {
                    var base64Data = vm.TempImage.Split(',')[1];
                    byte[] imageBytes = Convert.FromBase64String(base64Data);
                    var stream = new MemoryStream(imageBytes);
                    vm.Image = new FormFile(stream, 0, imageBytes.Length, "image", "image.jpg");
                }
                catch (Exception)
                {
                    // Handle potential Base64 conversion errors if necessary
                }
            }

            // 2. Validate Image
            if (isNewImageUpload)
            {
                var result = Helper.ValidateImage(vm.Image!);

                if (result != ValidationResult.Success)
                {
                    ModelState.AddModelError("Image", result.ErrorMessage!);
                    vm.GenreList = db.Genres.Select(g => new SelectListItem(g.Name, g.Id)).ToList();
                    vm.AuthorList = db.Authors.Select(a => new SelectListItem(a.Name, a.Id)).ToList();
                    return View(vm);
                }

            }

            if (vm.Image != null)
            {
                imageUrl = Helper.SaveImage(vm.Image!, "books", en.WebRootPath);
            }

            // 3. Add book
            var lastId = db.Books.OrderByDescending(b => b.Id).Select(b => b.Id).FirstOrDefault();

            db.Books.Add(new()
            {
                // 3.1 Generate new ID
                Id = GenerateNewId(lastId, 'B'),
                Title = vm.Title.Trim(),
                Description = vm.Description.Trim(),
                ISBN = vm.ISBN.Replace("-", "").Trim().Replace(" ", "").ToUpper(),
                PublicationYear = vm.PublicationYear,
                Edition = vm.Edition,
                ImageURL = imageUrl,
                IsFeatured = vm.IsFeatured,
                IsArchived = vm.IsArchived,
                DateAdded = DateOnly.FromDateTime(DateTime.Now),
            });
            db.SaveChanges();

            var newBookId = db.Books.OrderByDescending(b => b.Id).Select(b => b.Id).FirstOrDefault();

            // 3.2 Add genres
            foreach (var genreId in vm.SelectedGenreIds)
            {
                var genre = db.Genres.Find(genreId);
                if (genre != null)
                {
                    db.Books.Find(newBookId)?.Genres.Add(genre);
                }
            }

            // 3.3 Add authors
            foreach (var authorId in vm.SelectedAuthorIds)
            {
                var author = db.Authors.Find(authorId);
                if (author != null)
                {
                    db.Books.Find(newBookId)?.Authors.Add(author);
                }
            }

            db.SaveChanges();

            // 4. Display toast messagee (success)
            TempData["ToastMessage"] = $"Book '{vm.Title}' added successfully!";
            TempData["ToastType"] = "success";
            TempData["SubmissionSuccess"] = true;

            return RedirectToAction("Browse");
        }

        // 4.1 Display toast message (error)
        TempData["ToastMessage"] = "Failed to add book. Please check errors.";
        TempData["ToastType"] = "error";

        return View(vm);
    }

    public IActionResult InsertGenre()
    {
        return View();
    }

    [HttpPost]
    public IActionResult InsertGenre(GenreVM vm)
    {
        // 1. Check if genre already exists
        if (db.Genres.Any(g => g.Name == vm.Name))
        {
            ModelState.AddModelError("Name", "This Genre already exists.");
        }

        if (ModelState.IsValid)
        {
            // 2. Add genre
            var lastId = db.Genres.OrderByDescending(g => g.Id).Select(g => g.Id).FirstOrDefault();

            db.Genres.Add(new()
            {
                // 2.1 Generate new ID
                Id = GenerateNewId(lastId, 'G'),
                Name = vm.Name.Trim(),
            });
            db.SaveChanges();

            // 3. Display toast messagee (success)
            TempData["ToastMessage"] = $"Genre '{vm.Name}' added successfully!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Browse");
        }

        // 3.1 Display toast message (error)
        TempData["ToastMessage"] = "Failed to add Genre. Please check errors.";
        TempData["ToastType"] = "error";

        return View(vm);
    }

    public IActionResult InsertAuthor()
    {
        return View();
    }

    [HttpPost]
    public IActionResult InsertAuthor(AuthorVM vm)
    {
        // 1. Check if author already exists
        if (db.Authors.Any(a => a.Name == vm.Name))
        {
            ModelState.AddModelError("Name", "This Author already exists.");
        }

        if (ModelState.IsValid)
        {
            // 2. Add author
            var lastId = db.Authors.OrderByDescending(a => a.Id).Select(a => a.Id).FirstOrDefault();

            db.Authors.Add(new()
            {
                // 2.1 Generate new ID
                Id = GenerateNewId(lastId, 'A'),
                Name = vm.Name.Trim(),
            });
            db.SaveChanges();

            // 3. Display toast messagee (success)
            TempData["ToastMessage"] = $"Author '{vm.Name}' added successfully!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Browse");
        }

        // 3.1 Display toast message (error)
        TempData["ToastMessage"] = "Failed to add Author. Please check errors.";
        TempData["ToastType"] = "error";

        return View(vm);
    }

    public static string GenerateNewId(string? lastId, char prefixChar)
    {
        // 1. Guard clause against bad data in DB
        if (lastId != null && lastId.Length != 6)
        {
            throw new InvalidOperationException($"Existing ID '{lastId}' is invalid. Expected 6 characters.");
        }

        // 2. Handle cases where database is empty
        if (string.IsNullOrEmpty(lastId)) return $"{prefixChar}0001A";

        // 3. Format and Extract
        lastId = lastId.ToUpper();
        prefixChar = char.ToUpper(prefixChar);
        char prefix  = lastId[0];
        string number = lastId.Substring(1, 4);
        char suffix = lastId[5];


        // 4. Check if ID prefix is same as passed prefix
        if (!lastId.StartsWith(prefixChar)) throw new ArgumentException($"The last ID '{lastId}' does not start with '{prefixChar}'.");

        // 4. Parse number to int
        int currentNumber = int.Parse(number);

        // 5. Check for rollover
        int newNumber;
        char newSuffix;

        if (currentNumber >= 9999)
        {
            newNumber = 1;
            newSuffix = (char)(suffix + 1);
        }
        else
        {
            newNumber = currentNumber + 1;
            newSuffix = suffix;
        }

        // 6. Return error if reach limit
        if (newSuffix > 'Z')
        {
            throw new InvalidOperationException($"Maximum ID limit reached ({prefix}9999Z). Cannot generate new ID.");
        }

        // 7. Return newly generated ID
        return $"{prefixChar}{newNumber:D4}{newSuffix}";
    }
}
