namespace MyApi.DTO.Employee;

public sealed class EmployeeQueryDto
{
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public decimal? MinSalary { get; set; }

    public string? SortBy { get; set; } // name | salary | id
    public bool Desc { get; set; } = false;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

