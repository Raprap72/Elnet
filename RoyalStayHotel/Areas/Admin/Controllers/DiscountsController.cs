using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RoyalStayHotel.Data;
using RoyalStayHotel.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DiscountsController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public DiscountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Discounts
        public async Task<IActionResult> Index(string searchString, string filterType, bool? filterActive)
        {
            var discounts = _context.Discounts
                .Include(d => d.AppliedBookings)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                discounts = discounts.Where(d => 
                    d.Name.Contains(searchString) ||
                    d.Description.Contains(searchString) ||
                    d.DiscountCode.Contains(searchString));
            }

            // Apply type filter
            if (!string.IsNullOrEmpty(filterType) && Enum.TryParse<DiscountType>(filterType, out var type))
            {
                discounts = discounts.Where(d => d.Type == type);
            }

            // Apply active filter
            if (filterActive.HasValue)
            {
                discounts = discounts.Where(d => d.IsActive == filterActive.Value);
            }

            // Populate the ViewBag with filter options
            ViewBag.Types = Enum.GetValues(typeof(DiscountType))
                .Cast<DiscountType>()
                .Select(t => new SelectListItem
                {
                    Value = t.ToString(),
                    Text = t.ToString(),
                    Selected = t.ToString() == filterType
                });

            // Get stats for dashboard
            ViewBag.ActiveDiscounts = await _context.Discounts.CountAsync(d => d.IsActive);
            ViewBag.ExpiredDiscounts = await _context.Discounts.CountAsync(d => d.EndDate < DateTime.Now);
            ViewBag.UpcomingDiscounts = await _context.Discounts.CountAsync(d => d.StartDate > DateTime.Now);
            ViewBag.TotalDiscounts = await _context.Discounts.CountAsync();

            // Set active filter selection
            ViewBag.FilterActive = filterActive;

            // Sort by active status and end date
            discounts = discounts.OrderByDescending(d => d.IsActive).ThenBy(d => d.EndDate);

            return View(await discounts.ToListAsync());
        }

        // GET: Admin/Discounts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discount = await _context.Discounts
                .Include(d => d.AppliedBookings)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(m => m.DiscountId == id);

            if (discount == null)
            {
                return NotFound();
            }

            return View(discount);
        }

        // GET: Admin/Discounts/Create
        public IActionResult Create()
        {
            // Populate discount type dropdown
            ViewBag.DiscountTypes = new SelectList(Enum.GetValues(typeof(DiscountType)));
            
            // Set default dates
            ViewBag.StartDate = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.EndDate = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd");
            
            // Populate room type dropdown for room-specific discounts
            ViewBag.RoomTypes = new SelectList(Enum.GetValues(typeof(RoomType)));
            
            return View();
        }

        // POST: Admin/Discounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Type,DiscountValue,IsPercentage,DiscountCode,StartDate,EndDate,MaxUses,MinimumBookingAmount,RoomTypeId,MinimumStayDays")] Discount discount)
        {
            if (ModelState.IsValid)
            {
                // Validate dates
                if (discount.StartDate >= discount.EndDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after start date");
                    ViewBag.DiscountTypes = new SelectList(Enum.GetValues(typeof(DiscountType)), discount.Type);
                    ViewBag.RoomTypes = new SelectList(Enum.GetValues(typeof(RoomType)), discount.RoomTypeId);
                    return View(discount);
                }
                
                // Validate discount code uniqueness if provided
                if (!string.IsNullOrEmpty(discount.DiscountCode) && 
                    await _context.Discounts.AnyAsync(d => d.DiscountCode == discount.DiscountCode))
                {
                    ModelState.AddModelError("DiscountCode", "This discount code is already in use");
                    ViewBag.DiscountTypes = new SelectList(Enum.GetValues(typeof(DiscountType)), discount.Type);
                    ViewBag.RoomTypes = new SelectList(Enum.GetValues(typeof(RoomType)), discount.RoomTypeId);
                    return View(discount);
                }
                
                // Set active status based on start date
                discount.IsActive = discount.StartDate <= DateTime.Now && discount.EndDate > DateTime.Now;
                
                // Initialize usage count
                discount.UsageCount = 0;
                
                _context.Add(discount);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Discount created successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            // If model is invalid, repopulate dropdowns
            ViewBag.DiscountTypes = new SelectList(Enum.GetValues(typeof(DiscountType)), discount.Type);
            ViewBag.RoomTypes = new SelectList(Enum.GetValues(typeof(RoomType)), discount.RoomTypeId);
            
            return View(discount);
        }

        // GET: Admin/Discounts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }
            
            // Populate dropdowns
            ViewBag.DiscountTypes = new SelectList(Enum.GetValues(typeof(DiscountType)), discount.Type);
            ViewBag.RoomTypes = new SelectList(Enum.GetValues(typeof(RoomType)), discount.RoomTypeId);
            
            return View(discount);
        }

        // POST: Admin/Discounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DiscountId,Name,Description,Type,DiscountValue,IsPercentage,IsActive,DiscountCode,StartDate,EndDate,MaxUses,UsageCount,MinimumBookingAmount,RoomTypeId,MinimumStayDays")] Discount discount)
        {
            if (id != discount.DiscountId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate dates
                    if (discount.StartDate >= discount.EndDate)
                    {
                        ModelState.AddModelError("EndDate", "End date must be after start date");
                        ViewBag.DiscountTypes = new SelectList(Enum.GetValues(typeof(DiscountType)), discount.Type);
                        ViewBag.RoomTypes = new SelectList(Enum.GetValues(typeof(RoomType)), discount.RoomTypeId);
                        return View(discount);
                    }
                    
                    // Validate discount code uniqueness if changed
                    var originalDiscount = await _context.Discounts.AsNoTracking().FirstOrDefaultAsync(d => d.DiscountId == id);
                    if (!string.IsNullOrEmpty(discount.DiscountCode) && 
                        discount.DiscountCode != originalDiscount.DiscountCode &&
                        await _context.Discounts.AnyAsync(d => d.DiscountCode == discount.DiscountCode))
                    {
                        ModelState.AddModelError("DiscountCode", "This discount code is already in use");
                        ViewBag.DiscountTypes = new SelectList(Enum.GetValues(typeof(DiscountType)), discount.Type);
                        ViewBag.RoomTypes = new SelectList(Enum.GetValues(typeof(RoomType)), discount.RoomTypeId);
                        return View(discount);
                    }
                    
                    // Check if max uses has been reached
                    if (discount.MaxUses.HasValue && discount.UsageCount >= discount.MaxUses.Value)
                    {
                        discount.IsActive = false;
                    }
                    
                    // Check if promotion has expired
                    if (discount.EndDate < DateTime.Now)
                    {
                        discount.IsActive = false;
                    }
                    
                    _context.Update(discount);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Discount updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DiscountExists(discount.DiscountId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            // If model is invalid, repopulate dropdowns
            ViewBag.DiscountTypes = new SelectList(Enum.GetValues(typeof(DiscountType)), discount.Type);
            ViewBag.RoomTypes = new SelectList(Enum.GetValues(typeof(RoomType)), discount.RoomTypeId);
            
            return View(discount);
        }

        // GET: Admin/Discounts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discount = await _context.Discounts
                .FirstOrDefaultAsync(m => m.DiscountId == id);
                
            if (discount == null)
            {
                return NotFound();
            }

            // Check if discount has applied bookings
            bool hasAppliedBookings = await _context.Bookings.AnyAsync(b => b.AppliedDiscountId == id);
            ViewBag.HasAppliedBookings = hasAppliedBookings;

            return View(discount);
        }

        // POST: Admin/Discounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            
            // Check if discount has applied bookings before deleting
            bool hasAppliedBookings = await _context.Bookings.AnyAsync(b => b.AppliedDiscountId == id);
            
            if (hasAppliedBookings)
            {
                // Instead of deleting, mark as inactive
                discount.IsActive = false;
                await _context.SaveChangesAsync();
                
                TempData["WarningMessage"] = "Discount could not be deleted because it's used in bookings. It has been deactivated instead.";
            }
            else
            {
                _context.Discounts.Remove(discount);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Discount deleted successfully!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Discounts/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }
            
            // Check if discount can be activated
            if (!discount.IsActive)
            {
                // Check if it's expired
                if (discount.EndDate < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "Cannot activate a discount that has already expired.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Check if it's reached max uses
                if (discount.MaxUses.HasValue && discount.UsageCount >= discount.MaxUses.Value)
                {
                    TempData["ErrorMessage"] = "Cannot activate a discount that has reached its maximum usage.";
                    return RedirectToAction(nameof(Index));
                }
            }
            
            discount.IsActive = !discount.IsActive;
            await _context.SaveChangesAsync();
            
            string status = discount.IsActive ? "activated" : "deactivated";
            TempData["SuccessMessage"] = $"Discount {status} successfully!";
            
            return RedirectToAction(nameof(Index));
        }

        private bool DiscountExists(int id)
        {
            return _context.Discounts.Any(e => e.DiscountId == id);
        }
    }
} 