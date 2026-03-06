using BCrypt.Net;
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

    public AuthService(
        Iemployeerepository employeeRepo,
        Irefreshtokenrepository refreshRepo,
        Itokenservice tokenService)
    {
        _employeeRepo = employeeRepo;
        _refreshRepo = refreshRepo;
        _tokenService = tokenService;
    }

    public async Task Register(Registerdto dto)
    {
        var existingUser = await _employeeRepo.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new Exception("Email already in use");
        var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var employee = new Employee
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = hash,
            role = dto.Role,
            DepartmentId = dto.DepartmentId
        };
        
        
        try
        {
            await _employeeRepo.AddAsync(employee);
            await _employeeRepo.SaveAsync();
        }
        catch (DbUpdateException)
        {
            
            throw new Exception("Email already in use");
        }
    }

    public async Task<(string accessToken, string refreshToken)> Login(Logindto dto)
    {
        var user = await _employeeRepo.GetByEmailAsync(dto.Email);

        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        var accessToken = _tokenService.CreateAccessToken(user);

        var refreshToken = _tokenService.GenerateRefreshToken();

        refreshToken.EmployeeId = user.Id;

        await _refreshRepo.Add(refreshToken);

        return (accessToken, refreshToken.Token);
    }

    public async Task<string?> RefreshToken(string token)
    {
        var storedToken = await _refreshRepo.GetToken(token);

        if (storedToken == null || storedToken.Expires < DateTime.UtcNow)
            return null;

        var employee = await _employeeRepo.GetByIdAsync(storedToken.EmployeeId);

        if (employee == null)
            return null;

        return _tokenService.CreateAccessToken(employee);
    }
}