using Microsoft.EntityFrameworkCore;
using StudyApp.Api.Models;

namespace StudyApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<Figure> Figures => Set<Figure>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<ExtractionRun> ExtractionRuns => Set<ExtractionRun>();
    public DbSet<GenerationRun> GenerationRuns => Set<GenerationRun>();
    public DbSet<StudyGuide> StudyGuides => Set<StudyGuide>();
    public DbSet<Flashcard> Flashcards => Set<Flashcard>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<ConceptMap> ConceptMaps => Set<ConceptMap>();

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

        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<ExtractionRun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Status).HasConversion<string>();
            entity.HasOne(r => r.Module)
                  .WithMany(m => m.ExtractionRuns)
                  .HasForeignKey(r => r.ModuleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.FileName).HasMaxLength(500);
            entity.Property(d => d.Status)
                  .HasConversion<string>();
            entity.HasOne(d => d.Module)
                  .WithMany(m => m.Documents)
                  .HasForeignKey(d => d.ModuleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.FileName).HasMaxLength(500);
            entity.HasOne(c => c.Document)
                  .WithMany(d => d.Chunks)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Figure>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.S3Key).HasMaxLength(1000);
            entity.HasOne(f => f.Document)
                  .WithMany(d => d.Figures)
                  .HasForeignKey(f => f.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.Module)
                  .WithMany(m => m.Sections)
                  .HasForeignKey(s => s.ModuleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GenerationRun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Status).HasConversion<string>();
            entity.HasOne(r => r.Module)
                  .WithMany(m => m.GenerationRuns)
                  .HasForeignKey(r => r.ModuleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudyGuide>(entity =>
        {
            entity.HasKey(sg => sg.Id);
            entity.HasOne(sg => sg.Section)
                  .WithMany()
                  .HasForeignKey(sg => sg.SectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Flashcard>(entity =>
        {
            entity.HasKey(fc => fc.Id);
            entity.HasOne(fc => fc.Section)
                  .WithMany()
                  .HasForeignKey(fc => fc.SectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(qq => qq.Id);
            entity.HasOne(qq => qq.Section)
                  .WithMany()
                  .HasForeignKey(qq => qq.SectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConceptMap>(entity =>
        {
            entity.HasKey(cm => cm.Id);
            entity.HasOne(cm => cm.Section)
                  .WithMany()
                  .HasForeignKey(cm => cm.SectionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
