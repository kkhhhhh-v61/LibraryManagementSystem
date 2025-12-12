using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult StaffDashboard(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;

            ViewBag.SelectedDate = selectedDate;

            return View();
        }
    }
}
