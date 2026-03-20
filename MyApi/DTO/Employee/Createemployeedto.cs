using System.ComponentModel.DataAnnotations;

namespace MyApi.DTO.Employee;

public record class Createemployeedto
{
    [Required]
    public required string Name { get; set; }
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string Role { get; set; } = "Employee";
    public decimal Salary { get; set; }
    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }
}
