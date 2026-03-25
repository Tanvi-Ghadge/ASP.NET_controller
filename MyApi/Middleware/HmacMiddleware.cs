using System.Text;
using System.Security.Cryptography;
using MyApi.data;
using MyApi.Service.Interface;
using Microsoft.EntityFrameworkCore; 
public class HmacMiddleware
{
    private readonly RequestDelegate _next;

    public HmacMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(
        HttpContext context,
        Dbcontext db,
        IHmacservice hmacService,
        INonceservice nonceService)
    {
        var path = context.Request.Path.Value!.ToLower();

        if (path!.Contains("/auth"))
        {
            await _next(context);
            return;
        }
        if (path!.Contains("/dapper-employees"))
        {
            await _next(context);
            return;
        }

        var apiKey = context.Request.Headers["X-API-KEY"].FirstOrDefault();
        var signature = context.Request.Headers["X-SIGNATURE"].FirstOrDefault();
        var timestamp = context.Request.Headers["X-TIMESTAMP"].FirstOrDefault();
        var nonce = context.Request.Headers["X-NONCE"].FirstOrDefault();

        if (apiKey == null || signature == null || timestamp == null || nonce == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing headers");
            return;
        }

        var employee = db.Employees.FirstOrDefault(e => e.ApiKey == apiKey);

        if (employee == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("user not found with apikey");
            return;
        }

        //Replay check
        if (await nonceService.IsReplayAsync(nonce))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Replay detected");
            return;
        }

        // ⏱️ Timestamp check
        var requestTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp));

        if (DateTime.UtcNow - requestTime > TimeSpan.FromMinutes(5))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Request expired");
            return;
        }

        var secret = hmacService.Decrypt(employee.HmacSecretEncrypted);

        context.Request.EnableBuffering();

        string body = "";
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

       var method = context.Request.Method.ToUpper();
        

        var data = $"{method}{path}{body}{timestamp}{nonce}";

        var isValid = hmacService.VerifySignature(secret, data, signature);

        if (!isValid)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid signature-no match");
            return;
        }

        // ✅ Store nonce
        await nonceService.StoreNonceAsync(nonce, TimeSpan.FromMinutes(5));

        await _next(context);
    }
}