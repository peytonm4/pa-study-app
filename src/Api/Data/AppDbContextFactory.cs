using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudyApp.Api.Data;

// Used by dotnet-ef migrations tooling only (design time)
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=studyapp;Username=studyapp;Password=studyapp");
        return new AppDbContext(optionsBuilder.Options);
    }
}
