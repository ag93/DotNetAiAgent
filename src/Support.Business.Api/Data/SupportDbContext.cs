using Microsoft.EntityFrameworkCore;
using Support.Business.Api.Data.Entities;

namespace Support.Business.Api.Data;

public sealed class SupportDbContext(DbContextOptions<SupportDbContext> options) : DbContext(options)
{
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();
    public DbSet<TicketApprovalRequest> TicketApprovalRequests => Set<TicketApprovalRequest>();

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


        modelBuilder.Entity<TicketApprovalRequest>(entity =>
        {
            entity.ToTable("TicketApprovalRequests");

            entity.HasKey(approval => approval.Id);

            entity.Property(approval => approval.Reason)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(approval => approval.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(approval => approval.ReviewedBy)
                .HasMaxLength(200);

            entity.HasOne(approval => approval.Ticket)
                .WithMany()
                .HasForeignKey(approval => approval.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(approval => new
            {
                approval.TicketId,
                approval.Status
            });
        });

    }
}
