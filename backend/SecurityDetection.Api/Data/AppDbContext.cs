using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Signature> Signatures => Set<Signature>();
    public DbSet<Detection> Detections => Set<Detection>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Detection>().HasIndex(x => new { x.Status, x.Priority, x.CreatedAtUtc });
        modelBuilder.Entity<Detection>().HasIndex(x => new { x.AssignedToUserId, x.Status })
            .IsUnique().HasFilter("[AssignedToUserId] IS NOT NULL AND [Status] = 1");
        modelBuilder.Entity<Detection>().HasOne(x => x.Customer).WithMany(x => x.Detections)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Detection>().HasOne(x => x.Signature).WithMany(x => x.Detections)
            .HasForeignKey(x => x.SignatureId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Detection>().HasOne(x => x.AssignedToUser).WithMany(x => x.AssignedDetections)
            .HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
