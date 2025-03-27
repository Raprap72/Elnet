using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace RoyalStayHotel.Areas.Admin.Controllers
{
    [Area("Admin")]
    public abstract class AdminBaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            // Skip authentication check for Login and Logout actions
            if (context.ActionDescriptor.DisplayName != null && 
                (context.ActionDescriptor.DisplayName.Contains("Login") || 
                 context.ActionDescriptor.DisplayName.Contains("Logout")))
            {
                return;
            }
            
            // Check if admin is authenticated
            var adminId = context.HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                // Redirect to login if not authenticated
                context.Result = new RedirectToActionResult("Login", "Home", new { area = "Admin" });
            }
        }
    }
} 