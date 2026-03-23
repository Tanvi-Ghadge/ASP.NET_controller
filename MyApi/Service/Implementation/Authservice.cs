using BCrypt.Net;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MyApi.DTO.Employee;
using MyApi.models.entities;
using MyApi.Repository.Interface;
using MyApi.Service.Interface;

public class AuthService : Iauthservice
{
    private readonly Iemployeerepository _employeeRepo;
    private readonly Irefreshtokenrepository _refreshRepo;
    private readonly Itokenservice _tokenService;
    private readonly IHmacservice _hmacservice;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        Iemployeerepository employeeRepo,
        Irefreshtokenrepository refreshRepo,
        Itokenservice tokenService,
        IHmacservice hmacservice,
        ILogger<AuthService> logger)
    {
        _employeeRepo = employeeRepo;
        _refreshRepo = refreshRepo;
        _tokenService = tokenService;
        _hmacservice = hmacservice;
        _logger = logger;
    }

    public async Task<(string accessToken, string rawrefreshToken)> Register(Registerdto dto)
    {
        _logger.LogInformation("Register requested for email {Email}.", dto.Email);
        var existingUser = await _employeeRepo.GetByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration rejected because email already exists ({Email}).", dto.Email);
            throw new Exception("Email already in use");
        }
        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var employee = new Employee
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = hash,
            role = dto.Role,
            HmacSecret = _hmacservice.GenerateHmacSecret(),
            DepartmentId = dto.DepartmentId
        };
        await _employeeRepo.AddAsync(employee);
        await _employeeRepo.SaveAsync();
        var accessToken = _tokenService.CreateAccessToken(employee);

        var rawrefreshToken = _tokenService.GenerateRefreshToken();
        var entity= _tokenService.CreateRefreshToken(employee, rawrefreshToken);

        

        await _refreshRepo.Add(entity);
        _logger.LogInformation("Registration completed for employee (id={EmployeeId}, email={Email}).", employee.Id, employee.Email);
        
        return (accessToken, rawrefreshToken);
        
        
    }

    public async Task<(string accessToken, string rawrefreshToken)> Login(Logindto dto)
    {
        _logger.LogInformation("Login requested for email {Email}.", dto.Email);
        var user = await _employeeRepo.GetByEmailAsync(dto.Email);

        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for email {Email}.", dto.Email);
            throw new Exception("Invalid credentials");
        }

        var accessToken = _tokenService.CreateAccessToken(user);

        var rawrefreshToken = _tokenService.GenerateRefreshToken();
        var entity= _tokenService.CreateRefreshToken(user, rawrefreshToken);

        

        await _refreshRepo.Add(entity);
        _logger.LogInformation("Login succeeded for employee (id={EmployeeId}, email={Email}).", user.Id, user.Email);

        return (accessToken, rawrefreshToken);
    }

    public async Task<(string accessToken, string rawrefreshToken)> RefreshToken(string token)
    {
        _logger.LogInformation("Refresh token requested.");
        var hashedToken = _tokenService.HashToken(token);
        var storedToken = await _refreshRepo.GetToken(hashedToken);

        
        if (storedToken == null || storedToken.Expires < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token rejected because it was missing or expired.");
            throw new Exception("Invalid refresh token");
        }
        storedToken.IsRevoked = true;

        var employee = await _employeeRepo.GetByIdAsync(storedToken.EmployeeId);

        if (employee == null)
        {
            _logger.LogWarning("Refresh token failed because employee was not found (employeeId={EmployeeId}).", storedToken.EmployeeId);
            throw new Exception("Employee not found");
        }

        var accessToken = _tokenService.CreateAccessToken(employee);
        var rawrefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenEntity = _tokenService.CreateRefreshToken(employee, rawrefreshToken);

        await _refreshRepo.Add(newRefreshTokenEntity);
        _logger.LogInformation("Refresh token succeeded for employee (id={EmployeeId}).", employee.Id);

        return (accessToken, rawrefreshToken);
    }

    
}
