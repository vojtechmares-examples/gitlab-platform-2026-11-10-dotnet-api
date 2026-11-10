using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotnetApi.Data;

/// <summary>
/// Used only by the EF Core tools ("dotnet ef migrations add / bundle"). The connection string is a placeholder:
/// the tools never connect, and the migration bundle gets the real one with --connection when the pipeline runs it.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time_only;Username=none;Password=none")
            .Options;
        return new AppDbContext(options);
    }
}
