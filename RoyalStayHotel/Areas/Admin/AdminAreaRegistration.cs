using Microsoft.AspNetCore.Mvc;

namespace RoyalStayHotel.Areas.Admin
{
    [Area("Admin")]
    public class AdminAreaRegistration : AreaAttribute
    {
        public AdminAreaRegistration() : base("Admin")
        {
        }
    }
} 