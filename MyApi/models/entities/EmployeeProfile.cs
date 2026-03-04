using System;

namespace MyApi.models.entities;

public class EmployeeProfile
{
    public int EmployeeId { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }
    
    public Employee? Employee { get; set; }
    
    
}
