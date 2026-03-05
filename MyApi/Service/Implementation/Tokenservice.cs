using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MyApi.Service.Interface;
using Microsoft.IdentityModel.Tokens;
using MyApi.models.entities;
public class TokenService : Itokenservice
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateAccessToken(Employee employee)
    {
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

    public Refreshtoken GenerateRefreshToken()
    {
        return new Refreshtoken
        {
            Token = Guid.NewGuid().ToString(),
            Expires = DateTime.UtcNow.AddDays(7)
        };
    }
}