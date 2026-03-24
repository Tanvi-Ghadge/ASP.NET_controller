namespace MyApi.DTO.Employee;

public record class ProjectDto
{
    public int ProjectId { get; set; }
    public string? Name { get; set; }
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
}
