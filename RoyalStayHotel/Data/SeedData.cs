using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoyalStayHotel.Models;

namespace RoyalStayHotel.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Check if we already have contact form submissions
                if (context.ContactFormSubmissions.Any())
                {
                    return; // DB has been seeded
                }

                // Add sample contact form submissions
                context.ContactFormSubmissions.AddRange(
                    new ContactFormSubmission
                    {
                        Name = "John Doe",
                        Email = "john.doe@example.com",
                        PhoneNumber = "+1234567890",
                        Subject = "Reservation Inquiry",
                        Message = "I would like to inquire about availability for a Deluxe Room for 2 adults from June 15-20. What are the rates and is airport pickup available?",
                        SubmissionDate = DateTime.Now.AddDays(-5),
                        IsRead = true
                    },
                    new ContactFormSubmission
                    {
                        Name = "Jane Smith",
                        Email = "jane.smith@example.com",
                        PhoneNumber = "+9876543210",
                        Subject = "Special Request",
                        Message = "We are celebrating our anniversary and would like to request a room with a view. Also, is it possible to arrange for a surprise cake and champagne?",
                        SubmissionDate = DateTime.Now.AddDays(-2),
                        IsRead = false
                    },
                    new ContactFormSubmission
                    {
                        Name = "Robert Johnson",
                        Email = "robert.j@example.com",
                        PhoneNumber = "+2345678901",
                        Subject = "Feedback",
                        Message = "I recently stayed at your hotel and wanted to express my appreciation for the excellent service provided by your staff, particularly by Maria at the front desk who went above and beyond to make our stay memorable.",
                        SubmissionDate = DateTime.Now.AddHours(-12),
                        IsRead = false
                    }
                );

                context.SaveChanges();
            }
        }
    }
} 