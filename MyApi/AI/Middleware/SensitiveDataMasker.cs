using System.Text.RegularExpressions;

namespace MyApi.AI.Middleware;

/// <summary>
/// Shared helpers for masking secrets in prompts/responses.
/// </summary>
public static class SensitiveDataMasker
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex PasswordRegex = new(
        @"(?i)(password|passwd|pwd|secret|api[_-]?key|token)\s*[:=]\s*\S+",
        RegexOptions.Compiled);

    public static string Mask(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var masked = PasswordRegex.Replace(input, "$1=***");
        masked = EmailRegex.Replace(masked, m =>
        {
            var parts = m.Value.Split('@');
            if (parts.Length != 2)
            {
                return "***";
            }

            var user = parts[0].Length <= 1 ? "*" : parts[0][0] + "***";
            return $"{user}@{parts[1]}";
        });
        return masked;
    }
}
