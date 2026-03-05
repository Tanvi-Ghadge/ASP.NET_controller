namespace MyApi.DTO.Employee;

public record class Registerdto
{
    public required string Name { get; set; }

    public required string Email { get; set; }

    public required string Password { get; set; }

    public required string Role { get; set; }

    public required int DepartmentId { get; set; }
}
