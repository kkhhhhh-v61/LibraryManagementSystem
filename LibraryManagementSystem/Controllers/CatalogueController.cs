using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers;

public class CatalogueController : Controller
{
    public IActionResult Browse()
    {
        return View();
    }
}
