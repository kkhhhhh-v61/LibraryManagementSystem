using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using System;

namespace LibraryManagementSystem.Controllers
{
    public class RoomController(LMSDB db, IWebHostEnvironment en) : Controller
    {
        // GET: Room/Index
        [Authorize(Roles = "MEMBER")]
        public IActionResult Index(string search = "", string sortOrder = "")
        {
            ViewData["CurrentFilter"] = search;
            ViewData["CurrentSort"] = sortOrder;

            ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "id_asc" : "";
            ViewData["NameSortParm"] = sortOrder == "Name" ? "name_desc" : "Name";
            ViewData["RateSortParm"] = sortOrder == "Rate" ? "rate_desc" : "Rate";

            var query = db.RoomCategories.Include(c => c.Rooms).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(c => c.Name.Contains(search) || c.Description.Contains(search));
            }

            switch (sortOrder)
            {
                case "id_asc":
                    query = query.OrderBy(c => c.Id);
                    break;
                case "Name":
                    query = query.OrderBy(c => c.Name);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(c => c.Name);
                    break;
                case "Rate":
                    query = query.OrderBy(c => c.HourlyRate);
                    break;
                case "rate_desc":
                    query = query.OrderByDescending(c => c.HourlyRate);
                    break;
                default:
                    query = query.OrderByDescending(c => c.Id);
                    break;
            }

            var categories = query.ToList();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CategoryTable", categories);
            }

            var vm = new RoomIndexVM
            {
                Categories = categories
            };

            ViewBag.CategoryList = new SelectList(db.RoomCategories, "Id", "Name");

            return View(vm);
        }

        // =======================================================================================
        // 1. CREATE CATEGORY
        // =======================================================================================
        [HttpPost]
        //[Authorize(Roles = "MEMBER")]
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

            if (vm.CategoryInput.Image != null)
            {
                var validationResult = Helper.ValidateImage(vm.CategoryInput.Image);
                if (validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
                {
                    ModelState.AddModelError("CategoryInput.Image", validationResult.ErrorMessage!);
                }
            }

            if (ModelState.IsValid)
            {
                string newId = GenerateNextId("Category");
                string uniqueFileName = "";

                if (vm.CategoryInput.Image != null)
                {
                    uniqueFileName = Helper.SaveImage(vm.CategoryInput.Image, "categories", en.WebRootPath);
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

                ModelState.Clear();
                var emptyVm = new RoomIndexVM();
                ViewData["SuccessMessage"] = "Category created successfully!";
                return PartialView("_CreateCategoryForm", emptyVm);
            }

            return PartialView("_CreateCategoryForm", vm);
        }

        // ==========================================================================================
        // 2. CREATE ROOM
        // ==========================================================================================
        [HttpPost]
        //[Authorize(Roles = "STAFF")]
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

            ViewBag.CategoryList = new SelectList(db.RoomCategories, "Id", "Name", vm.RoomInput.RoomCategoryId);

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

                ModelState.Clear();
                var emptyVm = new RoomIndexVM();

                ViewBag.CategoryList = new SelectList(db.RoomCategories, "Id", "Name");
                ViewData["SuccessMessage"] = "Room added successfully!";

                return PartialView("_CreateRoomForm", emptyVm);
            }

            return PartialView("_CreateRoomForm", vm);
        }

        // Id auto generate
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

            if (vm.CategoryUpdate.Image != null)
            {
                var validationResult = Helper.ValidateImage(vm.CategoryUpdate.Image);
                if (validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
                {
                    ModelState.AddModelError("CategoryUpdate.Image", validationResult.ErrorMessage!);
                }
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
                        string newFileName = Helper.SaveImage(vm.CategoryUpdate.Image, "categories", en.WebRootPath);

                        if (!string.IsNullOrEmpty(existingCategory.ImageURL))
                        {
                            string oldPath = Path.Combine(en.WebRootPath, "images", "categories", existingCategory.ImageURL);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        existingCategory.ImageURL = newFileName;
                    }

                    db.SaveChanges();

                    TempData["Success"] = "Category updated successfully!";
                    return RedirectToAction("Edit", new { id = vm.CategoryUpdate.Id });
                }
            }

            TempData["Error"] = "Failed to update category.";

            var categoryId = vm.CategoryUpdate.Id;
            var freshData = db.RoomCategories.Include(c => c.Rooms).FirstOrDefault(c => c.Id == categoryId);
            if (freshData != null)
            {
                vm.CategoryDisplay = freshData;
                vm.CategoryUpdate.ExistingImageURL = freshData.ImageURL;
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