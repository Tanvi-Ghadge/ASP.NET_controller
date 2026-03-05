using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("register")]
    public async Task<IActionResult> Register(Registerdto dto)
    {
        await _authService.Register(dto);
        return Ok("User registered");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(Logindto dto)
    {
        var tokens = await _authService.Login(dto);

        Response.Cookies.Append(
            "refreshToken",
            tokens.refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

        return Ok(new { accessToken = tokens.accessToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshtoken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshtoken))
            return Unauthorized();

        var newAccessToken = await _authService.RefreshToken(refreshtoken);

        if (newAccessToken == null)
            return Unauthorized();

        return Ok(new { accessToken = newAccessToken });
    }
}