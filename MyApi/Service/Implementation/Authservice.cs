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
    private readonly IHmacservice _hmacservice;

    public AuthService(
        Iemployeerepository employeeRepo,
        Irefreshtokenrepository refreshRepo,
        Itokenservice tokenService,
        IHmacservice hmacservice)
    {
        _employeeRepo = employeeRepo;
        _refreshRepo = refreshRepo;
        _tokenService = tokenService;
        _hmacservice = hmacservice;
    }

    public async Task<(string accessToken, string rawrefreshToken)> Register(Registerdto dto)
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
            HmacSecret = _hmacservice.GenerateHmacSecret(),
            DepartmentId = dto.DepartmentId
        };
        await _employeeRepo.AddAsync(employee);
        await _employeeRepo.SaveAsync();
        var accessToken = _tokenService.CreateAccessToken(employee);

        var rawrefreshToken = _tokenService.GenerateRefreshToken();
        var entity= _tokenService.CreateRefreshToken(employee, rawrefreshToken);

        

        await _refreshRepo.Add(entity);
        
        return (accessToken, rawrefreshToken);
        
        
    }

    public async Task<(string accessToken, string rawrefreshToken)> Login(Logindto dto)
    {
        var user = await _employeeRepo.GetByEmailAsync(dto.Email);

        if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        var accessToken = _tokenService.CreateAccessToken(user);

        var rawrefreshToken = _tokenService.GenerateRefreshToken();
        var entity= _tokenService.CreateRefreshToken(user, rawrefreshToken);

        

        await _refreshRepo.Add(entity);

        return (accessToken, rawrefreshToken);
    }

    public async Task<(string accessToken, string rawrefreshToken)> RefreshToken(string token)
    {
        var hashedToken = _tokenService.HashToken(token);
        var storedToken = await _refreshRepo.GetToken(hashedToken);

        
        if (storedToken == null || storedToken.Expires < DateTime.UtcNow)
            throw new Exception("Invalid refresh token");
        storedToken.IsRevoked = true;

        var employee = await _employeeRepo.GetByIdAsync(storedToken.EmployeeId);

        if (employee == null)
            throw new Exception("Employee not found");

        var accessToken = _tokenService.CreateAccessToken(employee);
        var rawrefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenEntity = _tokenService.CreateRefreshToken(employee, rawrefreshToken);

        await _refreshRepo.Add(newRefreshTokenEntity);

        return (accessToken, rawrefreshToken);
    }

    
}