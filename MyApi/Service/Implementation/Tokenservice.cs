using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MyApi.Service.Interface;
using Microsoft.IdentityModel.Tokens;
using MyApi.models.entities;
public class TokenService : Itokenservice
{
    private readonly IConfiguration _config;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration config, ILogger<TokenService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string CreateAccessToken(Employee employee)
    {
        _logger.LogInformation("Creating access token for employee (id={EmployeeId}, email={Email}).", employee.Id, employee.Email);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new Claim(ClaimTypes.Email, employee.Email),
            new Claim(ClaimTypes.Role, employee.role)
        };

        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        _logger.LogInformation("Generating refresh token.");
        var bytes = new byte[64];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        _logger.LogInformation("Hashing refresh token.");
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public Refreshtoken CreateRefreshToken(Employee employee, string rawToken)
    {
        _logger.LogInformation("Creating refresh token entity for employee (id={EmployeeId}).", employee.Id);
        return new Refreshtoken
        {
            Token = HashToken(rawToken),
            EmployeeId = employee.Id,
            Expires = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
    }

    
}
