using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyApi.DTO.Employee;
using MyApi.Service.Interface;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Iauthservice _authService;


    public AuthController(Iauthservice authService)
    {
        _authService = authService;

    }
    // Registration and login endpoints 
    [HttpPost("register")]
    public async Task<IActionResult> Register(Registerdto dto)
    {
        var (accessToken, refreshToken) = await _authService.Register(dto);

        return Ok(new
        {
            access_token = accessToken,
            refresh_token = refreshToken
        });
    }

    [EnableRateLimiting("loginpolicy")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(Logindto dto)
    {
        var (accessToken, refreshToken) = await _authService.Login(dto);

        return Ok(new
        {
            access_token = accessToken,
            refresh_token = refreshToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(string Token)
    {
        var (accessToken, refreshToken) = await _authService.RefreshToken(Token);

        return Ok(new
        {
            access_token = accessToken,
            refresh_token = refreshToken
        });
    }
}
