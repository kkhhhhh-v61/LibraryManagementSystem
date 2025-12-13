using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

public class BookController : Controller
{
    public IActionResult Browse()
    {
        return View();
    }

    public IActionResult InsertBook()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Insert(BookInsertVM vm)
    {
        return View(vm);
    }

    public IActionResult InsertAuthor()
    {
        return View();
    }

    public IActionResult InsertGenre()
    {
        return View();
    }
}
