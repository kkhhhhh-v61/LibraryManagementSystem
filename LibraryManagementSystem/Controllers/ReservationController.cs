using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers;

public class ReservationController : Controller
{
    private readonly LMSDB _context;

    public ReservationController(LMSDB context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // VIEW RESERVATION LIST
    // --------------------------------------------------
    /*
    public async Task<IActionResult> Index()
    {
        var reservations = await _context.Reservations
            .OrderBy(r => r.ReservationDate)
            .ToListAsync();

        return View(reservations);
    }
    */

    public IActionResult Index()
    {
        var reservations = new List<Reservation>
    {
        new Reservation
        {
            Id = "R001",
            UserId = "U001",
            BookId = "B001",
            Status = Reservation.ReservationStatus.PENDING,
            ReservationDate = DateTime.Now.AddDays(-2)
        },
        new Reservation
        {
            Id = "R002",
            UserId = "U002",
            BookId = "B002",
            Status = Reservation.ReservationStatus.READY,
            ReservationDate = DateTime.Now.AddDays(-1)
        }
    };

        return View(reservations);
    }


    // --------------------------------------------------
    // VIEW RESERVATION DETAILS
    // --------------------------------------------------
    public async Task<IActionResult> Details(string id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        int queuePosition = await _context.Reservations
            .Where(r => r.BookId == reservation.BookId &&
                        r.Status == Reservation.ReservationStatus.PENDING &&
                        r.ReservationDate < reservation.ReservationDate)
            .CountAsync() + 1;

        var vm = new ReservationVM
        {
            Id = reservation.Id,
            UserId = reservation.UserId,
            BookId = reservation.BookId,
            CopyId = reservation.CopyId,
            ReservationDate = reservation.ReservationDate,
            ReservationDue = reservation.ReservationDue,
            Status = reservation.Status,
        };

        return View(vm);
    }

    // --------------------------------------------------
    // CREATE RESERVATION (CHECK AVAILABILITY)
    // --------------------------------------------------
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(ReservationVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        bool hasAvailableCopy = await _context.Copies
            .AnyAsync(c => c.BookId == vm.BookId &&
                           c.Status == Copy.CopyStatus.AVAILABLE);

        if (hasAvailableCopy)
        {
            ModelState.AddModelError("", "Book is available. Please borrow directly.");
            return View(vm);
        }

        var reservation = new Reservation
        {
            Id = await GenerateReservationId(),   // ✅ AUTO INCREMENT STRING
            UserId = vm.UserId,
            BookId = vm.BookId,
            ReservationDate = DateTime.Now,
            Status = Reservation.ReservationStatus.PENDING
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // --------------------------------------------------
    // CANCEL RESERVATION
    // --------------------------------------------------
    public async Task<IActionResult> Cancel(string id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        reservation.Status = Reservation.ReservationStatus.CANCELLED;
        reservation.CopyId = null;

        await _context.SaveChangesAsync();

        await PromoteNextReservation(reservation.BookId);

        return RedirectToAction(nameof(Index));
    }

    // --------------------------------------------------
    // EXPIRE RESERVATION
    // --------------------------------------------------
    public async Task<IActionResult> Expire(string id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null) return NotFound();

        reservation.Status = Reservation.ReservationStatus.EXPIRED;
        reservation.CopyId = null;

        await _context.SaveChangesAsync();

        await PromoteNextReservation(reservation.BookId);

        return RedirectToAction(nameof(Index));
    }

    // --------------------------------------------------
    // AUTO-PROMOTION SYSTEM
    // --------------------------------------------------
    private async Task PromoteNextReservation(string bookId)
    {
        var nextReservation = await _context.Reservations
            .Where(r => r.BookId == bookId &&
                        r.Status == Reservation.ReservationStatus.PENDING)
            .OrderBy(r => r.ReservationDate)
            .FirstOrDefaultAsync();

        if (nextReservation == null)
            return;

        var copy = await _context.Copies
            .FirstOrDefaultAsync(c => c.BookId == bookId &&
                                      c.Status == Copy.CopyStatus.AVAILABLE);

        if (copy == null)
            return;

        copy.Status = Copy.CopyStatus.RESERVED;

        nextReservation.CopyId = copy.Id;
        nextReservation.Status = Reservation.ReservationStatus.READY;
        nextReservation.ReservationDue = DateTime.Now.AddDays(2);

        await _context.SaveChangesAsync();
    }
    private async Task<string> GenerateReservationId()
    {
        var lastId = await _context.Reservations
            .OrderByDescending(r => r.Id)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (lastId == null)
            return "R001";

        int number = int.Parse(lastId.Substring(1));
        return $"R{(number + 1):D3}";
    }
}
