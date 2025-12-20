using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Controllers;

public class ReservationController : Controller
{
    private readonly LMSDB _context;

    public ReservationController(LMSDB context)
    {
        _context = context;
    }

    // ==================================================
    // INDEX
    // ==================================================
    public async Task<IActionResult> Index()
    {
        var reservations = await _context.Reservations
            .OrderBy(r => r.ReservationDate)
            .ToListAsync();

        return View(reservations);
    }

    // ==================================================
    // DETAILS
    // ==================================================
    public async Task<IActionResult> Details(string id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        int queuePosition = 0;
        if (reservation.Status == Reservation.ReservationStatus.PENDING)
        {
            queuePosition = await _context.Reservations
                .Where(r => r.BookId == reservation.BookId &&
                            r.Status == Reservation.ReservationStatus.PENDING &&
                            r.ReservationDate < reservation.ReservationDate)
                .CountAsync() + 1;
        }

        var vm = new ReservationVM
        {
            Id = reservation.Id,
            UserId = reservation.UserId,
            BookId = reservation.BookId,
            CopyId = reservation.CopyId,
            ReservationDate = reservation.ReservationDate,
            ReservationDue = reservation.ReservationDue,
            Status = reservation.Status,
            QueuePosition = queuePosition
        };

        return View(vm);
    }

    // ==================================================
    // CREATE
    // ==================================================
    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // Book must exist
        if (!await _context.Books.AnyAsync(b => b.Id == vm.BookId))
        {
            ModelState.AddModelError("", "Book does not exist.");
            return View(vm);
        }

        // No duplicate active reservation
        if (await _context.Reservations.AnyAsync(r =>
            r.UserId == vm.UserId &&
            r.BookId == vm.BookId &&
            (r.Status == Reservation.ReservationStatus.PENDING ||
             r.Status == Reservation.ReservationStatus.READY)))
        {
            ModelState.AddModelError("", "You already have an active reservation.");
            return View(vm);
        }

        // Block if any copy is available
        if (await _context.Copies.AnyAsync(c =>
            c.BookId == vm.BookId &&
            c.Status == Copy.CopyStatus.AVAILABLE))
        {
            ModelState.AddModelError("", "Book is available. Please borrow directly.");
            return View(vm);
        }

        var reservation = new Reservation
        {
            Id = await GenerateReservationId(),
            UserId = vm.UserId,
            BookId = vm.BookId,
            ReservationDate = DateTime.Now,
            Status = Reservation.ReservationStatus.PENDING
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // ==================================================
    // BORROW (READY → LOAN)
    // ==================================================
    public async Task<IActionResult> Borrow(string id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Copy)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null ||
            reservation.Status != Reservation.ReservationStatus.READY ||
            reservation.Copy == null)
            return BadRequest();

        var loan = new Loan
        {
            Id = Guid.NewGuid().ToString("N")[..6].ToUpper(),
            UserId = reservation.UserId,
            CopyId = reservation.CopyId!,
            LoanDate = DateTime.Now,
            PickupDue = DateTime.Now.AddDays(1),
            LoanDue = DateTime.Now.AddDays(14),
            Status = Loan.LoanStatus.RESERVED
        };

        reservation.Copy.Status = Copy.CopyStatus.BORROWED;
        reservation.Status = Reservation.ReservationStatus.COMPLETED;

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Loan", new { id = loan.Id });
    }

    // ==================================================
    // CANCEL
    // ==================================================
    public async Task<IActionResult> Cancel(string id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Copy)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null) return NotFound();

        if (reservation.Copy != null)
            reservation.Copy.Status = Copy.CopyStatus.AVAILABLE;

        reservation.CopyId = null;
        reservation.Status = Reservation.ReservationStatus.CANCELLED;

        await _context.SaveChangesAsync();
        await PromoteNextReservation(reservation.BookId);

        return RedirectToAction(nameof(Index));
    }

    // ==================================================
    // EXPIRE
    // ==================================================
    public async Task<IActionResult> Expire(string id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Copy)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null) return NotFound();

        if (reservation.Copy != null)
            reservation.Copy.Status = Copy.CopyStatus.AVAILABLE;

        reservation.CopyId = null;
        reservation.Status = Reservation.ReservationStatus.EXPIRED;

        await _context.SaveChangesAsync();
        await PromoteNextReservation(reservation.BookId);

        return RedirectToAction(nameof(Index));
    }

    // ==================================================
    // AUTO-PROMOTION (FIFO)
    // ==================================================
    private async Task PromoteNextReservation(string bookId)
    {
        var next = await _context.Reservations
            .Where(r => r.BookId == bookId &&
                        r.Status == Reservation.ReservationStatus.PENDING)
            .OrderBy(r => r.ReservationDate)
            .FirstOrDefaultAsync();

        if (next == null) return;

        var copy = await _context.Copies
            .Where(c => c.BookId == bookId &&
                        c.Status == Copy.CopyStatus.AVAILABLE)
            .FirstOrDefaultAsync();

        if (copy == null) return;

        copy.Status = Copy.CopyStatus.RESERVED;
        next.CopyId = copy.Id;
        next.Status = Reservation.ReservationStatus.READY;
        next.ReservationDue = DateTime.Now.AddDays(2);

        await _context.SaveChangesAsync();
    }

    private async Task<string> GenerateReservationId()
    {
        var last = await _context.Reservations
            .OrderByDescending(r => r.Id)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        return last == null
            ? "R001"
            : $"R{(int.Parse(last[1..]) + 1):D3}";
    }
}
