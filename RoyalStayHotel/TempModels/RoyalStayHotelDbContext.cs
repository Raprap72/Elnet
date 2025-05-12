using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RoyalStayHotel.TempModels;

public partial class RoyalStayHotelDbContext : DbContext
{
    public RoyalStayHotelDbContext()
    {
    }

    public RoyalStayHotelDbContext(DbContextOptions<RoyalStayHotelDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Payment> Payments { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.BookingId, "IX_Payments_BookingId");

            entity.HasIndex(e => e.UserId, "IX_Payments_UserId");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
