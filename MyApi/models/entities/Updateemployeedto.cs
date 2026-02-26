namespace MyApi.models.entities;

public record class Updateemployeedto
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
}
