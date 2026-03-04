namespace MyApi.DTO.Employee;

public record class Updateemployeedto
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public decimal Salary { get; set; }
    public int DepartmentId { get; set; }
    public int? ManagerId { get; set; }
}
