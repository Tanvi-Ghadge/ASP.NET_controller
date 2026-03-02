using System;
using Microsoft.EntityFrameworkCore;
using MyApi.models.entities;

namespace MyApi.data;

public class Dbcontext : DbContext
{
    public Dbcontext(DbContextOptions<Dbcontext> options) : base(options)
    {
        
    }

    public DbSet<Employee> Employees { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 🔷 INDEXING EXAMPLE
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Id)
            .IsUnique(); // Email must be unique
        // 🔷 CONFIGURING PROPERTIES
        modelBuilder.Entity<Employee>()
            .Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);
    }

}

public class TeamsDbcontext : DbContext
{
    public TeamsDbcontext(DbContextOptions<TeamsDbcontext> options) : base(options)     //need non generic options for migration in case of multiple dbcontext
    {
        
    }

    public DbSet<League> Leagues { get; set; }

    public DbSet<Teams> Teams { get; set; }

}
