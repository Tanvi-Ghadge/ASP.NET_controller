using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MyApi.DTO.Employee;
using MyApi.Repository.Interface;
using System.Data;

namespace MyApi.Repository.Implementation;

public class DapperEmployeeRepository : IDapperrepo
{
    private readonly IDbConnection _db;
    private readonly ILogger<DapperEmployeeRepository> _logger;

    public DapperEmployeeRepository(IDbConnection db, ILogger<DapperEmployeeRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Reademployeedto>> GetAllAsync()
    {
        const string sql = """
            SELECT
                e.Id,
                e.Name,
                e.Email,
                e.Salary,
                d.Name AS DepartmentName,
                m.Name AS ManagerName
            FROM Employees e
            INNER JOIN Departments d ON d.Id = e.DepartmentId
            LEFT JOIN Employees m ON m.Id = e.ManagerId
            ORDER BY e.Id;
            """;

        _logger.LogInformation("Fetching all employees with Dapper.");

        await using var connection = CreateConnection();
        var employees = await connection.QueryAsync<Reademployeedto>(sql);
        return employees.ToList();
    }

    

    public async Task<Reademployeedto?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT
                e.Id,
                e.Name,
                e.Email,
                e.Salary,
                d.Name AS DepartmentName,
                m.Name AS ManagerName
            FROM Employees e
            INNER JOIN Departments d ON d.Id = e.DepartmentId
            LEFT JOIN Employees m ON m.Id = e.ManagerId
            WHERE e.Id = @Id;
            """;

        _logger.LogInformation("Fetching employee by id with Dapper (id={EmployeeId}).", id);

        await using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Reademployeedto>(sql, new { Id = id });
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM Employees
            WHERE Email = @Email
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId);
            """;

        await using var connection = CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, ExcludeId = excludeId });
        return count > 0;
    }

    public async Task<int> CreateAsync(Createemployeedto dto, string passwordHash, string hmacSecret)
{
    _logger.LogInformation(
        "Creating employee using stored procedure (email={Email}, name={Name}).",
        dto.Email, dto.Name);

    await using var connection = CreateConnection();

    return await connection.ExecuteScalarAsync<int>(
        "dbo.sp_CreateEmployee",   // stored procedure name
        new
        {
            dto.Name,
            dto.Email,
            PasswordHash = passwordHash,
            dto.Role,
            dto.Salary,
            HmacSecret = hmacSecret,
            dto.DepartmentId,
            dto.ManagerId
        },
        commandType: CommandType.StoredProcedure
    );
}

    public async Task<bool> UpdateAsync(int id, Updateemployeedto dto)
    {
        const string sql = """
            UPDATE Employees
            SET Name = @Name,
                Email = @Email,
                Salary = @Salary,
                DepartmentId = @DepartmentId,
                ManagerId = @ManagerId
            WHERE Id = @Id;
            """;

        _logger.LogInformation("Updating employee with Dapper (id={EmployeeId}).", id);

        await using var connection = CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new
        {
            Id = id,
            dto.Name,
            dto.Email,
            dto.Salary,
            dto.DepartmentId,
            dto.ManagerId
        });

        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = """
            DELETE FROM Employees
            WHERE Id = @Id;
            """;

        _logger.LogInformation("Deleting employee with Dapper (id={EmployeeId}).", id);

        await using var connection = CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<IEnumerable<Reademployeedto>> GetEmployeesWithProjectsAsync()
    {
        const string sql = @"
        SELECT 
            e.Id,
            e.Name,
            e.Email,
            e.Salary,

            d.Name AS DepartmentName,
            m.Name AS ManagerName,

            p.Id AS ProjectId,
            p.Name,
            p.Budget,
            p.StartDate

        FROM Employees e
        INNER JOIN Departments d ON e.DepartmentId = d.Id
        LEFT JOIN Employees m ON e.ManagerId = m.Id

        INNER JOIN EmployeeProjects ep ON e.Id = ep.EmployeeId
        INNER JOIN Projects p ON ep.ProjectId = p.Id;
        ";

        await using var connection = CreateConnection();

        var dict = new Dictionary<int, Reademployeedto>();

        var result = await connection.QueryAsync<
            Reademployeedto,
            ProjectDto,
            Reademployeedto
        >(
            sql,
            (emp, proj) =>
            {
                //  Deduplicate employee
                if (!dict.TryGetValue(emp.Id, out var existing))
                {
                    existing = emp;
                    existing.Projects = new List<ProjectDto>();
                    dict.Add(emp.Id, existing);
                }

                //  Add project if exists
                if (proj != null && proj.ProjectId != 0)
                {
                    existing.Projects.Add(proj);
                }

                return existing;
            },
            splitOn: "ProjectId"
        );

        return dict.Values;
    }

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_db.ConnectionString);
    }

    

    
}