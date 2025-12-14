using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.Controllers;

public class BookController(LMSDB db) : Controller
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
            SelectedGenreIds = [],
            AuthorList = db.Authors.Select(a => new SelectListItem(a.Name, a.Id)).ToList(),
            SelectedAuthorIds = [],
            PublicationYear = DateTime.Now.Year,
            Edition = 1,
            IsFeatured = false,
            IsArchived = false,
        };
        return View(vm);
    }

    [HttpPost]
    public IActionResult Insert(BookInsertVM vm)
    {
        // if (db.Books.Any(b => b.ISBN == vm.ISBN))
        // {
        //     ModelState.AddModelError("ISBN", "This ISBN already exists.");
        // }

        // if (ModelState.IsValid)
        // {
            
        // }

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
