using System;

namespace MyApi.models.entities;

public class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public DateTime EstablishedDate { get; set; }
    
    public List<Department> Departments { get; set; } = new();

}
