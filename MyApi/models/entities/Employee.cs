using System;

namespace MyApi.models.entities;

// public class Employee
// {
//     public Guid Id { get; set; }
//     public required string Name { get; set; }
//     public required string Email { get; set; }
//     public string? Phone { get; set; }
//     public string? Department { get; set; }
// }

public class Employee
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string role { get; set; } = "Employee";

    public decimal Salary { get; set; }

    public int DepartmentId { get; set; }

    public int? ManagerId { get; set; }
    
     public Department? Department { get; set; }

    public Employee? Manager { get; set; }

    public List<Employee> Subordinates { get; set; } = new();

    public EmployeeProfile? Profile { get; set; }

    public List<EmployeeProjects> EmployeeProjects { get; set; } = new();
    public List<Refreshtoken> RefreshTokens { get; set; } = new();
}

