using System.Text.RegularExpressions;

namespace MyApi.AI.Memory;

/// <summary>
/// Heuristic memory extractor for durable preferences and profile facts.
/// Ignores transactional / one-off operational requests.
/// </summary>
public sealed partial class MemoryExtractionService : IMemoryExtractionService
{
    private readonly IMemoryStore _memoryStore;
    private readonly ILogger<MemoryExtractionService> _logger;

    public MemoryExtractionService(
        IMemoryStore memoryStore,
        ILogger<MemoryExtractionService> logger)
    {
        _memoryStore = memoryStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MemoryRecord>> ExtractAsync(
        string userId,
        string userMessage,
        string? assistantReply = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userMessage))
        {
            return Task.FromResult<IReadOnlyList<MemoryRecord>>(Array.Empty<MemoryRecord>());
        }

        if (IsTransactionalRequest(userMessage))
        {
            _logger.LogDebug(
                "Skipping memory extraction for transactional message from user {UserId}.",
                userId);
            return Task.FromResult<IReadOnlyList<MemoryRecord>>(Array.Empty<MemoryRecord>());
        }

        var candidates = new List<MemoryRecord>();
        TryExtractReportPreference(userId, userMessage, candidates);
        TryExtractDepartment(userId, userMessage, candidates);
        TryExtractPreferredEmployee(userId, userMessage, candidates);
        TryExtractSummaryPreference(userId, userMessage, candidates);
        TryExtractGenericPreference(userId, userMessage, candidates);

        return Task.FromResult<IReadOnlyList<MemoryRecord>>(candidates);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MemoryRecord>> ExtractAndPersistAsync(
        string userId,
        string userMessage,
        string? assistantReply = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = await ExtractAsync(userId, userMessage, assistantReply, cancellationToken);
        if (candidates.Count == 0)
        {
            return candidates;
        }

        var saved = new List<MemoryRecord>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var persisted = await _memoryStore.SaveMemoryAsync(candidate, cancellationToken);
            saved.Add(persisted);
        }

        _logger.LogInformation(
            "Persisted {Count} memories for user {UserId}.",
            saved.Count,
            userId);

        return saved;
    }

    private static bool IsTransactionalRequest(string message)
    {
        var text = message.Trim();

        // One-off operational asks must never become long-term memory.
        if (TransactionalPrefixRegex().IsMatch(text))
        {
            return true;
        }

        if (ShowEmployeeRegex().IsMatch(text))
        {
            return true;
        }

        return false;
    }

    private static void TryExtractReportPreference(
        string userId,
        string message,
        List<MemoryRecord> candidates)
    {
        var match = ReportFormatRegex().Match(message);
        if (!match.Success)
        {
            match = ReportFormatInPhraseRegex().Match(message);
        }

        if (!match.Success)
        {
            return;
        }

        var format = NormalizeReportFormat(match.Groups["format"].Value);
        if (format is null)
        {
            return;
        }

        candidates.Add(Create(
            userId,
            key: "PreferredReportFormat",
            value: format,
            type: MemoryType.UserPreference));
    }

    private static void TryExtractDepartment(
        string userId,
        string message,
        List<MemoryRecord> candidates)
    {
        var match = DepartmentRegex().Match(message);
        if (!match.Success)
        {
            return;
        }

        candidates.Add(Create(
            userId,
            key: "Department",
            value: match.Groups["department"].Value.Trim(),
            type: MemoryType.UserProfile));
    }

    private static void TryExtractPreferredEmployee(
        string userId,
        string message,
        List<MemoryRecord> candidates)
    {
        var match = PreferredEmployeeRegex().Match(message);
        if (!match.Success)
        {
            return;
        }

        candidates.Add(Create(
            userId,
            key: "PreferredEmployeeId",
            value: match.Groups["id"].Value,
            type: MemoryType.LearnedFact));
    }

    private static void TryExtractSummaryPreference(
        string userId,
        string message,
        List<MemoryRecord> candidates)
    {
        if (!SummaryPreferenceRegex().IsMatch(message))
        {
            return;
        }

        candidates.Add(Create(
            userId,
            key: "PreferredReportDetail",
            value: "Summary",
            type: MemoryType.UserPreference));
    }

    private static void TryExtractGenericPreference(
        string userId,
        string message,
        List<MemoryRecord> candidates)
    {
        // Catch "I prefer X" when not already captured as a report format.
        if (candidates.Count > 0)
        {
            return;
        }

        var match = GenericPreferRegex().Match(message);
        if (!match.Success)
        {
            return;
        }

        var value = match.Groups["value"].Value.Trim().TrimEnd('.');
        if (value.Length < 2 || value.Length > 120)
        {
            return;
        }

        candidates.Add(Create(
            userId,
            key: "UserPreference",
            value: value,
            type: MemoryType.UserPreference));
    }

    private static string? NormalizeReportFormat(string raw)
    {
        var value = raw.Trim().TrimEnd('.', ',', '!');
        return value.ToUpperInvariant() switch
        {
            "PDF" => "PDF",
            "EXCEL" or "XLS" or "XLSX" or "SPREADSHEET" => "Excel",
            "CSV" => "CSV",
            "WORD" or "DOC" or "DOCX" => "Word",
            _ => value.Length <= 30 ? value : null
        };
    }

    private static MemoryRecord Create(string userId, string key, string value, MemoryType type)
    {
        var now = DateTimeOffset.UtcNow;
        return new MemoryRecord
        {
            MemoryId = Guid.NewGuid(),
            UserId = userId,
            Key = key,
            Value = value,
            Type = type,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    [GeneratedRegex(
        @"^\s*(show|list|get|find|fetch|delete|remove|update|create|add)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TransactionalPrefixRegex();

    [GeneratedRegex(
        @"\b(show|get|find|fetch)\s+employee\s+\d+\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ShowEmployeeRegex();

    [GeneratedRegex(
        @"\b(?:i\s+(?:always\s+)?(?:prefer|want)|always\s+prefer)\s+(?<format>pdf|excel|xlsx|xls|csv|word|doc|docx|spreadsheet)(?:\s+reports?)?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReportFormatRegex();

    [GeneratedRegex(
        @"\b(?:reports?|payroll)\b.*\b(?:in|as)\s+(?<format>pdf|excel|xlsx|xls|csv|word|doc|docx)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReportFormatInPhraseRegex();

    [GeneratedRegex(
        @"\bmy\s+department\s+is\s+(?<department>[A-Za-z][A-Za-z0-9 &\-]{1,40})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DepartmentRegex();

    [GeneratedRegex(
        @"\bi\s+always\s+work\s+with\s+employee\s+(?<id>\d+)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PreferredEmployeeRegex();

    [GeneratedRegex(
        @"\bi\s+only\s+want\s+summarized\s+reports?\b|\bprefer\s+summar(?:y|ized)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SummaryPreferenceRegex();

    [GeneratedRegex(
        @"\bi\s+(?:always\s+)?prefer\s+(?<value>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GenericPreferRegex();
}
