using System.Text;
using System.Text.RegularExpressions;

public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;

    public InputSanitizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Sanitize Query Parameters
        if (context.Request.Query.Any())
        {
            var sanitizedQuery = new Dictionary<string, string?>();

            foreach (var (key, value) in context.Request.Query)
            {
                sanitizedQuery[key] = Sanitize(value.ToString() ?? "");
            }

            context.Request.QueryString = QueryString.Create(sanitizedQuery);
        }

        // Sanitize JSON Body
        if (context.Request.ContentType != null &&
            context.Request.ContentType.Contains("application/json"))
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                leaveOpen: true
            );

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                var sanitizedBody = SanitizeJson(body);

                var bytes = Encoding.UTF8.GetBytes(sanitizedBody);
                context.Request.Body = new MemoryStream(bytes);
            }
        }

        await _next(context);
    }

    // Sanitize individual string
    private string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        input = input.Trim();

        // Remove script tags
        input = Regex.Replace(input,
            "<script.*?>.*?</script>",
            "",
            RegexOptions.IgnoreCase);

        // Remove HTML tags
        input = Regex.Replace(input, "<.*?>", "");

        return input;
    }

    // Sanitize full JSON string
    private string SanitizeJson(string json)
    {
        // Basic cleanup (safe level)
        json = Regex.Replace(json,
            "<script.*?>.*?</script>",
            "",
            RegexOptions.IgnoreCase);

        return json;
    }
}