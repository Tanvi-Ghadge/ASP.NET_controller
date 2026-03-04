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
    public DbSet<Company> Companies { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }
    public DbSet<Projects> Projects { get; set; }
    public DbSet<EmployeeProjects> EmployeeProjects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company - Department (one-to-many)
        modelBuilder.Entity<Company>()
            .HasMany(c => c.Departments)
            .WithOne(d => d.Company)
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Department - Employee (one-to-many)
        modelBuilder.Entity<Department>()
            .HasMany(d => d.Employees)
            .WithOne(e => e.Department)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Department - Manager (Employee) (each department has one manager)
        // No inverse collection on Employee specifically for departments.
        modelBuilder.Entity<Department>()
            .HasOne(d => d.Manager)
            .WithMany()
            .HasForeignKey(d => d.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Department - Projects (one-to-many)
        modelBuilder.Entity<Projects>()
            .HasOne(p => p.Department)
            .WithMany(d => d.Projects)
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Employee - Employee (manager/subordinates self-reference)
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Manager)
            .WithMany(e => e.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // EmployeeProfile primary key
        modelBuilder.Entity<EmployeeProfile>()
            .HasKey(p => p.EmployeeId);

        // Employee - EmployeeProfile (one-to-one)
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Profile)
            .WithOne(p => p.Employee)
            .HasForeignKey<EmployeeProfile>(p => p.EmployeeId);

        // Employee - Projects (many-to-many via EmployeeProjects)
        modelBuilder.Entity<EmployeeProjects>()
            .HasKey(ep => new { ep.EmployeeId, ep.ProjectId });

        modelBuilder.Entity<EmployeeProjects>()
            .HasOne(ep => ep.Employee)
            .WithMany(e => e.EmployeeProjects)
            .HasForeignKey(ep => ep.EmployeeId);

        modelBuilder.Entity<EmployeeProjects>()
            .HasOne(ep => ep.Project)
            .WithMany(p => p.EmployeeProjects)
            .HasForeignKey(ep => ep.ProjectId);

        modelBuilder.Entity<Employee>()
        .Property(e => e.Salary)
        .HasPrecision(10, 2);

    modelBuilder.Entity<Projects>()
        .Property(p => p.Budget)
        .HasPrecision(12, 2);
    }

}


