using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace RoyalStayHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddSampleDiscountsToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add sample discounts
            migrationBuilder.InsertData(
                table: "Discounts",
                columns: new[] { "DiscountId", "Name", "Code", "Description", "DiscountAmount", "IsPercentage", "StartDate", "EndDate", "IsActive", "MinimumStay", "MinimumSpend", "MaxUsage", "UsageCount", "Type", "RoomTypeId" },
                values: new object[] { 1, "Summer Special", "SUMMER15", "Get 15% off your stay during summer months.", 15m, true, DateTime.Now.AddDays(-30), DateTime.Now.AddDays(60), true, 2, 1000m, 50, 0, 3, null });

            migrationBuilder.InsertData(
                table: "Discounts",
                columns: new[] { "DiscountId", "Name", "Code", "Description", "DiscountAmount", "IsPercentage", "StartDate", "EndDate", "IsActive", "MinimumStay", "MinimumSpend", "MaxUsage", "UsageCount", "Type", "RoomTypeId" },
                values: new object[] { 2, "Weekend Getaway", "WEEKEND20", "20% discount on weekend bookings.", 20m, true, DateTime.Now.AddDays(-10), DateTime.Now.AddDays(80), true, 2, 2000m, 30, 0, 3, null });

            migrationBuilder.InsertData(
                table: "Discounts",
                columns: new[] { "DiscountId", "Name", "Code", "Description", "DiscountAmount", "IsPercentage", "StartDate", "EndDate", "IsActive", "MinimumStay", "MinimumSpend", "MaxUsage", "UsageCount", "Type", "RoomTypeId" },
                values: new object[] { 3, "Luxury Suite Deal", "LUXURY500", "₱500 off luxury suite bookings.", 500m, false, DateTime.Now.AddDays(-5), DateTime.Now.AddDays(45), true, 3, 5000m, null, 0, 0, 2 });

            migrationBuilder.InsertData(
                table: "Discounts",
                columns: new[] { "DiscountId", "Name", "Code", "Description", "DiscountAmount", "IsPercentage", "StartDate", "EndDate", "IsActive", "MinimumStay", "MinimumSpend", "MaxUsage", "UsageCount", "Type", "RoomTypeId" },
                values: new object[] { 4, "Early Bird Special", "EARLY10", "Book 30 days in advance and get 10% off.", 10m, true, DateTime.Now.AddDays(-60), DateTime.Now.AddDays(120), true, 1, null, 100, 0, 4, null });

            migrationBuilder.InsertData(
                table: "Discounts",
                columns: new[] { "DiscountId", "Name", "Code", "Description", "DiscountAmount", "IsPercentage", "StartDate", "EndDate", "IsActive", "MinimumStay", "MinimumSpend", "MaxUsage", "UsageCount", "Type", "RoomTypeId" },
                values: new object[] { 5, "Expired Offer", "EXPIRED25", "This offer has expired.", 25m, true, DateTime.Now.AddDays(-100), DateTime.Now.AddDays(-10), false, null, null, null, 0, 4, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove sample discounts
            migrationBuilder.DeleteData(
                table: "Discounts",
                keyColumn: "DiscountId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Discounts",
                keyColumn: "DiscountId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Discounts",
                keyColumn: "DiscountId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Discounts",
                keyColumn: "DiscountId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Discounts",
                keyColumn: "DiscountId",
                keyValue: 5);
        }
    }
}
