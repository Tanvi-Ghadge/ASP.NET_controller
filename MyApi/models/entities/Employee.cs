using System;

namespace MyApi.models.entities;

public class Employee
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
}
