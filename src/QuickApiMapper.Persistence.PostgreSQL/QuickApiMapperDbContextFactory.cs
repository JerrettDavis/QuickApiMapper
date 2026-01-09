using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuickApiMapper.Persistence.PostgreSQL;

/// <summary>
/// Design-time factory for QuickApiMapperDbContext to support EF Core migrations.
/// </summary>
public class QuickApiMapperDbContextFactory : IDesignTimeDbContextFactory<QuickApiMapperDbContext>
{
    public QuickApiMapperDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuickApiMapperDbContext>();

        // Use a default connection string for migrations
        // This will be replaced at runtime with the actual connection string
        optionsBuilder.UseNpgsql("Host=localhost;Database=quickapimapper;Username=postgres;Password=postgres");

        return new QuickApiMapperDbContext(optionsBuilder.Options);
    }
}
