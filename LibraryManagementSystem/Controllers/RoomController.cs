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
                Categories = db.RoomCategories.Include(c => c.Rooms).OrderByDescending(c => c.Id).ToList()
            };

            ViewBag.CategoryList = new SelectList(vm.Categories, "Id", "Name");

            return View(vm);
        }

        // =======================================================================================
        // 1. CREATE CATEGORY
        // =======================================================================================
        [HttpPost]
        public IActionResult CreateCategoryAjax(RoomIndexVM vm)
        {
            ModelState.Remove("RoomInput.Name");
            ModelState.Remove("RoomInput.Location");
            ModelState.Remove("RoomInput.Capacity");
            ModelState.Remove("RoomInput.RoomCategoryId");
            ModelState.Remove("RoomInput.Status");
            ModelState.Remove("RoomInput");

            bool nameExists = db.RoomCategories.Any(c => c.Name == vm.CategoryInput.Name.Trim());
            if (nameExists)
            {
                ModelState.AddModelError("CategoryInput.Name", "Category Name already exists.");
            }

            if (ModelState.IsValid)
            {
                string newId = GenerateNextId("Category");
                string uniqueFileName = "";

                if (vm.CategoryInput.Image != null)
                {
                    string uploadFolder = Path.Combine(en.WebRootPath, "images", "categories");
                    Directory.CreateDirectory(uploadFolder);
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + vm.CategoryInput.Image.FileName;
                    using (var stream = new FileStream(Path.Combine(uploadFolder, uniqueFileName), FileMode.Create))
                    {
                        vm.CategoryInput.Image.CopyTo(stream);
                    }
                }

                var newCategory = new RoomCategory
                {
                    Id = newId,
                    Name = vm.CategoryInput.Name.Trim(),
                    HourlyRate = vm.CategoryInput.HourlyRate,
                    Description = vm.CategoryInput.Description.Trim(),
                    ImageURL = uniqueFileName
                };

                db.RoomCategories.Add(newCategory);
                db.SaveChanges();


                return Json(new
                {
                    success = true,
                    message = "Category created successfully!",
                    data = new
                    {
                        id = newCategory.Id,
                        name = newCategory.Name,
                        rate = newCategory.HourlyRate.ToString("F2"),
                        description = newCategory.Description
                    }
                });
            }

            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

            return Json(new { success = false, message = "Please fix the errors below.", errors = errors });
        }

        // ==========================================================================================
        // 2. CREATE ROOM
        // ==========================================================================================
        [HttpPost]
        public IActionResult CreateRoomAjax(RoomIndexVM vm)
        {
            ModelState.Remove("CategoryInput.Name");
            ModelState.Remove("CategoryInput.Description");
            ModelState.Remove("CategoryInput.HourlyRate");
            ModelState.Remove("CategoryInput.Image");
            ModelState.Remove("CategoryInput");

            bool nameExists = db.Rooms.Any(r => r.RoomCategoryId == vm.RoomInput.RoomCategoryId && r.Name == vm.RoomInput.Name.Trim());
            if (nameExists)
            {
                ModelState.AddModelError("RoomInput.Name", "Room Name already exists in this category.");
            }

            if (ModelState.IsValid)
            {
                string newId = GenerateNextId("Room");

                var newRoom = new Room
                {
                    Id = newId,
                    Name = vm.RoomInput.Name.Trim(),
                    Location = vm.RoomInput.Location.Trim(),
                    Capacity = vm.RoomInput.Capacity,
                    RoomCategoryId = vm.RoomInput.RoomCategoryId,
                    Status = vm.RoomInput.Status
                };

                db.Rooms.Add(newRoom);
                db.SaveChanges();

                return Json(new { success = true, message = "Room added successfully!" });
            }

            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

            return Json(new { success = false, message = "Failed to add room.", errors = errors });
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
        // 3. Edit PAGE 
        // =========================================================
        public IActionResult Edit(string id)
        {
            var category = db.RoomCategories.Include(c => c.Rooms).FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction("Index");
            }

            category.Rooms = category.Rooms.OrderByDescending(r => r.Id).ToList();

            var vm = new RoomUpdatePageVM
            {
                CategoryDisplay = category,

                CategoryUpdate = new RoomCategoryUpdateVM
                {
                    Id = category.Id,
                    Name = category.Name,
                    HourlyRate = category.HourlyRate,
                    Description = category.Description,
                    ExistingImageURL = category.ImageURL
                }
            };

            return View(vm);
        }

        // =========================================================
        // 4. UPDATE CATEGORY 
        // =========================================================
        [HttpPost]
        public IActionResult Edit(RoomUpdatePageVM vm)
        {
            ModelState.Remove("CategoryDisplay.Name");
            ModelState.Remove("CategoryDisplay.Description");
            ModelState.Remove("CategoryDisplay.HourlyRate");
            ModelState.Remove("CategoryDisplay.Id");
            ModelState.Remove("CategoryDisplay");

            ModelState.Remove("CategoryUpdate.Image");

            ModelState.Remove("RoomUpdate.RoomId");
            ModelState.Remove("RoomUpdate.Name");
            ModelState.Remove("RoomUpdate.Location");
            ModelState.Remove("RoomUpdate.Capacity");
            ModelState.Remove("RoomUpdate.Status");
            ModelState.Remove("RoomUpdate");

            bool nameExists = db.RoomCategories.Any(c => c.Name == vm.CategoryUpdate.Name.Trim() && c.Id != vm.CategoryUpdate.Id);

            if (nameExists)
            {
                ModelState.AddModelError("CategoryUpdate.Name", "Category Name already exists.");
            }

            if (ModelState.IsValid)
            {
                var existingCategory = db.RoomCategories.FirstOrDefault(c => c.Id == vm.CategoryUpdate.Id);

                if (existingCategory != null)
                {
                    existingCategory.Name = vm.CategoryUpdate.Name.Trim();
                    existingCategory.HourlyRate = vm.CategoryUpdate.HourlyRate;
                    existingCategory.Description = vm.CategoryUpdate.Description.Trim();

                    if (vm.CategoryUpdate.Image != null)
                    {
                        string uploadFolder = Path.Combine(en.WebRootPath, "images", "categories");
                        Directory.CreateDirectory(uploadFolder);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + vm.CategoryUpdate.Image.FileName;

                        using (var stream = new FileStream(Path.Combine(uploadFolder, uniqueFileName), FileMode.Create))
                        {
                            vm.CategoryUpdate.Image.CopyTo(stream);
                        }

                        existingCategory.ImageURL = uniqueFileName;
                    }

                    db.SaveChanges();

                    TempData["Success"] = "Category details updated!";
                    return RedirectToAction("Edit", new { id = vm.CategoryUpdate.Id });
                }
            }

            TempData["Error"] = "Failed to update category. Please check inputs.";

            var categoryId = vm.CategoryUpdate.Id;
            var freshData = db.RoomCategories.Include(c => c.Rooms).FirstOrDefault(c => c.Id == categoryId);

            if (freshData != null)
            {
                vm.CategoryDisplay = freshData;
            }

            return View("Edit", vm);
        }

        // =========================================================
        // 5. UPDATE ROOM DETAILS 
        // =========================================================
        [HttpPost]
        public IActionResult UpdateRoomDetails(RoomUpdatePageVM vm)
        {
            ModelState.Remove("CategoryUpdate.Id");
            ModelState.Remove("CategoryUpdate.Name");
            ModelState.Remove("CategoryUpdate.HourlyRate");
            ModelState.Remove("CategoryUpdate.Description");
            ModelState.Remove("CategoryUpdate");

            ModelState.Remove("CategoryDisplay.Name");
            ModelState.Remove("CategoryDisplay.Description");
            ModelState.Remove("CategoryDisplay.HourlyRate");
            ModelState.Remove("CategoryDisplay.Id");
            ModelState.Remove("CategoryDisplay");

            ModelState.Remove("CategoryUpdate.Image");

            var room = db.Rooms.FirstOrDefault(r => r.Id == vm.RoomUpdate.RoomId);

            if (room == null)
            {
                TempData["Error"] = "Room not found.";
                return RedirectToAction("Index");
            }

            bool nameExists = db.Rooms.Any(r => r.RoomCategoryId == room.RoomCategoryId && r.Name == vm.RoomUpdate.Name.Trim() && r.Id != vm.RoomUpdate.RoomId);

            if (nameExists)
            {
                ModelState.AddModelError("RoomUpdate.Name", "Room Name already exists in this category.");
            }

            if (ModelState.IsValid)
            {
                room.Name = vm.RoomUpdate.Name.Trim();
                room.Location = vm.RoomUpdate.Location.Trim();
                room.Capacity = vm.RoomUpdate.Capacity;
                room.Status = vm.RoomUpdate.Status;

                db.SaveChanges();

                TempData["Success"] = $"Room {vm.RoomUpdate.RoomId} updated successfully!";
                return RedirectToAction("Edit", new { id = room.RoomCategoryId });
            }

            TempData["Error"] = "Failed to update room. Please check inputs.";
            var categoryId = room.RoomCategoryId;
            var freshData = db.RoomCategories.Include(c => c.Rooms).FirstOrDefault(c => c.Id == categoryId);

            if (freshData != null)
            {
                vm.CategoryDisplay = freshData;

                vm.CategoryUpdate = new RoomCategoryUpdateVM
                {
                    Id = freshData.Id,
                    Name = freshData.Name,
                    HourlyRate = freshData.HourlyRate,
                    Description = freshData.Description
                };
            }

            ViewData["ActiveModal"] = "roomEditModal";

            return View("Edit", vm);
        }

        // =========================================================
        // 6. DELETE CATEGORY
        // =========================================================
        [HttpPost]
        public IActionResult DeleteCategory(string id)
        {
            var category = db.RoomCategories.Include(c => c.Rooms).FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction("Index");
            }

            if (category.Rooms.Any())
            {
                TempData["Error"] = $"Cannot delete '{category.Name}' because it still contains rooms. Please delete the rooms first.";
                return RedirectToAction("Index");
            }

            db.RoomCategories.Remove(category);
            db.SaveChanges();

            TempData["Success"] = "Category deleted successfully!";
            return RedirectToAction("Index");
        }

        // =========================================================
        // 7. DELETE ROOM 
        // =========================================================
        [HttpPost]
        public IActionResult DeleteRoom(string id)
        {
            var room = db.Rooms.FirstOrDefault(r => r.Id == id);

            if (room == null)
            {
                TempData["Error"] = "Room not found.";
                return RedirectToAction("Index");
            }

            string returnId = room.RoomCategoryId;

            db.Rooms.Remove(room);
            db.SaveChanges();

            TempData["Success"] = "Room deleted successfully!";
            return RedirectToAction("Edit", new { id = returnId });
        }

        // =========================================================
        // 8. CREATE ROOM (FROM EDIT PAGE)
        // =========================================================
        [HttpPost]
        public IActionResult CreateRoomFromEdit(RoomUpdatePageVM vm)
        {
            ModelState.Remove("CategoryDisplay.Name");
            ModelState.Remove("CategoryDisplay.Description");
            ModelState.Remove("CategoryDisplay.HourlyRate");
            ModelState.Remove("CategoryDisplay.Id");
            ModelState.Remove("CategoryDisplay");

            ModelState.Remove("CategoryUpdate.Name");
            ModelState.Remove("CategoryUpdate.HourlyRate");
            ModelState.Remove("CategoryUpdate.Description");
            ModelState.Remove("CategoryUpdate.Image");
            ModelState.Remove("CategoryUpdate.ExistingImageURL");

            ModelState.Remove("RoomUpdate.RoomId");

            string categoryId = vm.CategoryUpdate.Id;

            bool nameExists = db.Rooms.Any(r => r.RoomCategoryId == categoryId && r.Name == vm.RoomUpdate.Name.Trim());

            if (nameExists)
            {
                ModelState.AddModelError("RoomUpdate.Name", "Room Name already exists in this category.");
            }

            if (ModelState.IsValid)
            {
                string newId = GenerateNextId("Room");

                var newRoom = new Room
                {
                    Id = newId,
                    Name = vm.RoomUpdate.Name.Trim(),
                    Location = vm.RoomUpdate.Location.Trim(),
                    Capacity = vm.RoomUpdate.Capacity,
                    RoomCategoryId = categoryId,
                    Status = vm.RoomUpdate.Status
                };

                db.Rooms.Add(newRoom);
                db.SaveChanges();

                TempData["Success"] = "Room added successfully!";
                return RedirectToAction("Edit", new { id = categoryId });
            }

            TempData["Error"] = "Failed to add room. Please check inputs.";

            var categoryData = db.RoomCategories.Include(c => c.Rooms).FirstOrDefault(c => c.Id == categoryId);

            if (categoryData != null)
            {
                vm.CategoryDisplay = categoryData;
                vm.CategoryDisplay.Rooms = vm.CategoryDisplay.Rooms.OrderByDescending(r => r.Id).ToList();
                vm.CategoryUpdate.Id = categoryData.Id;
                vm.CategoryUpdate.Name = categoryData.Name;
                vm.CategoryUpdate.Description = categoryData.Description;
                vm.CategoryUpdate.ExistingImageURL = categoryData.ImageURL;
            }

            ViewData["ActiveModal"] = "addRoomModal";

            return View("Edit", vm);
        }

    }
}