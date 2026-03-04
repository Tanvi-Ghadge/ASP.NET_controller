using System;

namespace MyApi.models.entities;

public class EmployeeProjects
{
    public int EmployeeId { get; set; }

    public int ProjectId { get; set; }

    public required string Role { get; set; }

    public DateTime AssignedDate { get; set; }
    public Employee? Employee { get; set; }
    public Projects? Project { get; set; }
    
}
