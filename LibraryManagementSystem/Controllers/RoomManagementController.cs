using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    public class RoomManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
