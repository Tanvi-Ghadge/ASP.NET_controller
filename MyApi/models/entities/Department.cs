using System;

namespace MyApi.models.entities;

public class Department
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int CompanyId { get; set; }

    public int? ManagerId { get; set; }

    // Navigation properties

    public Company? Company { get; set; }

    public Employee? Manager { get; set; }


    public List<Employee> Employees { get; set; } = new();

    public List<Projects> Projects { get; set; } = new();
}
