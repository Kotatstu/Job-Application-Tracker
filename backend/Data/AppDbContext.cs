using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<Company> Companies { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistory { get; set; }
    public DbSet<JobPostingDetails> JobPostingDetails { get; set; }
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MailboxConnection> MailboxConnections => Set<MailboxConnection>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<PendingJobApplication> PendingJobApplications => Set<PendingJobApplication>();
    public DbSet<PendingStatusChange> PendingStatusChanges => Set<PendingStatusChange>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(c => new { c.UserId, c.CompanyName }).IsUnique();
        });

        //A JobApplication has one company tied to it, restrict delete
        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.HasOne(ja => ja.Company)
            .WithMany()
            .HasForeignKey(ja => ja.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        //A ApplicationStatusHistroy has one JobApplication tied to it, will get deleted along with JA
        modelBuilder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.HasOne(h => h.JobApplication)
            .WithMany()
            .HasForeignKey(h => h.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<EmailMessage>().WithMany()
            .HasForeignKey(h => h.SourceEmailId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobPostingDetails>(entity =>
        {
            entity.HasOne(d => d.JobApplication)
            .WithOne()
            .HasForeignKey<JobPostingDetails>(d => d.JobApplicationId)//<> Telling which table hold the FK
            .OnDelete(DeleteBehavior.Cascade);

            //Doesnt need .HasIndex...IsUnique EF
        });

        modelBuilder.Entity<MailboxConnection>(entity =>
        {
            entity.Property(m => m.Provider).HasMaxLength(50);
            entity.HasIndex(m => new { m.UserId, m.Provider }).IsUnique();
        });

        modelBuilder.Entity<EmailMessage>(entity =>
        {
            entity.Property(e => e.ExternalMessageId).HasMaxLength(200);
            entity.HasIndex(e => new { e.UserId, e.ExternalMessageId }).IsUnique();
        });

        modelBuilder.Entity<PendingJobApplication>(entity =>
        {
            entity.HasOne(p => p.EmailMessage).WithMany()
                .HasForeignKey(p => p.EmailMessageId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JobApplication>().WithMany()
                .HasForeignKey(p => p.LinkedJobApplicationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => p.EmailMessageId).IsUnique();
        });

        modelBuilder.Entity<PendingStatusChange>(entity =>
        {
            entity.HasOne(p => p.EmailMessage).WithMany()
                .HasForeignKey(p => p.EmailMessageId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JobApplication>().WithMany()
                .HasForeignKey(p => p.SuggestedJobApplicationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => p.EmailMessageId).IsUnique();
        });

        modelBuilder.Entity<EmailMessage>()
        .Property(e => e.ProcessingStatus)
        .HasDefaultValue(EmailProcessingStatus.Pending);
    }
}