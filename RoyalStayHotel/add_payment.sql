-- Add payment for booking #16
INSERT INTO Payments (
    UserId,
    BookingId,
    Amount,
    Status,
    PaymentMethod,
    TransactionId,
    Notes,
    PaymentDate
)
VALUES (
    (SELECT UserId FROM Bookings WHERE BookingId = 16), -- UserId
    16, -- BookingId
    36692.00, -- Amount (from the booking total price)
    2, -- Status (2 = Completed in PaymentStatus enum)
    2, -- PaymentMethod (2 = GCash in PaymentMethod enum)
    'GC-123456789', -- TransactionId
    'Payment for Deluxe Suite Booking', -- Notes
    GETDATE() -- PaymentDate
);

-- Update booking status to Confirmed (BookingStatus.Confirmed = 1)
UPDATE Bookings
SET Status = 1
WHERE BookingId = 16; 