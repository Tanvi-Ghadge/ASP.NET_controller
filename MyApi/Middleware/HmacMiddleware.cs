using System.Security.Claims;
using System.Text;
using MyApi.Repository.Interface;
using MyApi.Service.Implementation;
using MyApi.Service.Interface;
public class HmacMiddleware
{
    private readonly RequestDelegate _next;
    public HmacMiddleware(
        RequestDelegate next
        )
    {
        _next = next;
    }
    public async Task Invoke(HttpContext context, IHmacservice _hmacservice, Iemployeerepository _employeeRepo)
    {
        var endpoint = context.GetEndpoint();
        var requiresHmac = endpoint?.Metadata.GetMetadata<RequireHmacAttribute>();

        if (requiresHmac != null)
        {
            //  1. Get user from JWT
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            var user = await _employeeRepo.GetByIdAsync(int.Parse(userId));

            if (user == null || string.IsNullOrEmpty(user.HmacSecret))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("User not found or no HMAC secret");
                return;
            }

            var secret = user.HmacSecret;

            //  2. Headers
            var signature = context.Request.Headers["X-Signature"].FirstOrDefault();
            var timestamp = context.Request.Headers["X-Timestamp"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing HMAC headers");
                return;
            }

            //  3. Replay protection (timestamp)
            if (!DateTime.TryParse(timestamp, out var requestTime) ||
                (DateTime.UtcNow - requestTime).TotalMinutes > 5)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Request expired");
                return;
            }

            //  4. Read body safely
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            //  5. Build data string
            var data = context.Request.Method +
                       context.Request.Path +
                       body +
                       timestamp;

            var computedSignature = _hmacservice.GenerateSignature(data, secret);

            if (computedSignature != signature)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid signature");
                return;
            }
        }

        await _next(context);
    }
}