using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyApi.data;

/// <summary>
/// Design-time factory so EF migrations do not require Redis/Hangfire at startup.
/// </summary>
public sealed class DbcontextFactory : IDesignTimeDbContextFactory<Dbcontext>
{
    public Dbcontext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("defaultconnection")
            ?? throw new InvalidOperationException("Connection string 'defaultconnection' was not found.");

        var optionsBuilder = new DbContextOptionsBuilder<Dbcontext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new Dbcontext(optionsBuilder.Options);
    }
}
