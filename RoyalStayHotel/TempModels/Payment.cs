using System;
using System.Collections.Generic;

namespace RoyalStayHotel.TempModels;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int UserId { get; set; }

    public int BookingId { get; set; }

    public decimal Amount { get; set; }

    public int Status { get; set; }

    public int PaymentStatus { get; set; }

    public int PaymentMethod { get; set; }

    public string? TransactionId { get; set; }

    public string? Notes { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? PaymentDetails { get; set; }
}
