using Microsoft.EntityFrameworkCore;
using Support.Business.Api.Data.Entities;

namespace Support.Business.Api.Data;

public sealed class SupportDbContext(DbContextOptions<SupportDbContext> options) : DbContext(options)
{
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.ToTable("Tickets");

            entity.HasKey(ticket => ticket.Id);

            entity.Property(ticket => ticket.CustomerId)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(ticket => ticket.OrderId)
                .HasMaxLength(40);

            entity.Property(ticket => ticket.Subject)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(ticket => ticket.Description)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(ticket => ticket.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasIndex(ticket => ticket.CustomerId);
        });
    }
}
