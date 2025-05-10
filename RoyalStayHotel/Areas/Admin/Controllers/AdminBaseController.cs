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

            // Temporarily disable authentication for demo purposes
            // All controllers will be accessible without login
            
            // Original authentication code:
            /*
            // Skip authentication for Login and Logout actions
            if (context.ActionDescriptor.DisplayName?.Contains("Account.Login") == true ||
                context.ActionDescriptor.DisplayName?.Contains("Account.Logout") == true)
            {
                return;
            }

            // Check if admin is authenticated
            var adminId = context.HttpContext.Session.GetInt32("AdminId");
            if (!adminId.HasValue)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "Admin" });
                return;
            }
            */
        }
    }
} 