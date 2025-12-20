using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    public class LoanController : Controller
    {
        public IActionResult History()
        {
            return View();
        }
    }
}
