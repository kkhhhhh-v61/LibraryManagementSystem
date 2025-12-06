using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Models;

public class ViewModels : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
