using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;

namespace LibraryManagementSystem.Controllers
{
    public class RoomController(LMSDB db, IWebHostEnvironment en) : Controller
    {
        // GET: Room/Index
        public IActionResult Index()
        {
            var vm = new RoomIndexVM
            {
                Categories = db.RoomCategories.Include(c => c.Rooms).ToList()
            };

            ViewBag.CategoryList = new SelectList(vm.Categories, "Id", "Name");

            return View(vm);
        }

        // =======================================================================================
        // 1. CREATE CATEGORY
        // =======================================================================================
        [HttpPost]
        public IActionResult Create(RoomIndexVM vm)
        {
            ModelState.Remove("RoomInput.Name");
            ModelState.Remove("RoomInput.Location");

            if (ModelState.IsValid)
            {
                string newId = GenerateNextId("Category");

                var newCategory = new RoomCategory
                {
                    Id = newId,
                    Name = vm.CategoryInput.Name,
                    HourlyRate = vm.CategoryInput.HourlyRate,
                    Description = vm.CategoryInput.Description
                };

                db.RoomCategories.Add(newCategory);
                db.SaveChanges();

                TempData["Success"] = "Category created successfully!";
                return RedirectToAction("Index");
            }
            vm.Categories = db.RoomCategories.Include(c => c.Rooms).ToList();
            ViewBag.CategoryList = new SelectList(vm.Categories, "Id", "Name");

            TempData["Error"] = "Please fix the errors below.";

            return View("Index", vm);
        }

        // ==========================================================================================
        // 2. CREATE ROOM
        // ==========================================================================================
        [HttpPost]
        public IActionResult CreateRoom(RoomIndexVM vm)
        {
            ModelState.Remove("CategoryInput.Name");
            ModelState.Remove("CategoryInput.Description");

            if (ModelState.IsValid)
            {
                string newId = GenerateNextId("Room");
                string uniqueFileName = "";

                // Handle image upload if user selected one
                if (vm.RoomInput.Image != null)
                {
                    string uploadFolder = Path.Combine(en.WebRootPath, "images", "rooms");
                    Directory.CreateDirectory(uploadFolder);
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + vm.RoomInput.Image.FileName;

                    using (var stream = new FileStream(Path.Combine(uploadFolder, uniqueFileName), FileMode.Create))
                    {
                        vm.RoomInput.Image.CopyTo(stream);
                    }
                }

                var newRoom = new Room
                {
                    Id = newId,
                    Name = vm.RoomInput.Name,
                    Location = vm.RoomInput.Location,
                    Capacity = vm.RoomInput.Capacity,
                    RoomCategoryId = vm.RoomInput.RoomCategoryId,
                    Status = vm.RoomInput.Status,
                    ImageURL = uniqueFileName
                };

                db.Rooms.Add(newRoom);
                db.SaveChanges();

                TempData["Success"] = "Room added!";
                return RedirectToAction("Index");
            }

            vm.Categories = db.RoomCategories.Include(c => c.Rooms).ToList();
            ViewBag.CategoryList = new SelectList(vm.Categories, "Id", "Name");

            TempData["Error"] = "Failed to add room.";
            return View("Index", vm);
        }

        private string GenerateNextId(string type)
        {
            string prefix = type == "Room" ? "R" : "C";
            string? lastId = null;

            if (type == "Room")
            {
                lastId = db.Rooms.OrderByDescending(r => r.Id).Select(r => r.Id).FirstOrDefault();
            }
            else
            {
                lastId = db.RoomCategories.OrderByDescending(c => c.Id).Select(c => c.Id).FirstOrDefault();
            }

            if (string.IsNullOrEmpty(lastId)) return prefix + "01";

            string numberPart = lastId.Substring(1);
            if (int.TryParse(numberPart, out int number))
            {
                return prefix + (number + 1).ToString("D2");
            }

            return prefix + "99";
        }

        public IActionResult Update(string id)
        {
            var category = db.RoomCategories.Include(c => c.Rooms).FirstOrDefault(c => c.Id == id);
            if (category == null) return NotFound();
            return View(category);
        }
    }
}
