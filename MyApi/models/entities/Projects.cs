using System;

namespace MyApi.models.entities;

public class Projects
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public decimal Budget { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public List<EmployeeProjects> EmployeeProjects { get; set; } = new();
}
