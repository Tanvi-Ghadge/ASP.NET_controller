using System;
using Microsoft.EntityFrameworkCore;
using MyApi.models.entities;

namespace MyApi.data;

public class Dbcontext : DbContext
{
    public Dbcontext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
}
