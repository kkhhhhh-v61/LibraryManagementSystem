﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers
{
    [Authorize]
    public class LoanController(LMSDB db) : Controller
    {
        public IActionResult History()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string bookId)
        {
            var username = User.Identity?.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            if (user == null) return RedirectToAction("Login", "Account");

            // Find an available copy
            var copy = db.Copies.FirstOrDefault(c => c.BookId == bookId && c.Status == Copy.CopyStatus.AVAILABLE);

            if (copy == null)
            {
                TempData["ToastMessage"] = "Sorry, no copies are currently available.";
                TempData["ToastType"] = "error";
                return RedirectToAction("BookDetails", "Book", new { id = bookId });
            }

            // Create Loan
            var lastId = db.Loans.OrderByDescending(l => l.Id).Select(l => l.Id).FirstOrDefault();
            var loan = new Loan
            {
                Id = Helper.GenerateNewId(lastId, 'L'),
                UserId = user.Id,
                CopyId = copy.Id,
                LoanDate = DateOnly.FromDateTime(DateTime.Now),
                PickupDue = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
                LoanDue = DateOnly.FromDateTime(DateTime.Now.AddDays(14)),
                Status = Loan.LoanStatus.RESERVED
            };

            copy.Status = Copy.CopyStatus.RESERVED;
            db.Loans.Add(loan);
            db.SaveChanges();

            TempData["ToastMessage"] = "Book reserved successfully! Please pick it up within 3 days.";
            TempData["ToastType"] = "success";

            return RedirectToAction("BookDetails", "Book", new { id = bookId });
        }
    }
}
