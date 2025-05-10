using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RoyalStayHotel.Models;

namespace RoyalStayHotel.Areas.Staff.Controllers
{
    [Area("Staff")]
    public class StaffBaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Skip authentication for login/logout actions
            if (context.ActionDescriptor.DisplayName?.Contains("Account.Login") == true ||
                context.ActionDescriptor.DisplayName?.Contains("Account.Logout") == true)
            {
                return;
            }

            // Check if staff is authenticated
            var staffId = HttpContext.Session.GetString("StaffId");
            if (string.IsNullOrEmpty(staffId))
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "Staff" });
                return;
            }

            // Check if the authenticated user is a staff member
            var userType = HttpContext.Session.GetString("UserType");
            if (!Enum.TryParse<UserType>(userType, out var type) || type != UserType.Staff)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "Staff" });
                return;
            }
        }

        protected int GetCurrentStaffId()
        {
            var staffId = HttpContext.Session.GetString("StaffId");
            return int.Parse(staffId ?? "0");
        }

        protected string GetCurrentStaffName()
        {
            return HttpContext.Session.GetString("StaffName") ?? "Unknown Staff";
        }
    }
} 