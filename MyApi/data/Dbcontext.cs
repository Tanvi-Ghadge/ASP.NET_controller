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
    public DbSet<Refreshtoken> RefreshTokens { get; set; }
    public DbSet<AiMemory> AiMemories { get; set; }
    public DbSet<WorkflowCheckpointEntity> WorkflowCheckpoints { get; set; }
    public DbSet<ApprovalRequestEntity> ApprovalRequests { get; set; }
    public DbSet<ApprovalHistoryEntity> ApprovalHistory { get; set; }

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

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email)
            .IsUnique();

        modelBuilder.Entity<Employee>()
        .Property(e => e.Name)
        .IsRequired()
        .HasMaxLength(100);

        modelBuilder.Entity<AiMemory>(entity =>
        {
            entity.ToTable("AiMemories");
            entity.HasKey(m => m.MemoryId);
            entity.Property(m => m.UserId).IsRequired().HasMaxLength(128);
            entity.Property(m => m.Key).IsRequired().HasMaxLength(128);
            entity.Property(m => m.Value).IsRequired().HasMaxLength(2000);
            entity.HasIndex(m => new { m.UserId, m.Key }).IsUnique();
            entity.HasIndex(m => m.UserId);
        });

        modelBuilder.Entity<WorkflowCheckpointEntity>(entity =>
        {
            entity.ToTable("WorkflowCheckpoints");
            entity.HasKey(c => c.CheckpointId);
            entity.HasIndex(c => c.WorkflowInstanceId).IsUnique();
            entity.Property(c => c.WorkflowDefinitionId).IsRequired().HasMaxLength(128);
            entity.Property(c => c.WorkflowName).IsRequired().HasMaxLength(256);
            entity.Property(c => c.SessionId).IsRequired().HasMaxLength(128);
            entity.Property(c => c.UserId).IsRequired().HasMaxLength(128);
            entity.Property(c => c.CorrelationId).IsRequired().HasMaxLength(64);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(64);
            entity.Property(c => c.CompletedStepsJson).IsRequired();
            entity.Property(c => c.VariablesJson).IsRequired();
            entity.Property(c => c.CurrentMessage).IsRequired();
        });

        modelBuilder.Entity<ApprovalRequestEntity>(entity =>
        {
            entity.ToTable("ApprovalRequests");
            entity.HasKey(a => a.ApprovalRequestId);
            entity.HasIndex(a => a.WorkflowInstanceId);
            entity.HasIndex(a => a.Status);
            entity.Property(a => a.WorkflowDefinitionId).IsRequired().HasMaxLength(128);
            entity.Property(a => a.CorrelationId).IsRequired().HasMaxLength(64);
            entity.Property(a => a.SessionId).IsRequired().HasMaxLength(128);
            entity.Property(a => a.UserId).IsRequired().HasMaxLength(128);
            entity.Property(a => a.Title).IsRequired().HasMaxLength(256);
            entity.Property(a => a.Description).IsRequired().HasMaxLength(2000);
            entity.HasMany(a => a.History)
                .WithOne(h => h.ApprovalRequest)
                .HasForeignKey(h => h.ApprovalRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalHistoryEntity>(entity =>
        {
            entity.ToTable("ApprovalHistory");
            entity.HasKey(h => h.HistoryId);
            entity.Property(h => h.Action).IsRequired().HasMaxLength(64);
            entity.Property(h => h.Actor).IsRequired().HasMaxLength(128);
        });
    }
    
}


