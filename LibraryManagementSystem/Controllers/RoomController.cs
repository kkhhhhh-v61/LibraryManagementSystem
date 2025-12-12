using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    public class RoomController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Update(string id)
        {
            ViewBag.CategoryId = id;
            return View();
        }
    }
}
