using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

public class CatalogueController : Controller
{
    public IActionResult Browse()
    {
        return View();
    }

    public IActionResult Detail()
    {
        return View();
    }

    public IActionResult Insert()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Insert(BookInsertVM vm)
    {
        return View(vm);
    }
}
