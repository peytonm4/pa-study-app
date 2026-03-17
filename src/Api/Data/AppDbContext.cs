using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Models;

namespace StudyApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.Name).HasMaxLength(100);
            entity.HasIndex(u => u.Email).IsUnique();

            // DevUser seed — hardcoded Guid REQUIRED (never use Guid.NewGuid() in HasData)
            entity.HasData(new User
            {
                Id = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Dev User",
                Email = "dev@local",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }
}
