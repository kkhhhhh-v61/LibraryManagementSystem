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
            ModelState.Remove("RoomInput.Capacity");
            ModelState.Remove("RoomInput.RoomCategoryId");

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

        // =========================================================
        // 3. UPDATE PAGE 
        // =========================================================
        public IActionResult Update(string id)
        {
            var category = db.RoomCategories.Include(c => c.Rooms).FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction("Index");
            }

            return View(category);
        }

        // =========================================================
        // 4. UPDATE CATEGORY 
        // =========================================================
        [HttpPost]
        public IActionResult Edit(RoomCategory category)
        {
            if (ModelState.IsValid)
            {
                var existingCategory = db.RoomCategories.FirstOrDefault(c => c.Id == category.Id);

                if (existingCategory != null)
                {
                    existingCategory.Name = category.Name;
                    existingCategory.HourlyRate = category.HourlyRate;
                    existingCategory.Description = category.Description;

                    db.SaveChanges();

                    TempData["Success"] = "Category details updated!";

                    return RedirectToAction("Update", new { id = category.Id });
                }
            }

            TempData["Error"] = "Failed to update category.";

            category.Rooms = db.Rooms.Where(r => r.RoomCategoryId == category.Id).ToList();
            return View("Update", category);
        }

        // =========================================================
        // 5. UPDATE ROOM DETAILS 
        // =========================================================
        [HttpPost]
        public IActionResult UpdateRoomDetails(string roomId, string name, string location, int capacity, Room.RoomStatus status)
        {
            var room = db.Rooms.FirstOrDefault(r => r.Id == roomId);

            if (room != null)
            {
                room.Name = name;
                room.Location = location;
                room.Capacity = capacity;
                room.Status = status;

                db.SaveChanges();

                TempData["Success"] = $"Room {roomId} updated successfully!";

                return RedirectToAction("Update", new { id = room.RoomCategoryId });
            }

            TempData["Error"] = "Room not found or update failed.";
            return RedirectToAction("Index");
        }
    }
}
