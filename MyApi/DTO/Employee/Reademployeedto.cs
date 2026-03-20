using System.ComponentModel.DataAnnotations;

namespace MyApi.DTO.Employee;

public record class Reademployeedto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public decimal Salary { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
   
}
